using KeepVibingAndNobodyExplodes.Buttplug.Settings;

namespace KeepVibingAndNobodyExplodes.Buttplug
{
    public class DeviceFeature
    {
        public Device Device { get; set; }
        public int FeatureIndex { get; set; }
        public Buttplug.Feature Feature { get; set; }
        public FeatureSettings Settings { get; set; }

        public DeviceFeature(Device device, int featureIndex, Buttplug.Feature feature, FeatureSettings settings)
        {
            Device = device;
            FeatureIndex = featureIndex;
            Feature = feature;
            Settings = settings;
        }

        public string DisplayName => $"{Device.DeviceName} - {Feature.FeatureType} {FeatureIndex}";

        public bool IsVibrator => Feature.IsVibrator;
        public bool IsConstrictor => Feature.IsConstrictor;
        public bool IsOscillator => Feature.IsOscillator;
    }
}
