using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace KeepVibingAndNobodyExplodes;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    private ButtplugManager buttplugManager;
    
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        
        // Initialize Buttplug manager
        InitializeButtplug();
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
}
