using BepInEx;
using BepInEx.Configuration;
using LoveMachine.Core.NonPortable;

namespace LoveMachine.Core.Config
{
    internal static class ConstrictConfig
    {
        public static IntensityConfigSettings IntensitySettings { get; private set; }

        internal static void Initialize(BaseUnityPlugin plugin)
        {
            int order = 1000;
            const string constrictSettingsTitle = "Pressure Settings";
            IntensitySettings = new IntensityConfigSettings(plugin, constrictSettingsTitle, ref order);
        }
    }
}