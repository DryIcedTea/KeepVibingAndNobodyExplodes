using BepInEx;
using System;
using System.Collections.Generic;

namespace KeepVibingAndNobodyExplodes.Buttplug
{
    public static class Buttplug
    {
        private static int NewId => UnityEngine.Random.Range(0, int.MaxValue);

        public static object RequestServerInfo() => new
        {
            RequestServerInfo = new
            {
                Id = NewId,
                ClientName = Paths.ProcessName,
                MessageVersion = 3
            }
        };

        public static object RequestDeviceList() => new
        {
            RequestDeviceList = new
            {
                Id = NewId
            }
        };

        public static object StartScan() => new
        {
            StartScanning = new
            {
                Id = NewId
            }
        };

        public static object StopScan() => new
        {
            StopScanning = new
            {
                Id = NewId
            }
        };

        public static object StopDeviceCmd(Device device) => new
        {
            StopDeviceCmd = new
            {
                Id = NewId,
                DeviceIndex = device.DeviceIndex
            }
        };

        public static object StopAllDevices() => new
        {
            StopAllDevices = new
            {
                Id = NewId
            }
        };

        public static object LinearCmd(Device device, int featureIndex, float position, float durationSecs) => new
        {
            LinearCmd = new
            {
                Id = NewId,
                DeviceIndex = device.DeviceIndex,
                Vectors = new[] {
                    new
                    {
                        Index = featureIndex,
                        Duration = (int)(durationSecs * 1000f),
                        Position = position
                    }
                }
            }
        };

        public static object ScalarCmd(Device device, int featureIndex, float value, string actuatorType) => new
        {
            ScalarCmd = new
            {
                Id = NewId,
                DeviceIndex = device.DeviceIndex,
                Scalars = new[]
                {
                    new
                    {
                        Index = featureIndex,
                        Scalar = value,
                        ActuatorType = actuatorType
                    }
                }
            }
        };

        public static object RotateCmd(Device device, int featureIndex, float speed, bool clockwise) => new
        {
            RotateCmd = new
            {
                Id = NewId,
                DeviceIndex = device.DeviceIndex,
                Rotations = new[]
                {
                    new
                    {
                        Index = featureIndex,
                        Speed = speed,
                        Clockwise = clockwise
                    }
                }
            }
        };

        public static object SensorReadCmd(Device device, int sensorIndex, string sensorType) => new
        {
            SensorReadCmd = new
            {
                Id = NewId,
                DeviceIndex = device.DeviceIndex,
                SensorIndex = sensorIndex,
                SensorType = sensorType
            }
        };

        public class Feature
        {
            public string FeatureType { get; set; }
            public int StepCount { get; set; }
            
            public bool IsVibrator => FeatureType == "Vibrate";
            public bool IsConstrictor => FeatureType == "Constrict";
            public bool IsOscillator => FeatureType == "Oscillate";
            public bool HasBatteryLevel => FeatureType == "Battery";
        }

        public class DeviceMessage
        {
            public Feature[] LinearCmd { get; set; } = new Feature[0];
            public Feature[] RotateCmd { get; set; } = new Feature[0];
            public Feature[] ScalarCmd { get; set; } = new Feature[0];
            public Feature[] SensorReadCmd { get; set; } = new Feature[0];
        }
    }
}
