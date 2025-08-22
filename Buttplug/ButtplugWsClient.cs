using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LitJson;
using UnityEngine;
using WebSocket4Net;
using BepInEx.Logging;

namespace KeepVibingAndNobodyExplodes.Buttplug
{
    // Wrapper class to avoid ambiguous reference with HarmonyX
    internal class MessageQueue
    {
        private readonly object _queue;
        private readonly System.Type _queueType;
        
        public MessageQueue()
        {
            // Use reflection to avoid ambiguous reference
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(System.Collections.Concurrent.ConcurrentBag<>));
            _queueType = assembly.GetType("System.Collections.Concurrent.ConcurrentQueue`1").MakeGenericType(typeof(IEnumerator));
            _queue = System.Activator.CreateInstance(_queueType);
        }
        
        public void Enqueue(IEnumerator item) 
        {
            var method = _queueType.GetMethod("Enqueue");
            method.Invoke(_queue, new object[] { item });
        }
        
        public bool TryDequeue(out IEnumerator result) 
        {
            var method = _queueType.GetMethod("TryDequeue");
            var parameters = new object[] { null };
            bool success = (bool)method.Invoke(_queue, parameters);
            result = (IEnumerator)parameters[0];
            return success;
        }
    }

    public class DeviceListEventArgs : EventArgs
    {
        public List<Device> Before { get; set; }
        public List<Device> After { get; set; }
    }

    public class ButtplugWsClient : MonoBehaviour
    {
        private WebSocket websocket;
        private MessageQueue incoming;
        
        public event EventHandler<DeviceListEventArgs> OnDeviceListUpdated;
        
        public List<Device> Devices { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsConsensual { get; set; } = true;
        
        private bool reconnecting;
        private ManualLogSource Logger;
        
        // Configuration - you can make these configurable later
        private string WebSocketHost = "ws://127.0.0.1";
        private int WebSocketPort = 12345;
        private int ReconnectBackoffSecs = 5;

        private void Start() 
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource("ButtplugWsClient");
            Open();
        }

        private void Update()
        {
            // Process incoming messages on main thread
            while (incoming != null && incoming.TryDequeue(out IEnumerator coroutine))
            {
                StartCoroutine(coroutine);
            }
        }

        private void OnDestroy()
        {
            StopScan();
            StopAllDevices();
            Close();
        }

        public void Open()
        {
            IsConnected = false;
            Devices = new List<Device>();
            incoming = new MessageQueue();
            string address = WebSocketHost + ":" + WebSocketPort;
            
            if (!reconnecting)
            {
                Logger.LogInfo($"Connecting to Intiface server at {address}");
            }
            
            websocket = new WebSocket(address);
            websocket.Opened += (s, e) => incoming.Enqueue(OnOpened());
            websocket.Closed += (s, e) => incoming.Enqueue(OnClosed());
            websocket.MessageReceived += (s, e) => incoming.Enqueue(OnMessageReceived(e));
            websocket.Error += (s, e) => incoming.Enqueue(OnError(e));
            websocket.Open();
            StartCoroutine(RunReceiveLoop());
        }

        public void Close()
        {
            if (Logger != null)
                Logger.LogInfo("Disconnecting from Intiface server.");
            if (websocket != null)
            {
                websocket.Close();
                CleanUp();
            }
        }

        public void LinearCmd(DeviceFeature feature, float position, float durationSecs) =>
            SendWithConsent(
                Buttplug.LinearCmd(feature.Device, feature.FeatureIndex, position, durationSecs),
                feature);

        public void VibrateCmd(DeviceFeature feature, float intensity) =>
            SendWithConsent(
                Buttplug.ScalarCmd(feature.Device, feature.FeatureIndex, intensity, "Vibrate"),
                feature);

        public void ConstrictCmd(DeviceFeature feature, float pressure) =>
            SendWithConsent(
                Buttplug.ScalarCmd(feature.Device, feature.FeatureIndex, pressure, "Constrict"),
                feature);

        public void OscillateCmd(DeviceFeature feature, float speed) =>
            SendWithConsent(
                Buttplug.ScalarCmd(feature.Device, feature.FeatureIndex, speed, "Oscillate"),
                feature);

        public void RotateCmd(DeviceFeature feature, float speed, bool clockwise) =>
            SendWithConsent(
                Buttplug.RotateCmd(feature.Device, feature.FeatureIndex, speed, clockwise),
                feature);

        public void BatteryLevelCmd(Device device) => Send(Buttplug.SensorReadCmd(device, 0, "Battery"));

        public void StopDeviceCmd(Device device) => Send(Buttplug.StopDeviceCmd(device));

        public void StopAllDevices() => Send(Buttplug.StopAllDevices());

        private void RequestServerInfo() => Send(Buttplug.RequestServerInfo());

        private void RequestDeviceList() => Send(Buttplug.RequestDeviceList());

        public void StartScan() => Send(Buttplug.StartScan());

        private void StopScan() => Send(Buttplug.StopScan());

        public void Connect()
        {
            Close();
            Open();
        }

        private void Send(object command) 
        {
            if (websocket != null && websocket.State == WebSocketState.Open)
            {
                websocket.Send(JsonMapper.ToJson(new[] { command }));
            }
        }

        private void SendWithConsent(object command, DeviceFeature feature)
        {
            if (IsConsensual && feature.Settings.Enabled)
            {
                Send(command);
            }
        }

        private void CleanUp()
        {
            if (websocket != null)
            {
                websocket.Dispose();
                websocket = null;
            }
            
            StopAllCoroutines();
            if (Devices != null && Devices.Any())
            {
                UpdateDeviceList(new List<Device>());
            }
            IsConnected = false;
        }

        private IEnumerator RunReceiveLoop()
        {
            while (websocket != null && websocket.State == WebSocketState.Open)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator OnOpened()
        {
            reconnecting = false;
            Logger.LogInfo("Connected to Intiface. Commencing handshake.");
            RequestServerInfo();
            yield break;
        }

        private IEnumerator OnClosed()
        {
            if (!reconnecting)
            {
                Logger.LogInfo(IsConnected
                    ? "Disconnected from Intiface."
                    : "Failed to connect to Intiface.");
            }
            CleanUp();
            StartCoroutine(Reconnect());
            yield break;
        }

        private IEnumerator OnMessageReceived(MessageReceivedEventArgs e)
        {
            try
            {
                foreach (JsonData data in JsonMapper.ToObject(e.Message))
                {
                    bool handled = CheckOkMsg(data)
                        || CheckErrorMsg(data)
                        || CheckServerInfoMsg(data)
                        || CheckDeviceAddedRemovedMsg(data)
                        || CheckDeviceListMsg(data)
                        || CheckBatteryLevelReadingMsg(data);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error processing message: {ex.Message}");
            }
            yield break;
        }

        private IEnumerator OnError(SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            if (!reconnecting)
            {
                Logger.LogWarning($"Websocket error: {e.Exception.Message}");
            }
            yield break;
        }

        private IEnumerator Reconnect()
        {
            if (!reconnecting)
            {
                Logger.LogInfo($"Attempting to reconnect every {ReconnectBackoffSecs} seconds...");
            }
            reconnecting = true;
            yield return new WaitForSecondsRealtime(ReconnectBackoffSecs);
            Open();
        }

        private bool CheckOkMsg(JsonData data) => data.ContainsKey("Ok");

        private bool CheckErrorMsg(JsonData data)
        {
            if (!data.ContainsKey("Error"))
                return false;
                
            Logger.LogWarning($"Error from Intiface: {data.ToJson()}");
            return true;
        }

        private bool CheckServerInfoMsg(JsonData data)
        {
            if (!data.ContainsKey("ServerInfo"))
                return false;
                
            if (IsConnected)
            {
                Logger.LogWarning("Ignoring handshake message, client already registered.");
                return true;
            }
            
            IsConnected = true;
            Logger.LogInfo("Handshake successful.");
            StartScan();
            RequestDeviceList();
            StartCoroutine(RunBatteryLoop());
            return true;
        }

        private bool CheckDeviceAddedRemovedMsg(JsonData data)
        {
            if (!data.ContainsKey("DeviceAdded") && !data.ContainsKey("DeviceRemoved"))
                return false;
                
            RequestDeviceList();
            return true;
        }

        private bool CheckDeviceListMsg(JsonData data)
        {
            if (!data.ContainsKey("DeviceList"))
                return false;
                
            try
            {
                var devices = new List<Device>();
                var deviceList = data["DeviceList"];
                
                // Check if DeviceList contains Devices key
                if (!deviceList.ContainsKey("Devices"))
                {
                    Logger.LogInfo("DeviceList message received but no Devices key found");
                    UpdateDeviceList(devices); // Update with empty list
                    return true;
                }
                
                var deviceArray = deviceList["Devices"];
                
                if (deviceArray != null && deviceArray.IsArray)
                {
                    for (int i = 0; i < deviceArray.Count; i++)
                    {
                        var deviceData = deviceArray[i];
                        
                        // Safely parse device data with null checks
                        var device = new Device();
                        
                        if (deviceData.ContainsKey("DeviceIndex"))
                            device.DeviceIndex = (int)deviceData["DeviceIndex"];
                            
                        if (deviceData.ContainsKey("DeviceDisplayName"))
                            device.DeviceDisplayName = deviceData["DeviceDisplayName"].ToString();
                            
                        if (deviceData.ContainsKey("DeviceName"))
                            device.Settings.DeviceName = deviceData["DeviceName"].ToString();
                        
                        // Parse device messages if available
                        if (deviceData.ContainsKey("DeviceMessages"))
                        {
                            var messages = deviceData["DeviceMessages"];
                            device.DeviceMessages = ParseDeviceMessages(messages);
                        }
                        
                        devices.Add(device);
                    }
                }
                else
                {
                    Logger.LogInfo("No device array found in DeviceList message");
                }
                
                UpdateDeviceList(devices);
                ReadBatteryLevels();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error parsing device list: {ex.Message}");
                Logger.LogError($"Raw message: {data.ToJson()}");
            }
            return true;
        }

        private Buttplug.DeviceMessage ParseDeviceMessages(JsonData messages)
        {
            var deviceMessage = new Buttplug.DeviceMessage();
            
            try
            {
                if (messages.ContainsKey("ScalarCmd"))
                {
                    var scalarArray = messages["ScalarCmd"];
                    var scalarFeatures = new List<Buttplug.Feature>();
                    if (scalarArray.IsArray)
                    {
                        for (int i = 0; i < scalarArray.Count; i++)
                        {
                            var scalar = scalarArray[i];
                            scalarFeatures.Add(new Buttplug.Feature
                            {
                                FeatureType = scalar["ActuatorType"].ToString(),
                                StepCount = (int)scalar["StepCount"]
                            });
                        }
                    }
                    deviceMessage.ScalarCmd = scalarFeatures.ToArray();
                }
                
                if (messages.ContainsKey("LinearCmd"))
                {
                    var linearArray = messages["LinearCmd"];
                    var linearFeatures = new List<Buttplug.Feature>();
                    if (linearArray.IsArray)
                    {
                        for (int i = 0; i < linearArray.Count; i++)
                        {
                            var linear = linearArray[i];
                            linearFeatures.Add(new Buttplug.Feature
                            {
                                FeatureType = "Linear",
                                StepCount = (int)linear["StepCount"]
                            });
                        }
                    }
                    deviceMessage.LinearCmd = linearFeatures.ToArray();
                }
                
                if (messages.ContainsKey("RotateCmd"))
                {
                    var rotateArray = messages["RotateCmd"];
                    var rotateFeatures = new List<Buttplug.Feature>();
                    if (rotateArray.IsArray)
                    {
                        for (int i = 0; i < rotateArray.Count; i++)
                        {
                            var rotate = rotateArray[i];
                            rotateFeatures.Add(new Buttplug.Feature
                            {
                                FeatureType = "Rotate",
                                StepCount = (int)rotate["StepCount"]
                            });
                        }
                    }
                    deviceMessage.RotateCmd = rotateFeatures.ToArray();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error parsing device messages: {ex.Message}");
            }
            
            return deviceMessage;
        }

        private bool CheckBatteryLevelReadingMsg(JsonData data)
        {
            if (!data.ContainsKey("SensorReading"))
                return false;
                
            try
            {
                var sensorType = data["SensorReading"]["SensorType"].ToString();
                if (sensorType != "Battery")
                    return false;
                    
                var deviceIndex = (int)data["SensorReading"]["DeviceIndex"];
                var batteryData = data["SensorReading"]["Data"];
                if (batteryData.IsArray && batteryData.Count > 0)
                {
                    float level = (float)(double)batteryData[0] / 100f;
                    var device = Devices.FirstOrDefault(d => d.DeviceIndex == deviceIndex);
                    if (device != null)
                    {
                        device.BatteryLevel = level;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error parsing battery reading: {ex.Message}");
            }
            return true;
        }

        private void UpdateDeviceList(List<Device> newDevices)
        {
            var before = Devices ?? new List<Device>();
            Devices = newDevices;
            OnDeviceListUpdated?.Invoke(this, new DeviceListEventArgs { Before = before, After = newDevices });
            
            Logger.LogInfo($"Device list updated. Found {newDevices.Count} devices.");
            foreach (var device in newDevices)
            {
                Logger.LogInfo($"  - {device.DeviceName} (Index: {device.DeviceIndex})");
            }
        }

        private void ReadBatteryLevels()
        {
            foreach (var device in Devices.Where(d => d.HasBatteryLevel))
            {
                BatteryLevelCmd(device);
            }
        }

        private IEnumerator RunBatteryLoop()
        {
            while (IsConnected)
            {
                yield return new WaitForSeconds(30f); // Check battery every 30 seconds
                ReadBatteryLevels();
            }
        }
    }
}
