namespace KeepVibingAndNobodyExplodes.Buttplug.Settings
{
    public class DeviceSettings
    {
        public string DeviceName { get; set; } = "";
        public FeatureSettings GlobalFeatureSettings { get; set; } = new FeatureSettings();
        
        // Device-specific settings
        public StrokerSettings StrokerSettings { get; set; }
        public VibratorSettings VibratorSettings { get; set; }
        public OscillatorSettings OscillatorSettings { get; set; }
        public ConstrictSettings ConstrictSettings { get; set; }
        
        // Feature command settings
        public FeatureSettings[] LinearCmdSettings { get; set; } = new FeatureSettings[0];
        public FeatureSettings[] RotateCmdSettings { get; set; } = new FeatureSettings[0];
        public FeatureSettings[] ScalarCmdSettings { get; set; } = new FeatureSettings[0];
    }
}

