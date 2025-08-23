using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BepInEx.Logging;
using KeepVibingAndNobodyExplodes.Buttplug;
using KeepVibingAndNobodyExplodes.Buttplug.Settings;

namespace KeepVibingAndNobodyExplodes
{
    public class ButtplugManager : MonoBehaviour
    {
        private ButtplugWsClient client;
        private ManualLogSource logger;
        
        private Dictionary<int, Coroutine> activeVibrationCoroutines = new Dictionary<int, Coroutine>();
        
        public bool IsConnected => client != null && client.IsConnected;
        public List<Device> Devices => client?.Devices ?? new List<Device>();
        
        public event EventHandler<DeviceListEventArgs> OnDeviceListUpdated;

        public void Initialize(ManualLogSource logSource)
        {
            logger = logSource;
            
            
            client = gameObject.AddComponent<ButtplugWsClient>();
            client.OnDeviceListUpdated += (sender, args) =>
            {
                logger.LogInfo($"Device list updated: {args.After.Count} devices found");
                OnDeviceListUpdated?.Invoke(sender, args);
            };
        }

        public void Connect()
        {
            if (client != null)
            {
                client.Connect(Plugin.IntifaceHost, Plugin.IntifacePort);
            }
        }

        public void Disconnect()
        {
            if (client != null)
            {
                client.Close();
            }
        }

        public void StartScanning()
        {
            if (client != null && client.IsConnected)
            {
                client.StartScan();
                logger.LogInfo("Started scanning for devices");
            }
            else
            {
                logger.LogWarning("Cannot start scanning - not connected to Intiface");
            }
        }

        public void StopAllDevices()
        {
            if (client != null)
            {
                client.StopAllDevices();
                logger.LogInfo("Stopped all devices");
            }
        }

        public void VibrateDevice(string deviceName, float intensity, float duration = 0)
        {
            var device = Devices.FirstOrDefault(d => d.DeviceName.Contains(deviceName));
            if (device == null)
            {
                logger.LogWarning($"Device not found: {deviceName}");
                return;
            }

            // Find the list index for this device
            int listIndex = Devices.IndexOf(device);
            
            var vibratorFeatures = GetVibratorFeatures(device);
            foreach (var feature in vibratorFeatures)
            {
                client.VibrateCmd(feature, intensity);
            }

            if (duration > 0)
            {
                // Stop vibration after duration using list index
                if (activeVibrationCoroutines.TryGetValue(listIndex, out var existingCoroutine))
                {
                    StopCoroutine(existingCoroutine);
                }
                var coroutine = StartCoroutine(StopVibrateAfterDelay(listIndex, duration));
                activeVibrationCoroutines[listIndex] = coroutine;
            }
        }

        public void VibrateDeviceByIndex(int deviceIndex, float intensity, float duration = 0)
        {
            if (deviceIndex < 0 || deviceIndex >= Devices.Count)
            {
                logger.LogWarning($"Device index out of range: {deviceIndex}. Available devices: {Devices.Count}");
                return;
            }

            var device = Devices[deviceIndex];
            logger.LogInfo($"Vibrating device {deviceIndex}: {device.DeviceName}");

            var vibratorFeatures = GetVibratorFeatures(device);
            foreach (var feature in vibratorFeatures)
            {
                client.VibrateCmd(feature, intensity);
            }

            if (duration > 0)
            {
                // Stop vibration after duration using list index (deviceIndex)
                if (activeVibrationCoroutines.TryGetValue(deviceIndex, out var existingCoroutine))
                {
                    StopCoroutine(existingCoroutine);
                }
                var coroutine = StartCoroutine(StopVibrateAfterDelay(deviceIndex, duration));
                activeVibrationCoroutines[deviceIndex] = coroutine;
            }
        }

        public void StrokeDevice(string deviceName, float position, float duration)
        {
            var device = Devices.FirstOrDefault(d => d.DeviceName.Contains(deviceName));
            if (device == null)
            {
                logger.LogWarning($"Device not found: {deviceName}");
                return;
            }

            var strokerFeatures = GetStrokerFeatures(device);
            foreach (var feature in strokerFeatures)
            {
                client.LinearCmd(feature, position, duration);
            }
        }

        public void StrokeDeviceByIndex(int deviceIndex, float position, float duration)
        {
            if (deviceIndex < 0 || deviceIndex >= Devices.Count)
            {
                logger.LogWarning($"Device index out of range: {deviceIndex}. Available devices: {Devices.Count}");
                return;
            }

            var device = Devices[deviceIndex];
            logger.LogInfo($"Stroking device {deviceIndex}: {device.DeviceName}");

            var strokerFeatures = GetStrokerFeatures(device);
            foreach (var feature in strokerFeatures)
            {
                client.LinearCmd(feature, position, duration);
            }
        }

        private System.Collections.IEnumerator StopVibrateAfterDelay(int deviceListIndex, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Verify the device still exists at this index
            if (deviceListIndex >= 0 && deviceListIndex < Devices.Count)
            {
                var device = Devices[deviceListIndex];
                var vibratorFeatures = GetVibratorFeatures(device);
                foreach (var feature in vibratorFeatures)
                {
                    client.VibrateCmd(feature, 0);
                }
            }
            
            // Clean up the tracking dictionary using list index
            activeVibrationCoroutines.Remove(deviceListIndex);
        }

        private List<DeviceFeature> GetVibratorFeatures(Device device)
        {
            var features = new List<DeviceFeature>();
            for (int i = 0; i < device.DeviceMessages.ScalarCmd.Length; i++)
            {
                var feature = device.DeviceMessages.ScalarCmd[i];
                if (feature.IsVibrator)
                {
                    var settings = i < device.Settings.ScalarCmdSettings.Length 
                        ? device.Settings.ScalarCmdSettings[i] 
                        : new FeatureSettings();
                    features.Add(new DeviceFeature(device, i, feature, settings));
                }
            }
            return features;
        }

        private List<DeviceFeature> GetStrokerFeatures(Device device)
        {
            var features = new List<DeviceFeature>();
            for (int i = 0; i < device.DeviceMessages.LinearCmd.Length; i++)
            {
                var feature = device.DeviceMessages.LinearCmd[i];
                var settings = i < device.Settings.LinearCmdSettings.Length 
                    ? device.Settings.LinearCmdSettings[i] 
                    : new FeatureSettings();
                features.Add(new DeviceFeature(device, i, feature, settings));
            }
            return features;
        }

        public void LogDeviceInfo()
        {
            if (!IsConnected)
            {
                logger.LogInfo("Not connected to Intiface server");
                return;
            }

            logger.LogInfo($"Connected devices: {Devices.Count}");
            for (int i = 0; i < Devices.Count; i++)
            {
                var device = Devices[i];
                logger.LogInfo($"  Device {i}: {device.DeviceName} (DeviceIndex: {device.DeviceIndex})");
                logger.LogInfo($"    Vibrators: {device.DeviceMessages.ScalarCmd.Count(f => f.IsVibrator)}");
                logger.LogInfo($"    Strokers: {device.DeviceMessages.LinearCmd.Length}");
                logger.LogInfo($"    Rotators: {device.DeviceMessages.RotateCmd.Length}");
                if (device.HasBatteryLevel)
                {
                    logger.LogInfo($"    Battery: {device.BatteryLevel * 100:F0}%");
                }
            }
        }
    }
}
