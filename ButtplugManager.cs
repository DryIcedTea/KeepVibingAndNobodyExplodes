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
        
        private Dictionary<string, Coroutine> activeVibrationCoroutines = new Dictionary<string, Coroutine>();
        
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

            var vibratorFeatures = GetVibratorFeatures(device);
            foreach (var feature in vibratorFeatures)
            {
                client.VibrateCmd(feature, intensity);
            }

            if (duration > 0)
            {
                // Stop vibration after duration
                if (activeVibrationCoroutines.TryGetValue(deviceName, out var existingCoroutine))
                {
                    StopCoroutine(existingCoroutine);
                }
                var coroutine = StartCoroutine(StopVibrateAfterDelay(device, duration));
                activeVibrationCoroutines[deviceName] = coroutine;
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

        private System.Collections.IEnumerator StopVibrateAfterDelay(Device device, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            var vibratorFeatures = GetVibratorFeatures(device);
            foreach (var feature in vibratorFeatures)
            {
                client.VibrateCmd(feature, 0);
            }
            
            // Clean up the tracking dictionary
            activeVibrationCoroutines.Remove(device.DeviceName);
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
            foreach (var device in Devices)
            {
                logger.LogInfo($"  Device: {device.DeviceName} (Index: {device.DeviceIndex})");
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
