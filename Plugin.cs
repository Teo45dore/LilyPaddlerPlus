using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LilyPaddlerPlus.Config;

namespace LilyPaddlerPlus;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInProcess("SubnauticaZero.exe")]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony Harmony { get; set; }
    internal const string PLUGIN_GUID = "com.Teo45dore.LilyPaddlerPlus";
    internal const string PLUGIN_NAME = "LilyPaddlerPlus";
    internal const string PLUGIN_VERSION = "1.0.0";


    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        new ModConfig(base.Config);

        Patch();

        Logger.LogInfo($"{PLUGIN_GUID} v{PLUGIN_VERSION} has loaded!");
    }

    private void Update()
    {
        ModConfig.Instance?.ProcessConfigReloadQueue();
    }

    private void OnDestroy()
    {
        ModConfig.Instance?.Dispose();
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        Logger.LogDebug("Finished patching!");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }
}
