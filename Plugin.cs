using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;

namespace KeepVibingAndNobodyExplodes;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    private ButtplugManager buttplugManager;
    private static Plugin instance;
    
    // Harmony instance
    private const string harmonyId = "com.yourname.ktane.hapticsmod";
    private static Harmony harmonyInstance;
    
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        
        // Set instance for static access
        instance = this;
        
        // Initialize Buttplug manager
        InitializeButtplug();
        
        // Apply Harmony patches
        ApplyHarmonyPatches();
    }
    
    private void ApplyHarmonyPatches()
    {
        if (harmonyInstance == null)
        {
            harmonyInstance = new Harmony(harmonyId);
        }
        
        // This will find all classes in your assembly with HarmonyPatch attributes and apply them.
        harmonyInstance.PatchAll();
        Logger.LogInfo("Harmony patches applied.");
    }
    
    private void InitializeButtplug()
    {
        // Create a GameObject to host the ButtplugManager
        var buttplugObject = new GameObject("ButtplugManager");
        DontDestroyOnLoad(buttplugObject);
        
        // Add and initialize the ButtplugManager
        buttplugManager = buttplugObject.AddComponent<ButtplugManager>();
        buttplugManager.Initialize(Logger);
        
        // Set up event handlers
        buttplugManager.OnDeviceListUpdated += (sender, args) =>
        {
            Logger.LogInfo($"Devices updated: {args.After.Count} devices connected");
            buttplugManager.LogDeviceInfo();
        };
        
        Logger.LogInfo("ButtplugManager initialized. Use 'buttplugManager.Connect()' to connect to Intiface.");
    }
    
    private void Start()
    {
        // Auto-connect to Intiface server on start (optional)
        if (buttplugManager != null)
        {
            Logger.LogInfo("Attempting to connect to Intiface server...");
            buttplugManager.Connect();
        }
    }
    
    public void TestVibration()
    {
        if (buttplugManager != null && buttplugManager.IsConnected)
        {
            // Test vibration on first available device
            var devices = buttplugManager.Devices;
            if (devices.Count > 0)
            {
                Logger.LogInfo("Testing vibration for 2 seconds...");
                buttplugManager.VibrateDevice(devices[0].DeviceName, 0.5f, 2.0f);
            }
            else
            {
                Logger.LogInfo("No devices available for testing");
            }
        }
        else
        {
            Logger.LogInfo("Not connected to Intiface server");
        }
    }
    
    /// <summary>
    /// Vibrates all connected vibrator devices
    /// </summary>
    /// <param name="power">Vibration power (0.0 to 1.0)</param>
    /// <param name="duration">Duration in seconds</param>
    public void VibrateAllDevices(float power, float duration)
    {
        if (buttplugManager != null && buttplugManager.IsConnected)
        {
            var devices = buttplugManager.Devices;
            if (devices.Count > 0)
            {
                Logger.LogInfo($"Vibrating {devices.Count} devices at {power} power for {duration} seconds");
                foreach (var device in devices)
                {
                    buttplugManager.VibrateDevice(device.DeviceName, power, duration);
                }
            }
            else
            {
                Logger.LogInfo("No devices available for vibration");
            }
        }
        else
        {
            Logger.LogInfo("Not connected to Intiface server");
        }
    }
    
    /// <summary>
    /// Static access to vibrate devices from Harmony patches
    /// </summary>
    /// <param name="power">Vibration power (0.0 to 1.0)</param>
    /// <param name="duration">Duration in seconds</param>
    public static void TriggerVibration(float power, float duration)
    {
        instance?.VibrateAllDevices(power, duration);
    }
}

/// <summary>
/// This class contains the patch for the SnippableWire's Interact method.
/// </summary>
[HarmonyPatch(typeof(SnippableWire), "Interact")]
public class WireSnipHapticPatch
{
    /// <summary>
    /// This is the Harmony Prefix. It runs *before* the original Interact method.
    /// We use it to save the state of the wire before it's cut.
    /// </summary>
    /// <param name="__instance">The instance of the SnippableWire being interacted with.</param>
    /// <param name="__state">A special Harmony parameter to pass data from Prefix to Postfix.</param>
    [HarmonyPrefix]
    public static void Prefix(SnippableWire __instance, out bool __state)
    {
        // Store the "snipped" status of the wire *before* the Interact method runs.
        // This will be 'false' if the wire is about to be cut.
        __state = __instance.Snipped;
    }

    /// <summary>
    /// This is the Harmony Postfix. It runs *after* the original Interact method.
    /// </summary>
    /// <param name="__instance">The instance of the SnippableWire that was interacted with.</param>
    /// <param name="__state">The data we saved in the Prefix method.</param>
    [HarmonyPostfix]
    public static void Postfix(SnippableWire __instance, bool __state)
    {
        // Retrieve the state from before the method ran.
        bool wasSnippedBeforeInteract = __state;
        
        // Get the state *after* the method has run.
        bool isSnippedAfterInteract = __instance.Snipped;

        // We only want to fire the haptic if the state changed from not-snipped to snipped.
        if (!wasSnippedBeforeInteract && isSnippedAfterInteract)
        {
            // Vibrate all devices at 0.5 power for 0.6 seconds when wire is cut
            Plugin.TriggerVibration(0.5f, 0.6f);
        }
    }
}
