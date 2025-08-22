using System.Collections;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;

namespace KeepVibingAndNobodyExplodes;

[BepInPlugin("dryicedmatcha.keepvibing", "Keep Vibing And Nobody Explodes", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    private ButtplugManager buttplugManager;
    private static Plugin instance;
    
    private const string harmonyId = "com.yourname.ktane.hapticsmod";
    private static Harmony harmonyInstance;
    
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        
        instance = this;
        
        InitializeButtplug();
        
        ApplyHarmonyPatches();
    }
    
    private void ApplyHarmonyPatches()
    {
        if (harmonyInstance == null)
        {
            harmonyInstance = new Harmony(harmonyId);
        }
        
        harmonyInstance.PatchAll();
        Logger.LogInfo("Harmony patches applied.");
    }
    
    private void InitializeButtplug()
    {
        
        var buttplugObject = new GameObject("ButtplugManager");
        DontDestroyOnLoad(buttplugObject);
        
        
        buttplugManager = buttplugObject.AddComponent<ButtplugManager>();
        buttplugManager.Initialize(Logger);
        
        
        buttplugManager.OnDeviceListUpdated += (sender, args) =>
        {
            Logger.LogInfo($"Devices updated: {args.After.Count} devices connected");
            buttplugManager.LogDeviceInfo();
        };
        
        Logger.LogInfo("ButtplugManager initialized. Use 'buttplugManager.Connect()' to connect to Intiface.");
    }
    
    private void Start()
    {
        
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
    /// <param name="frameDelay">Number of frames to delay (0 for no delay)</param>
    public static void TriggerVibration(float power, float duration, int frameDelay = 0)
    {
        if (frameDelay > 0)
        {
            instance?.StartCoroutine(TriggerVibrationDelayed(power, duration, frameDelay));
        }
        else
        {
            instance?.VibrateAllDevices(power, duration);
        }
    }
    private static IEnumerator TriggerVibrationDelayed(float power, float duration, int frameDelay)
    {
        for (int i = 0; i < frameDelay; i++)
        {
            yield return null; // Wait for one frame
        }
        instance?.VibrateAllDevices(power, duration);
    }
}

/// <summary>
/// Vibration when regular wire is cut. WIRE MODULE
/// </summary>
[HarmonyPatch(typeof(SnippableWire), "Interact")]
public class WireSnipVibrationPatch
{
    [HarmonyPrefix]
    public static void Prefix(SnippableWire __instance, out bool __state)
    {
        __state = __instance.Snipped;
    }
    
    [HarmonyPostfix]
    public static void Postfix(SnippableWire __instance, bool __state)
    {
        
        bool wasSnippedBeforeInteract = __state;
        
        
        bool isSnippedAfterInteract = __instance.Snipped;

        
        if (!wasSnippedBeforeInteract && isSnippedAfterInteract)
        {
            Plugin.TriggerVibration(0.5f, 0.1f);
        }
    }
}

/// <summary>
/// Vibration when you get a strike.
/// </summary>
[HarmonyPatch(typeof(Bomb), "OnStrike")]
[HarmonyPriority(Priority.Low)]
public class StrikeVibrationPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        
        Plugin.TriggerVibration(0.7f, 0.5f, 1);
    }
}

/// <summary>
/// VIBRATION WHEN BOMB DETONATES
/// </summary>
[HarmonyPatch(typeof(Bomb), "Detonate")]
[HarmonyPriority(Priority.Last)]
public class ExplosionVibrationPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        Plugin.TriggerVibration(1.0f, 3.0f, 2);
    }
}

/// <summary>
/// Vibration when Module is Solved
/// </summary>
[HarmonyPatch(typeof(Bomb), "OnPass")]
public class ModuleSolveVibrationPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        Plugin.TriggerVibration(1.0f, 0.1f);
    }
}

/// <summary>
/// VIBRATION ON THE BUTTON MODULE
/// </summary>
[HarmonyPatch(typeof(PressableButton))]
public class BigButtonHapticPatch
{

    [HarmonyPostfix]
    [HarmonyPatch("Interact")]
    public static void PressPostfix()
    {
        Plugin.TriggerVibration(0.4f, 500.0f);
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("InteractEnded")]
    public static void ReleasePrefix(PressableButton __instance)
    {
        if (__instance.IsInteracting())
        {
            Plugin.TriggerVibration(0.5f, 0.1f);
        }
    }
}
