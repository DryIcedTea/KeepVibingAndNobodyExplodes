using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Assets.Scripts.Props;
using DarkTonic.MasterAudio;
using BepInEx;
using BepInEx.Configuration;
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
    
    private const string harmonyId = "com.dryicedmatcha.ktane.keepvibing";
    private static Harmony harmonyInstance;
    
    // Configuration entries - individual module controls
    private ConfigEntry<bool> enableWireVibration;
    private ConfigEntry<float> wireVibrationStrength;
    
    // Intiface connection settings
    private ConfigEntry<string> intifaceHost;
    private ConfigEntry<int> intifacePort;
    
    private ConfigEntry<bool> enableStrikeVibration;
    private ConfigEntry<float> strikeVibrationStrength;
    
    private ConfigEntry<bool> enableExplosionVibration;
    private ConfigEntry<float> explosionVibrationStrength;
    
    private ConfigEntry<bool> enableModuleSolveVibration;
    private ConfigEntry<float> moduleSolveVibrationStrength;
    
    private ConfigEntry<bool> enableButtonVibration;
    private ConfigEntry<float> buttonPressVibrationStrength;
    private ConfigEntry<float> buttonReleaseVibrationStrength;
    
    private ConfigEntry<bool> enableKeypadVibration;
    private ConfigEntry<float> keypadVibrationBaseStrength;
    
    private ConfigEntry<bool> enableSimonVibration;
    private ConfigEntry<float> simonVibrationBaseStrength;
    
    private ConfigEntry<bool> enableWhosOnFirstVibration;
    private ConfigEntry<float> whosOnFirstVibrationBaseStrength;
    
    private ConfigEntry<bool> enableMemoryVibration;
    private ConfigEntry<float> memoryVibrationBaseStrength;
    
    private ConfigEntry<bool> enableMorseVibration;
    private ConfigEntry<float> morseBlinkVibrationStrength;
    private ConfigEntry<float> morseScrollVibrationStrength;
    
    private ConfigEntry<bool> enableComplicatedWireVibration;
    private ConfigEntry<float> complicatedWireVibrationStrength;
    
    private ConfigEntry<bool> enableWireSequenceVibration;
    private ConfigEntry<float> wireSequenceWireVibrationStrength;
    private ConfigEntry<float> wireSequenceStageVibrationBaseStrength;
    
    private ConfigEntry<bool> enableMazeVibration;
    private ConfigEntry<float> mazeVibrationStrength;
    
    private ConfigEntry<bool> enablePasswordVibration;
    private ConfigEntry<float> passwordVibrationStrength;
    
    private ConfigEntry<bool> enableCapacitorDischargeVibration;
    private ConfigEntry<float> capacitorDischargePushVibrationStrength;
    private ConfigEntry<float> capacitorDischargeReleaseVibrationStrength;
    
    private ConfigEntry<bool> enableNeedyKnobVibration;
    private ConfigEntry<float> needyKnobVibrationStrength;
    
    private ConfigEntry<bool> enableVentGasVibration;
    private ConfigEntry<float> ventGasVibrationStrength;
    
    private ConfigEntry<bool> enableAlarmClockVibration;
    private ConfigEntry<float> alarmClockVibrationStrength;
    
    // Module-specific accessors
    public static bool EnableWireVibration => instance?.enableWireVibration?.Value ?? true;
    public static float WireVibrationStrength => instance?.wireVibrationStrength?.Value ?? 0.5f;
    
    public static bool EnableStrikeVibration => instance?.enableStrikeVibration?.Value ?? true;
    public static float StrikeVibrationStrength => instance?.strikeVibrationStrength?.Value ?? 0.7f;
    
    public static bool EnableExplosionVibration => instance?.enableExplosionVibration?.Value ?? true;
    public static float ExplosionVibrationStrength => instance?.explosionVibrationStrength?.Value ?? 1.0f;
    
    public static bool EnableModuleSolveVibration => instance?.enableModuleSolveVibration?.Value ?? true;
    public static float ModuleSolveVibrationStrength => instance?.moduleSolveVibrationStrength?.Value ?? 1.0f;
    
    public static bool EnableButtonVibration => instance?.enableButtonVibration?.Value ?? true;
    public static float ButtonPressVibrationStrength => instance?.buttonPressVibrationStrength?.Value ?? 0.4f;
    public static float ButtonReleaseVibrationStrength => instance?.buttonReleaseVibrationStrength?.Value ?? 0.5f;
    
    public static bool EnableKeypadVibration => instance?.enableKeypadVibration?.Value ?? true;
    public static float KeypadVibrationBaseStrength => instance?.keypadVibrationBaseStrength?.Value ?? 0.25f;
    
    public static bool EnableSimonVibration => instance?.enableSimonVibration?.Value ?? true;
    public static float SimonVibrationBaseStrength => instance?.simonVibrationBaseStrength?.Value ?? 0.2f;
    
    public static bool EnableWhosOnFirstVibration => instance?.enableWhosOnFirstVibration?.Value ?? true;
    public static float WhosOnFirstVibrationBaseStrength => instance?.whosOnFirstVibrationBaseStrength?.Value ?? 0.25f;
    
    public static bool EnableMemoryVibration => instance?.enableMemoryVibration?.Value ?? true;
    public static float MemoryVibrationBaseStrength => instance?.memoryVibrationBaseStrength?.Value ?? 0.2f;
    
    public static bool EnableMorseVibration => instance?.enableMorseVibration?.Value ?? true;
    public static float MorseBlinkVibrationStrength => instance?.morseBlinkVibrationStrength?.Value ?? 0.4f;
    public static float MorseScrollVibrationStrength => instance?.morseScrollVibrationStrength?.Value ?? 0.2f;
    
    public static bool EnableComplicatedWireVibration => instance?.enableComplicatedWireVibration?.Value ?? true;
    public static float ComplicatedWireVibrationStrength => instance?.complicatedWireVibrationStrength?.Value ?? 0.5f;
    
    public static bool EnableWireSequenceVibration => instance?.enableWireSequenceVibration?.Value ?? true;
    public static float WireSequenceWireVibrationStrength => instance?.wireSequenceWireVibrationStrength?.Value ?? 0.5f;
    public static float WireSequenceStageVibrationBaseStrength => instance?.wireSequenceStageVibrationBaseStrength?.Value ?? 0.2f;
    
    public static bool EnableMazeVibration => instance?.enableMazeVibration?.Value ?? true;
    public static float MazeVibrationStrength => instance?.mazeVibrationStrength?.Value ?? 0.3f;
    
    public static bool EnablePasswordVibration => instance?.enablePasswordVibration?.Value ?? true;
    public static float PasswordVibrationStrength => instance?.passwordVibrationStrength?.Value ?? 0.3f;
    
    public static bool EnableCapacitorDischargeVibration => instance?.enableCapacitorDischargeVibration?.Value ?? true;
    public static float CapacitorDischargePushVibrationStrength => instance?.capacitorDischargePushVibrationStrength?.Value ?? 0.7f;
    public static float CapacitorDischargeReleaseVibrationStrength => instance?.capacitorDischargeReleaseVibrationStrength?.Value ?? 0.1f;
    
    public static bool EnableNeedyKnobVibration => instance?.enableNeedyKnobVibration?.Value ?? true;
    public static float NeedyKnobVibrationStrength => instance?.needyKnobVibrationStrength?.Value ?? 0.2f;
    
    public static bool EnableVentGasVibration => instance?.enableVentGasVibration?.Value ?? true;
    public static float VentGasVibrationStrength => instance?.ventGasVibrationStrength?.Value ?? 0.5f;
    
    public static bool EnableAlarmClockVibration => instance?.enableAlarmClockVibration?.Value ?? true;
    public static float AlarmClockVibrationStrength => instance?.alarmClockVibrationStrength?.Value ?? 1.0f;
    
    // Intiface connection accessors
    public static string IntifaceHost => instance?.intifaceHost?.Value ?? "127.0.0.1";
    public static int IntifacePort => instance?.intifacePort?.Value ?? 12345;
    
    private void Awake()
    {
        // Initialize configuration
        enableWireVibration = Config.Bind("Wire", 
                                     "EnableVibration", 
                                     true, 
                                     "Enable or disable vibrations for the Wire module");
        
        wireVibrationStrength = Config.Bind("Wire", 
                                                 "VibrationStrength", 
                                                 0.5f, 
                                                 "Strength of the vibration for the Wire module (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        enableStrikeVibration = Config.Bind("General", 
                                     "EnableStrikeVibration", 
                                     true, 
                                     "Enable or disable vibrations when the bomb receives a strike");
        
        strikeVibrationStrength = Config.Bind("General", 
                                                 "StrikeVibrationStrength", 
                                                 0.7f, 
                                                 "Strength of the vibration when the bomb receives a strike (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        enableExplosionVibration = Config.Bind("General", 
                                     "EnableExplosionVibration", 
                                     true, 
                                     "Enable or disable vibrations when the bomb explodes");
        
        explosionVibrationStrength = Config.Bind("General", 
                                                 "ExplosionVibrationStrength", 
                                                 1.0f, 
                                                 "Strength of the vibration when the bomb explodes (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        enableModuleSolveVibration = Config.Bind("General", 
                                     "EnableModuleSolveVibration", 
                                     true, 
                                     "Enable or disable vibrations when a module is solved");
        
        moduleSolveVibrationStrength = Config.Bind("General", 
                                                 "ModuleSolveVibrationStrength", 
                                                 1.0f, 
                                                 "Strength of the vibration when a module is solved (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        enableButtonVibration = Config.Bind("Button", 
                                     "EnableVibration", 
                                     true, 
                                     "Enable or disable vibrations for button presses");
        
        buttonPressVibrationStrength = Config.Bind("Button", 
                                                 "PressVibrationStrength", 
                                                 0.4f, 
                                                 "Strength of the vibration when a button is pressed (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        buttonReleaseVibrationStrength = Config.Bind("Button", 
                                                 "ReleaseVibrationStrength", 
                                                 0.5f, 
                                                 "Strength of the vibration when a button is released (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        enableKeypadVibration = Config.Bind("Keypad", 
                                     "EnableVibration", 
                                     true, 
                                     "Enable or disable vibrations for the Keypad module");
        
        keypadVibrationBaseStrength = Config.Bind("Keypad", 
                                                 "VibrationBaseStrength", 
                                                 0.25f, 
                                                 "Base strength of the vibration for the Keypad module (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        enableSimonVibration = Config.Bind("Simon", 
                                     "EnableVibration", 
                                     true, 
                                     "Enable or disable vibrations for the Simon Says module");
        
        simonVibrationBaseStrength = Config.Bind("Simon", 
                                                 "VibrationBaseStrength", 
                                                 0.2f, 
                                                 "Base strength of the vibration for the Simon Says module (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        enableWhosOnFirstVibration = Config.Bind("WhosOnFirst", 
                                     "EnableVibration", 
                                     true, 
                                     "Enable or disable vibrations for the Who's On First module");
        
        whosOnFirstVibrationBaseStrength = Config.Bind("WhosOnFirst", 
                                                 "VibrationBaseStrength", 
                                                 0.25f, 
                                                 "Base strength of the vibration for the Who's On First module (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        enableMemoryVibration = Config.Bind("Memory", 
                                     "EnableVibration", 
                                     true, 
                                     "Enable or disable vibrations for the Memory module");
        
        memoryVibrationBaseStrength = Config.Bind("Memory", 
                                                 "VibrationBaseStrength", 
                                                 0.2f, 
                                                 "Base strength of the vibration for the Memory module (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        enableMorseVibration = Config.Bind("Morse", 
                                     "EnableVibration", 
                                     true, 
                                     "Enable or disable vibrations for the Morse Code module");
        
        morseBlinkVibrationStrength = Config.Bind("Morse", 
                                                 "BlinkVibrationStrength", 
                                                 0.4f, 
                                                 "Strength of the vibration for the Morse Code module when the light blinks (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        morseScrollVibrationStrength = Config.Bind("Morse", 
                                                 "ScrollVibrationStrength", 
                                                 0.2f, 
                                                 "Strength of the vibration for scrolling through frequencies in the Morse Code module (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        enableComplicatedWireVibration = Config.Bind("ComplicatedWire", 
                                     "EnableVibration", 
                                     true, 
                                     "Enable or disable vibrations for the Complicated Wire module");
        
        complicatedWireVibrationStrength = Config.Bind("ComplicatedWire", 
                                                 "VibrationStrength", 
                                                 0.5f, 
                                                 "Strength of the vibration for the Complicated Wire module (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        enableWireSequenceVibration = Config.Bind("WireSequence", 
                                     "EnableVibration", 
                                     true, 
                                     "Enable or disable vibrations for the Wire Sequence module");
        
        wireSequenceWireVibrationStrength = Config.Bind("WireSequence", 
                                                 "WireVibrationStrength", 
                                                 0.5f, 
                                                 "Strength of the vibration for wire snips in the Wire Sequence module (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        wireSequenceStageVibrationBaseStrength = Config.Bind("WireSequence", 
                                                 "StageVibrationBaseStrength", 
                                                 0.2f, 
                                                 "Base strength of the vibration for stage transitions in the Wire Sequence module (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        enableMazeVibration = Config.Bind("Maze", 
                                     "EnableVibration", 
                                     true, 
                                     "Enable or disable vibrations for the Maze module");
        
        mazeVibrationStrength = Config.Bind("Maze", 
                                                 "VibrationStrength", 
                                                 0.3f, 
                                                 "Strength of the vibration for the Maze module (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        enablePasswordVibration = Config.Bind("Password", 
                                     "EnableVibration", 
                                     true, 
                                     "Enable or disable vibrations for the Password module");
        
        passwordVibrationStrength = Config.Bind("Password", 
                                                 "VibrationStrength", 
                                                 0.3f, 
                                                 "Strength of the vibration for the Password module (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        enableCapacitorDischargeVibration = Config.Bind("CapacitorDischarge", 
                                     "EnableVibration", 
                                     true, 
                                     "Enable or disable vibrations for the Capacitor Discharge module");
        
        capacitorDischargePushVibrationStrength = Config.Bind("CapacitorDischarge", 
                                                 "PushVibrationStrength", 
                                                 0.7f, 
                                                 "Strength of the vibration when pushing the capacitor in the Capacitor Discharge module (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        capacitorDischargeReleaseVibrationStrength = Config.Bind("CapacitorDischarge", 
                                                 "ReleaseVibrationStrength", 
                                                 0.1f, 
                                                 "Strength of the vibration when releasing the capacitor in the Capacitor Discharge module (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        enableNeedyKnobVibration = Config.Bind("NeedyKnob", 
                                     "EnableVibration", 
                                     true, 
                                     "Enable or disable vibrations for the Needy Knob module");
        
        needyKnobVibrationStrength = Config.Bind("NeedyKnob", 
                                                 "VibrationStrength", 
                                                 0.2f, 
                                                 "Strength of the vibration for the Needy Knob module (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        enableVentGasVibration = Config.Bind("VentGas", 
                                     "EnableVibration", 
                                     true, 
                                     "Enable or disable vibrations for the Venting Gas module");
        
        ventGasVibrationStrength = Config.Bind("VentGas", 
                                                 "VibrationStrength", 
                                                 0.5f, 
                                                 "Strength of the vibration for the Venting Gas module (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        enableAlarmClockVibration = Config.Bind("AlarmClock", 
                                     "EnableVibration", 
                                     true, 
                                     "Enable or disable vibrations for the Alarm Clock module");
        
        alarmClockVibrationStrength = Config.Bind("AlarmClock", 
                                                 "VibrationStrength", 
                                                 1.0f, 
                                                 "Strength of the vibration for the Alarm Clock module (0.1 = 10% strength, 1.0 = 100% strength)" );
        
        intifaceHost = Config.Bind("Intiface", 
                                     "Host", 
                                     "127.0.0.1", 
                                     "Host address for Intiface server");
        
        intifacePort = Config.Bind("Intiface", 
                                     "Port", 
                                     12345, 
                                     "Port number for Intiface server");
        
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {"Keep Vibing and Nobody Explodes"} is loaded!");
        
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
    
    private void Update()
    {
        // Check for K key press to trigger reconnect
        if (Input.GetKeyDown(KeyCode.K))
        {
            Logger.LogInfo("K key pressed - attempting to reconnect to Buttplug...");
            ReconnectButtplug();
        }
        
        // Check for P key press to send test vibration pulse
        if (Input.GetKeyDown(KeyCode.P))
        {
            Logger.LogInfo("P key pressed - stopping vibratons");
            VibrateAllDevices(0.1f, 0.05f);
        }
    }
    
    private void ReconnectButtplug()
    {
        if (buttplugManager == null)
        {
            Logger.LogWarning("ButtplugManager is not initialized!");
            return;
        }
        
        Logger.LogInfo("Disconnecting from Buttplug server...");
        buttplugManager.Disconnect();
        
        
        StartCoroutine(ReconnectAfterDelay(1.0f));
    }
    
    private System.Collections.IEnumerator ReconnectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        Logger.LogInfo("Attempting to reconnect to Buttplug server...");
        buttplugManager.Connect();
    }
    
    public void TestVibration()
    {
        if (buttplugManager != null && buttplugManager.IsConnected)
        {
            
            var devices = buttplugManager.Devices;
            if (devices.Count > 0)
            {
                Logger.LogInfo("Testing vibration for 2 seconds...");
                buttplugManager.VibrateDeviceByIndex(0, 0.5f, 2.0f);
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
        // Power is now set directly by the patches, no multiplier needed
        float clampedPower = Mathf.Clamp01(power);
        
        if (buttplugManager != null && buttplugManager.IsConnected)
        {
            var devices = buttplugManager.Devices;
            if (devices.Count > 0)
            {
                Logger.LogInfo($"Vibrating {devices.Count} devices at {clampedPower} power for {duration} seconds");
                for (int i = 0; i < devices.Count; i++)
                {
                    buttplugManager.VibrateDeviceByIndex(i, clampedPower, duration);
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
        // No global check needed, patches handle their own enable/disable
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
///
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
        if (!Plugin.EnableWireVibration) return;
        
        bool wasSnippedBeforeInteract = __state;
        bool isSnippedAfterInteract = __instance.Snipped;

        if (!wasSnippedBeforeInteract && isSnippedAfterInteract)
        {
            Plugin.TriggerVibration(Plugin.WireVibrationStrength, 0.1f);
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
        if (!Plugin.EnableStrikeVibration) return;
        
        Plugin.TriggerVibration(Plugin.StrikeVibrationStrength, 0.5f, 1);
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
        if (!Plugin.EnableExplosionVibration) return;
        
        Plugin.TriggerVibration(Plugin.ExplosionVibrationStrength, 3.0f, 2);
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
        if (!Plugin.EnableModuleSolveVibration) return;
        
        Plugin.TriggerVibration(Plugin.ModuleSolveVibrationStrength, 0.1f);
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
        if (!Plugin.EnableButtonVibration) return;
        
        Plugin.TriggerVibration(Plugin.ButtonPressVibrationStrength, 500.0f);
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("InteractEnded")]
    public static void ReleasePrefix(PressableButton __instance)
    {
        if (!Plugin.EnableButtonVibration) return;
        
        if (__instance.IsInteracting())
        {
            Plugin.TriggerVibration(Plugin.ButtonReleaseVibrationStrength, 0.1f);
        }
    }
}

/// <summary>
/// VIBRATION ON KEYPAD MODULE AKA SYMBOLS MODULE
/// </summary>
[HarmonyPatch(typeof(KeypadComponent), "ButtonDown")]
public class KeypadHapticPatch
{
    
    //Check how many buttons are alredy pressed
    [HarmonyPrefix]
    public static void Prefix(KeypadComponent __instance, out int __state)
    {
        int correctButtonsPressed = 0;
        foreach (var button in __instance.buttons)
        {
            if (button.IsStayingDown)
            {
                correctButtonsPressed++;
            }
        }
        __state = correctButtonsPressed;
    }
    
    [HarmonyPostfix]
    public static void Postfix(bool __result, int __state)
    {
        if (!Plugin.EnableKeypadVibration || !__result) return;
        
        int correctButtonsPressedBeforeThisOne = __state;
        
        float power = Plugin.KeypadVibrationBaseStrength + (correctButtonsPressedBeforeThisOne * 0.25f);
        
        power = Mathf.Clamp(power, Plugin.KeypadVibrationBaseStrength, 1.0f);

        Plugin.TriggerVibration(power, 0.3f);
    }
}

/// <summary>
/// PATCH: Simon says glow
/// </summary>
[HarmonyPatch(typeof(SimonButton), "Glow")]
public class SimonSaysFlashHapticPatch
{
    private const float DURATION = 0.3f;

    
    private static readonly FieldInfo isFocusedField = AccessTools.Field(typeof(BombComponent), "isFocused");
    private static readonly FieldInfo lastIndexField = AccessTools.Field(typeof(SimonComponent), "lastIndex");

    [HarmonyPrefix]
    public static void Prefix(SimonButton __instance)
    {
        if (!Plugin.EnableSimonVibration) return;
        
        var simonComponent = __instance.ParentComponent as SimonComponent;
        if (simonComponent == null) return;

        
        bool isModuleFocused = (bool)isFocusedField.GetValue(simonComponent);
        if (isModuleFocused)
        {
            
            int currentStage = (int)lastIndexField.GetValue(simonComponent);
            
            float power = Plugin.SimonVibrationBaseStrength + (currentStage * 0.2f);
            power = Mathf.Clamp(power, Plugin.SimonVibrationBaseStrength, 1.0f);
            
            Plugin.TriggerVibration(power, DURATION);
        }
    }
}


/// <summary>
/// PATCH 2: Vibrations on button presses.
/// </summary>
[HarmonyPatch(typeof(SimonComponent), "ButtonDown")]
public class SimonSaysButtonHapticPatch
{
    private const float DURATION = 0.3f;

    private static readonly FieldInfo solveProgressField = AccessTools.Field(typeof(SimonComponent), "solveProgress");
    private static readonly FieldInfo currentSequenceField = AccessTools.Field(typeof(SimonComponent), "currentSequence");
    
    [HarmonyPrefix]
    public static void Prefix(SimonComponent __instance, int index)
    {
        if (!Plugin.EnableSimonVibration) return;
        
        if (__instance.IsSolved || !__instance.IsActive)
        {
            return;
        }

        int solveProgress = (int)solveProgressField.GetValue(__instance);
        int[] currentSequence = (int[])currentSequenceField.GetValue(__instance);

        if (__instance.MapToSolution(currentSequence[solveProgress]) == index)
        {
            float power = Plugin.SimonVibrationBaseStrength + (solveProgress * 0.2f);
            power = Mathf.Clamp(power, Plugin.SimonVibrationBaseStrength, 1.0f);
            Plugin.TriggerVibration(power, DURATION);
        }
    }
}

[HarmonyPatch(typeof(WhosOnFirstComponent), "ButtonDown")]
public class WhosOnFirstHapticPatch
{
    private const float DURATION = 1.0f;
    
    private static readonly FieldInfo currentStageField = AccessTools.Field(typeof(WhosOnFirstComponent), "currentStage");
    
    [HarmonyPrefix]
    public static void Prefix(WhosOnFirstComponent __instance, out int __state)
    {
        __state = (int)currentStageField.GetValue(__instance);
    }
    
    [HarmonyPostfix]
    public static void Postfix(bool __result, int __state)
    {
        if (!Plugin.EnableWhosOnFirstVibration || !__result) return;
        
        int stageBeforePress = __state;
        
        float power = Plugin.WhosOnFirstVibrationBaseStrength + (stageBeforePress * 0.25f);
        
        power = Mathf.Clamp(power, Plugin.WhosOnFirstVibrationBaseStrength, 1.0f);

        Plugin.TriggerVibration(power, DURATION);
    }
}

/// <summary>
/// MEMORY MODULE
/// </summary>
[HarmonyPatch(typeof(MemoryComponent), "HandleCorrectEntry")]
public class MemoryComponentHapticPatch
{
    private const float DURATION = 0.4f;
    
    private static readonly FieldInfo currentStageField = AccessTools.Field(typeof(MemoryComponent), "currentStage");
    
    [HarmonyPrefix]
    public static void Prefix(MemoryComponent __instance)
    {
        if (!Plugin.EnableMemoryVibration) return;
        
        if (__instance.IsSolved)
        {
            return;
        }
        
        int stage = (int)currentStageField.GetValue(__instance);
        
        float power = Plugin.MemoryVibrationBaseStrength + (stage * 0.2f);
        
        power = Mathf.Clamp(power, Plugin.MemoryVibrationBaseStrength, 1.0f);

        Plugin.TriggerVibration(power, DURATION);
    }
}
/// <summary>
/// MORSE CODE MODULE
/// PATCH 1 OF 2: Handles the vibrations for scrolling through frequencies.
/// </summary>
[HarmonyPatch]
public class MorseCodeScrollHapticPatch
{
    private const float DURATION = 0.2f;
    
    [HarmonyPatch(typeof(MorseCodeComponent), "OnButtonUpPushed")]
    [HarmonyPatch(typeof(MorseCodeComponent), "OnButtonDownPushed")]
    [HarmonyPrefix]
    public static void Prefix(MorseCodeComponent __instance, out int __state)
    {
        __state = __instance.CurrentFrequencyIndex;
    }
    
    [HarmonyPatch(typeof(MorseCodeComponent), "OnButtonUpPushed")]
    [HarmonyPatch(typeof(MorseCodeComponent), "OnButtonDownPushed")]
    [HarmonyPostfix]
    public static void Postfix(MorseCodeComponent __instance, int __state)
    {
        if (!Plugin.EnableMorseVibration) return;
        
        int oldIndex = __state;
        int newIndex = __instance.CurrentFrequencyIndex;
        
        if (oldIndex != newIndex)
        {
            Plugin.TriggerVibration(Plugin.MorseScrollVibrationStrength, DURATION);
        }
    }
}

/// <summary>
/// MORSE CODE MODULE
/// PATCH 2: Vibrations on the blinking light.
/// </summary>
[HarmonyPatch(typeof(MorseCodeComponent), "SetLED")]
public class MorseCodeBlinkHapticPatch
{
    private const float BLINK_START_DURATION = 3.0f; 
    
    private const float BLINK_STOP_DURATION = 0.05f; 

    private static readonly FieldInfo isFocusedField = AccessTools.Field(typeof(BombComponent), "isFocused");
    
    [HarmonyPrefix]
    public static void Prefix(MorseCodeComponent __instance, object state)
    {
        if (!Plugin.EnableMorseVibration) return;
        
        if (isFocusedField == null) return;
        
        bool isModuleFocused = (bool)isFocusedField.GetValue(__instance);
        if (!isModuleFocused)
        {
            return;
        }

        int stateValue = (int)state;
        
        if (stateValue == 0)
        {

            Plugin.TriggerVibration(Plugin.MorseBlinkVibrationStrength, BLINK_START_DURATION);
        }
        else 
        {
            Plugin.TriggerVibration(Plugin.MorseBlinkVibrationStrength, BLINK_STOP_DURATION);
        }
    }
}

/// <summary>
/// COMPLICATED WIRE MODULE
/// </summary>
[HarmonyPatch(typeof(VennSnippableWire), "Interact")]
public class ComplicatedWireHapticPatch
{
    [HarmonyPrefix]
    public static void Prefix(VennSnippableWire __instance, out bool __state)
    {
        __state = __instance.Snipped;
    }
    
    [HarmonyPostfix]
    public static void Postfix(VennSnippableWire __instance, bool __state)
    {
        if (!Plugin.EnableComplicatedWireVibration) return;
        
        bool wasSnippedBeforeInteract = __state;
        bool isSnippedAfterInteract = __instance.Snipped;
        
        if (!wasSnippedBeforeInteract && isSnippedAfterInteract)
        {
            Plugin.TriggerVibration(Plugin.ComplicatedWireVibrationStrength, 0.15f);
        }
    }
}

/// <summary>
/// WIRE SEQUENCE MODULE PART 1 - SNIPPING A WIRE
/// </summary>
[HarmonyPatch(typeof(WireSequenceWire), "Interact")]
public class WireSequenceWireHapticPatch
{
    [HarmonyPrefix]
    public static void Prefix(WireSequenceWire __instance, out bool __state)
    {
        __state = __instance.Snipped;
    }

    [HarmonyPostfix]
    public static void Postfix(WireSequenceWire __instance, bool __state)
    {
        if (!Plugin.EnableWireSequenceVibration) return;
        
        bool wasSnippedBefore = __state;
        bool isSnippedAfter = __instance.Snipped;
        
        if (!wasSnippedBefore && isSnippedAfter)
        {
            Plugin.TriggerVibration(Plugin.WireSequenceWireVibrationStrength, 0.1f);
        }
    }
}


/// <summary>
/// WIRE SEQUENCE MODULE PART 2 - GOING TO NEXT STAGE
/// </summary>
[HarmonyPatch(typeof(WireSequenceComponent), "DownButtonPressed")]
public class WireSequenceStageHapticPatch
{
    private const float DURATION = 0.9f;
    
    private static readonly FieldInfo currentPageField = AccessTools.Field(typeof(WireSequenceComponent), "currentPage");
    
    [HarmonyPrefix]
    public static void Prefix(WireSequenceComponent __instance, out int __state)
    {
        __state = (int)currentPageField.GetValue(__instance);
    }
    
    [HarmonyPostfix]
    public static void Postfix(WireSequenceComponent __instance, int __state)
    {
        if (!Plugin.EnableWireSequenceVibration) return;
        
        int oldPage = __state;
        int newPage = (int)currentPageField.GetValue(__instance);
        
        if (newPage > oldPage)
        {
            float power = Plugin.WireSequenceStageVibrationBaseStrength + (oldPage * 0.2f);
            
            power = Mathf.Clamp(power, Plugin.WireSequenceStageVibrationBaseStrength, 1.0f);

            Plugin.TriggerVibration(power, DURATION);
        }
    }
}

/// <summary>
/// MAZE MODULE
/// </summary>
[HarmonyPatch(typeof(InvisibleWallsComponent), "ButtonDown")]
public class MazeHapticPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        if (!Plugin.EnableMazeVibration) return;
        
        Plugin.TriggerVibration(Plugin.MazeVibrationStrength, 0.2f);
    }
}

/// <summary>
/// PASSWORD MODULE
/// </summary>
[HarmonyPatch]
public class PasswordSpinnerHapticPatch
{
    [HarmonyPatch(typeof(CharSpinner), "Next")]
    [HarmonyPatch(typeof(CharSpinner), "Previous")]
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (!Plugin.EnablePasswordVibration) return;
        
        Plugin.TriggerVibration(Plugin.PasswordVibrationStrength, 0.2f);
    }
}

///
///NEEDY MODULES HERE
///

/// <summary>
/// CAPACITOR DISCHARGE NEEDY MODULE VENT MODULE
/// </summary>
[HarmonyPatch]
public class CapacitorDischargeHapticPatch
{
    private const float PUSH_POWER = 0.7f;
    private const float PUSH_DURATION = 500.0f; 
    
    private const float RELEASE_POWER = 0.1f;
    private const float RELEASE_DURATION = 0.1f;
    
    [HarmonyPatch(typeof(NeedyDischargeComponent), "OnPush")]
    [HarmonyPostfix]
    public static void PushPostfix()
    {
        if (!Plugin.EnableCapacitorDischargeVibration) return;
        
        Plugin.TriggerVibration(Plugin.CapacitorDischargePushVibrationStrength, 500.0f);
    }
    
    [HarmonyPatch(typeof(NeedyDischargeComponent), "OnRelease")]
    [HarmonyPostfix]
    public static void ReleasePostfix()
    {
        if (!Plugin.EnableCapacitorDischargeVibration) return;
        
        Plugin.TriggerVibration(Plugin.CapacitorDischargeReleaseVibrationStrength, 0.1f);
    }
}

/// <summary>
/// KNOB NEEDY MODULE
/// </summary>
[HarmonyPatch]
public class NeedyKnobHapticPatch
{
    [HarmonyPatch(typeof(PointingKnob), "RotateLeft")]
    [HarmonyPatch(typeof(PointingKnob), "RotateRight")]
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (!Plugin.EnableNeedyKnobVibration) return;
        
        Plugin.TriggerVibration(Plugin.NeedyKnobVibrationStrength, 0.2f);
    }
}

/// <summary>
/// NEEDY VENTING GAS MODULE
/// </summary>
[HarmonyPatch(typeof(NeedyVentComponent), "ButtonDown")]
public class VentGasHapticPatch
{
    private static readonly FieldInfo displayChangingField = AccessTools.Field(typeof(NeedyVentComponent), "displayChanging");
    
    [HarmonyPrefix]
    public static void Prefix(NeedyVentComponent __instance, int index)
    {
        if (!Plugin.EnableVentGasVibration) return;
        
        // Do nothing if needy is not active.
        bool isDisplayChanging = (bool)displayChangingField.GetValue(__instance);
        if (__instance.State != NeedyComponent.NeedyStateEnum.Running || isDisplayChanging)
        {
            return;
        }
        
        bool isCorrectPress = false;

        // Index 0 is the "Yes" button.
        if (index == 0 && __instance.Question == NeedyVentComponent.QuestionEnum.VENT)
        {
            isCorrectPress = true;
        }
        // Index 1 is the "No" button.
        else if (index == 1 && __instance.Question == NeedyVentComponent.QuestionEnum.DETONATE)
        {
            isCorrectPress = true;
        }
        
        if (isCorrectPress)
        {
            Plugin.TriggerVibration(Plugin.VentGasVibrationStrength, 0.5f);
        }
    }
}

///
/// MISC STUFF HERE
///


/// <summary>
/// ALARM CLOCK VIBRATIONS
/// </summary>
public class AlarmClockHapticController : MonoBehaviour
{
    private const float POWER = 1.0f;
    
    private const float BEEP_VIBRATION_DURATION = 0.2f;
    private const float PAUSE_DURATION = 0.10f;
    
    private const float TOTAL_CYCLE_INTERVAL = (BEEP_VIBRATION_DURATION + PAUSE_DURATION)*2;

    private Coroutine hapticLoopCoroutine;
    private FieldInfo isOnField;
    private AlarmClock alarmClockInstance;

    public void Initialize(AlarmClock alarmClock)
    {
        alarmClockInstance = alarmClock;
        isOnField = AccessTools.Field(typeof(AlarmClock), "isOn");
        if (isOnField == null)
        {
            Plugin.Logger.LogError("[Haptics] CRITICAL: Could not find the 'isOn' field via reflection on AlarmClock!");
        }
    }

    public void StartHapticLoop()
    {
        if (hapticLoopCoroutine != null) StopCoroutine(hapticLoopCoroutine);
        hapticLoopCoroutine = StartCoroutine(HapticLoop());
    }

    public void StopHapticLoop()
    {
        if (hapticLoopCoroutine != null) StopCoroutine(hapticLoopCoroutine);
        hapticLoopCoroutine = null;
    }

    private IEnumerator HapticLoop()
    {
        yield return new WaitForSeconds(0.1f);
        if (isOnField == null) yield break;


        while ((bool)isOnField.GetValue(alarmClockInstance))
        {
            if (Plugin.EnableAlarmClockVibration)
            {
                Plugin.TriggerVibration(Plugin.AlarmClockVibrationStrength, BEEP_VIBRATION_DURATION);
            }
            
            yield return new WaitForSeconds(TOTAL_CYCLE_INTERVAL);
        }
    }
}


[HarmonyPatch(typeof(Assets.Scripts.Props.AlarmClock))] 
public class AlarmClockHapticPatch
{

    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    public static void StartPostfix(Assets.Scripts.Props.AlarmClock __instance)
    {
        var controller = __instance.gameObject.AddComponent<AlarmClockHapticController>();
        controller.Initialize(__instance);
    }


    [HarmonyPatch("TurnOn")]
    [HarmonyPostfix]
    public static void TurnOnPostfix(Assets.Scripts.Props.AlarmClock __instance)
    {
        var hapticController = __instance.GetComponent<AlarmClockHapticController>();
        if (hapticController != null)
        {
            hapticController.StartHapticLoop();
        }
    }
    
    [HarmonyPatch("TurnOff")]
    [HarmonyPostfix]
    public static void TurnOffPostfix(Assets.Scripts.Props.AlarmClock __instance)
    {
        var hapticController = __instance.GetComponent<AlarmClockHapticController>();
        if (hapticController != null)
        {
            hapticController.StopHapticLoop();
        }
    }
}
