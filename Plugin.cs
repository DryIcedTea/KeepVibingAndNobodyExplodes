using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Assets.Scripts.Props;
using DarkTonic.MasterAudio;
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
    
    private const string harmonyId = "com.dryicedmatcha.ktane.hapticsmod";
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
        if (__result)
        {
            int correctButtonsPressedBeforeThisOne = __state;
            
            float power = 0.25f + (correctButtonsPressedBeforeThisOne * 0.25f);
            
            power = Mathf.Clamp(power, 0.25f, 1.0f);

            Plugin.TriggerVibration(power, 0.3f);
        }
    }
}

/// <summary>
/// SIMON SAYS MODULE
/// </summary>
[HarmonyPatch(typeof(SimonComponent), "ButtonDown")]
public class SimonSaysHapticPatch
{
    private const float DURATION = 0.5f;
    
    private static readonly FieldInfo solveProgressField = AccessTools.Field(typeof(SimonComponent), "solveProgress");
    private static readonly FieldInfo currentSequenceField = AccessTools.Field(typeof(SimonComponent), "currentSequence");
    
    [HarmonyPrefix]
    public static void Prefix(SimonComponent __instance, int index)
    {
        
        if (__instance.IsSolved || !__instance.IsActive)
        {
            return;
        }
        
        int solveProgress = (int)solveProgressField.GetValue(__instance);
        int[] currentSequence = (int[])currentSequenceField.GetValue(__instance);
        
        if (__instance.MapToSolution(currentSequence[solveProgress]) == index)
        {
            float power = 0.2f + (solveProgress * 0.2f);
            power = Mathf.Clamp(power, 0.2f, 1.0f);

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
        if (__result)
        {
            int stageBeforePress = __state;
            
            float power = 0.25f + (stageBeforePress * 0.25f);
            
            power = Mathf.Clamp(power, 0.25f, 0.75f);

            Plugin.TriggerVibration(power, DURATION);
        }
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
        if (__instance.IsSolved)
        {
            return;
        }
        
        int stage = (int)currentStageField.GetValue(__instance);
        
        float power = 0.2f + (stage * 0.2f);
        
        power = Mathf.Clamp(power, 0.2f, 1.0f);

        Plugin.TriggerVibration(power, DURATION);
    }
}
/// <summary>
/// MORSE CODE MODULE
/// </summary>
[HarmonyPatch]
public class MorseCodeHapticPatch
{
    private const float POWER = 0.2f;
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
        int oldIndex = __state;
        int newIndex = __instance.CurrentFrequencyIndex;
        
        
        if (oldIndex != newIndex)
        {
            Plugin.TriggerVibration(POWER, DURATION);
        }
    }
}

/// <summary>
/// COMPLICATED WIRE MODULE
/// </summary>
[HarmonyPatch(typeof(VennSnippableWire), "Interact")]
public class ComplicatedWireHapticPatch
{
    private const float POWER = 0.5f;
    private const float DURATION = 0.15f;
    
    [HarmonyPrefix]
    public static void Prefix(VennSnippableWire __instance, out bool __state)
    {
        __state = __instance.Snipped;
    }
    
    [HarmonyPostfix]
    public static void Postfix(VennSnippableWire __instance, bool __state)
    {
        bool wasSnippedBeforeInteract = __state;
        bool isSnippedAfterInteract = __instance.Snipped;
        
        if (!wasSnippedBeforeInteract && isSnippedAfterInteract)
        {
            Plugin.TriggerVibration(POWER, DURATION);
        }
    }
}

/// <summary>
/// WIRE SEQUENCE MODULE PART 1 - SNIPPING A WIRE
/// </summary>
[HarmonyPatch(typeof(WireSequenceWire), "Interact")]
public class WireSequenceWireHapticPatch
{
    private const float POWER = 0.5f;
    private const float DURATION = 0.1f;

    [HarmonyPrefix]
    public static void Prefix(WireSequenceWire __instance, out bool __state)
    {
        __state = __instance.Snipped;
    }

    [HarmonyPostfix]
    public static void Postfix(WireSequenceWire __instance, bool __state)
    {
        bool wasSnippedBefore = __state;
        bool isSnippedAfter = __instance.Snipped;
        
        if (!wasSnippedBefore && isSnippedAfter)
        {
            Plugin.TriggerVibration(POWER, DURATION);
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
        int oldPage = __state;
        int newPage = (int)currentPageField.GetValue(__instance);
        
        if (newPage > oldPage)
        {
            float power = 0.2f + (oldPage * 0.2f);
            
            power = Mathf.Clamp(power, 0.2f, 0.8f);

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
    private const float POWER = 0.3f;
    private const float DURATION = 0.2f;
    
    [HarmonyPrefix]
    public static void Prefix()
    {
        Plugin.TriggerVibration(POWER, DURATION);
    }
}

/// <summary>
/// PASSWORD MODULE
/// </summary>
[HarmonyPatch]
public class PasswordSpinnerHapticPatch
{
    private const float POWER = 0.3f;
    private const float DURATION = 0.2f;
    
    [HarmonyPatch(typeof(CharSpinner), "Next")]
    [HarmonyPatch(typeof(CharSpinner), "Previous")]
    [HarmonyPostfix]
    public static void Postfix()
    {
        Plugin.TriggerVibration(POWER, DURATION);
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
        Plugin.TriggerVibration(PUSH_POWER, PUSH_DURATION);
    }
    
    [HarmonyPatch(typeof(NeedyDischargeComponent), "OnRelease")]
    [HarmonyPostfix]
    public static void ReleasePostfix()
    {
        Plugin.TriggerVibration(RELEASE_POWER, RELEASE_DURATION);
    }
}

/// <summary>
/// KNOB NEEDY MODULE
/// </summary>
[HarmonyPatch]
public class NeedyKnobHapticPatch
{
    private const float POWER = 0.2f;
    private const float DURATION = 0.2f;
    
    [HarmonyPatch(typeof(PointingKnob), "RotateLeft")]
    [HarmonyPatch(typeof(PointingKnob), "RotateRight")]
    [HarmonyPostfix]
    public static void Postfix()
    {
        Plugin.TriggerVibration(POWER, DURATION);
    }
}

/// <summary>
/// NEEDY VENTING GAS MODULE
/// </summary>
[HarmonyPatch(typeof(NeedyVentComponent), "ButtonDown")]
public class VentGasHapticPatch
{
    private const float POWER = 0.5f;
    private const float DURATION = 0.5f;
    
    private static readonly FieldInfo displayChangingField = AccessTools.Field(typeof(NeedyVentComponent), "displayChanging");
    
    [HarmonyPrefix]
    public static void Prefix(NeedyVentComponent __instance, int index)
    {
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
            Plugin.TriggerVibration(POWER, DURATION);
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
    private const float PAUSE_DURATION = 0.1f;
    
    private const float TOTAL_CYCLE_INTERVAL = BEEP_VIBRATION_DURATION + PAUSE_DURATION;

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
            Plugin.TriggerVibration(POWER, BEEP_VIBRATION_DURATION);
            
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
