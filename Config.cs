using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace LilyPaddlerPlus;

public class ModConfig : IDisposable
{
    private ConfigFile cfg;
    private readonly FileSystemWatcher watcher;

    public static ModConfig Instance { get; private set; }

    public static ConfigEntry<bool> DampCameraCfg { get; private set; }
    public static ConfigEntry<bool> SlipperyMovementCfg { get; private set; }
    public static ConfigEntry<bool> RandomizeHotbarCfg { get; private set; }
    public static ConfigEntry<bool> WigglyIconsCfg { get; private set; }
    public static ConfigEntry<bool> DOFTomfooleryCfg { get; private set; }
    public static ConfigEntry<float> IntensityCfg { get; private set; }
    public static ConfigEntry<bool> ShowFalseValuesCfg { get; private set; }
    public static ConfigEntry<bool> InvertCameraCfg { get; private set; }
    public static ConfigEntry<bool> InvertMovementCfg { get; private set; }
    public static ConfigEntry<bool> VerboseLoggingCfg { get; private set; }

    private bool isDisposing = false;

    internal ModConfig(ConfigFile c)
    {
        Instance = this;
        this.cfg = c;

        DampCameraCfg = this.cfg.Bind(
            "General",
            "DampCamera",
            true,
            "When disoriented, make the camera move sluggishly. (Not recomended for people who are easily nauseated)"
        );

        SlipperyMovementCfg = this.cfg.Bind(
            "General",
            "SlipperyMovement",
            true,
            "When disoriented, make movement slippery."
        );

        RandomizeHotbarCfg = this.cfg.Bind(
            "General",
            "RandomizeHotbarBinds",
            true,
            "When disoriented, randomize the keybinds for the hotbar."
        );

        WigglyIconsCfg = this.cfg.Bind(
            "General",
            "WigglyIcons",
            true,
            "When disoriented, make most GUI icons wiggle around the screen."
        );

        DOFTomfooleryCfg = this.cfg.Bind(
            "General",
            "DisruptDepthOfField",
            true,
            "When disoriented, mess with the depth of field (requires depth of field to be enabled in video settings)."
        );

        IntensityCfg = this.cfg.Bind(
            "General",
            "IntensityMultiplier",
            1f,
            new ConfigDescription("Multiplies the intensity of all the mod's effects, this only applies to effects that are scalable.", new AcceptableValueRange<float>(0, 100))
        );

        ShowFalseValuesCfg = this.cfg.Bind(
            "General.Community",
            "ShowFalseValues",
            true,
            "When disoriented, show false values on HUD such as a fake O2 value or fake minimap direction. Suggested by @Skippytbm on Nexus Mods!"
        );

        InvertCameraCfg = this.cfg.Bind(
            "General.Community",
            "InvertCameraControls",
            true,
            "When disoriented, invert camera controls. Suggested by @SirWarper420 on Nexus Mods!"
        );

        InvertMovementCfg = this.cfg.Bind(
            "General.Community",
            "InvertMovementControls",
            true,
            "When disoriented, invert movement controls. Inspired by @SirWarper420 on Nexus Mods!"
        );

        VerboseLoggingCfg = this.cfg.Bind(
            "Other",
            "VerboseLogging",
            false,
            "Enables more extensive logging for bug reports. (When turned on the \"Debug\" logging channel in Logging.Disk must be enabled within the BepInEx.cfg file!)"
        );

        this.watcher = new FileSystemWatcher(Paths.ConfigPath) {
            Filter = $"{Plugin.PLUGIN_GUID}.cfg",
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        this.watcher.Changed += this.ReloadConfig;

        Plugin.Logger.LogDebug($"Loaded ModConfig!");
    }

    void ClearOrphanedEntries()
    {
        PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
        Dictionary<ConfigDefinition, string> orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(this.cfg);
        orphanedEntries.Clear();
    }

    void ReloadConfig(object o, FileSystemEventArgs e)
    {
        this.cfg.SaveOnConfigSet = false;

        this.cfg.Reload();

        RefreshEntry(DampCameraCfg);
        RefreshEntry(SlipperyMovementCfg);
        RefreshEntry(RandomizeHotbarCfg);
        RefreshEntry(WigglyIconsCfg);
        RefreshEntry(DOFTomfooleryCfg);
        RefreshEntry(IntensityCfg);
        RefreshEntry(ShowFalseValuesCfg);
        RefreshEntry(InvertCameraCfg);
        RefreshEntry(InvertMovementCfg);
        RefreshEntry(VerboseLoggingCfg);

        this.ClearOrphanedEntries();

        this.cfg.SaveOnConfigSet = true;
        
        Plugin.Logger.LogDebug("Reloaded ModConfig!");
    }

    private void RefreshEntry<T>(ConfigEntry<T> entry) {
        entry = cfg.Bind(entry.Definition.Section, entry.Definition.Key, (T)entry.DefaultValue, entry.Description);
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.isDisposing) return;

        if (disposing) {
            this.watcher.Changed -= this.ReloadConfig;
            this.watcher.Dispose();
        }

        this.isDisposing = true;
    }
}
