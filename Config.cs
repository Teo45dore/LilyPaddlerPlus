using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace LilyPaddlerPlus.Config;

public class ModConfig : IDisposable
{
    private ConfigFile cfg;
    private readonly FileSystemWatcher watcher;

    public static ModConfig Instance { get; private set; }
    public ConfigEntry<bool> DampCameraCfg { get; private set; }
    public ConfigEntry<bool> SlipperyMovementCfg { get; private set; }
    public ConfigEntry<bool> RandomizeHotbarCfg { get; private set; }
    public ConfigEntry<float> IntensityCfg { get; private set; }
    public ConfigEntry<bool> MoreDebugCfg { get; private set; }
    public ConfigEntry<bool> InvertCameraCfg { get; private set; }
    public ConfigEntry<bool> InvertMovementCfg { get; private set; }

    private ConcurrentQueue<bool> reloadQueue = new ConcurrentQueue<bool>();
    private bool isDisposing = false;

    internal ModConfig(ConfigFile c)
    {
        Instance = this;
        this.cfg = c;

        this.DampCameraCfg = this.cfg.Bind(
            "General",
            "DampCamera",
            true,
            "When disoriented make the camera move sluggishly. (Not recomended for people who are easily nauseated)"
        );

        this.SlipperyMovementCfg = this.cfg.Bind(
            "General",
            "SlipperyMovement",
            true,
            "When disoriented make movement slippery."
        );

        this.RandomizeHotbarCfg = this.cfg.Bind(
            "General",
            "RandomizeHotbarBinds",
            true,
            "When disoriented randomize the keybinds for the hotbar."
        );

        this.IntensityCfg = this.cfg.Bind(
            "General",
            "IntensityMultiplier",
            1f,
            new ConfigDescription("Multiplies the intensity of the mod's effects, this only applies to effects that are scalable.", new AcceptableValueRange<float>(0, 100))
        );

        this.InvertCameraCfg = this.cfg.Bind(
            "General.Community",
            "InvertCameraControls",
            true,
            "When disoriented invert camera controls. Suggested by @SirWarper420 on Nexus Mods!"
        );

        this.InvertMovementCfg = this.cfg.Bind(
            "General.Community",
            "InvertMovementControls",
            true,
            "When disoriented invert movement controls. Inspired by @SirWarper420 on Nexus Mods!"
        );

        this.MoreDebugCfg = this.cfg.Bind(
            "Other",
            "MoreDebug",
            false,
            "Enables more extensive logging for bug reports. (When turned on the \"Debug\" logging channel in Logging.Disk must be enabled within the BepInEx.cfg file!)"
        );

        this.watcher = new FileSystemWatcher(Paths.ConfigPath) {
            Filter = "com.Teo45dore.LilyPaddlerPlus.cfg",
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        this.watcher.Changed += this.ReloadConfig;

        Plugin.Logger.LogDebug($"Loaded ModConfig!");
        if (this.MoreDebugCfg.Value) {
            Plugin.Logger.LogDebug($"Config values: DampCam = {this.DampCameraCfg.Value}, Slippery = {this.SlipperyMovementCfg.Value}, RandHotbar = {this.RandomizeHotbarCfg.Value}, InvertCam = {this.InvertCameraCfg.Value}, Intensity = {this.IntensityCfg.Value}, InvertMove = {this.InvertMovementCfg.Value}, MoreDebug = {this.MoreDebugCfg.Value}");
        }
    }

    void ClearOrphanedEntries()
    {
        PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
        Dictionary<ConfigDefinition, string> orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(this.cfg);
        orphanedEntries.Clear();
    }

    void ReloadConfig(object o, FileSystemEventArgs e)
    {
        bool lastDampCameraVal = this.DampCameraCfg.Value;
        bool lastSlipperyMoveVal = this.SlipperyMovementCfg.Value;
        bool lastRandHotbarVal = this.RandomizeHotbarCfg.Value;
        float lastIntensityVal = this.IntensityCfg.Value;
        bool lastInvertCamVal = this.InvertCameraCfg.Value;
        bool lastInvertMoveVal = this.InvertMovementCfg.Value;
        bool lastMoreDebugVal = this.MoreDebugCfg.Value;

        this.cfg.SaveOnConfigSet = false;

        this.cfg.Reload();

        bool settingsWereChanged = false;

        this.DampCameraCfg.Value = this.cfg.Bind(this.DampCameraCfg.Definition.Section, this.DampCameraCfg.Definition.Key, (bool)this.DampCameraCfg.DefaultValue, this.DampCameraCfg.Description).Value;
        this.SlipperyMovementCfg.Value = this.cfg.Bind(this.SlipperyMovementCfg.Definition.Section, this.SlipperyMovementCfg.Definition.Key, (bool)this.SlipperyMovementCfg.DefaultValue, this.SlipperyMovementCfg.Description).Value;
        this.RandomizeHotbarCfg.Value = this.cfg.Bind(this.RandomizeHotbarCfg.Definition.Section, this.RandomizeHotbarCfg.Definition.Key, (bool)this.RandomizeHotbarCfg.DefaultValue, this.RandomizeHotbarCfg.Description).Value;
        this.IntensityCfg.Value = this.cfg.Bind(this.IntensityCfg.Definition.Section, this.IntensityCfg.Definition.Key, (float)this.IntensityCfg.DefaultValue, this.IntensityCfg.Description).Value;
        this.InvertCameraCfg.Value = this.cfg.Bind(this.InvertCameraCfg.Definition.Section, this.InvertCameraCfg.Definition.Key, (bool)this.InvertCameraCfg.DefaultValue, this.InvertCameraCfg.Description).Value;
        this.InvertMovementCfg.Value = this.cfg.Bind(this.InvertMovementCfg.Definition.Section, this.InvertMovementCfg.Definition.Key, (bool)this.InvertMovementCfg.DefaultValue, this.InvertMovementCfg.Description).Value;
        this.MoreDebugCfg.Value = this.cfg.Bind(this.MoreDebugCfg.Definition.Section, this.MoreDebugCfg.Definition.Key, (bool)this.MoreDebugCfg.DefaultValue, this.MoreDebugCfg.Description).Value;


        for (int i = 0; i < 7; i++) {
            if (i == 0) {
                if (lastDampCameraVal != this.DampCameraCfg.Value) {
                    settingsWereChanged = true;
                    break;
                }
            }
            if (i == 1) {
                if (lastSlipperyMoveVal != this.SlipperyMovementCfg.Value) {
                    settingsWereChanged = true;
                    break;
                }
            }
            if (i == 2) {
                if (lastRandHotbarVal != this.RandomizeHotbarCfg.Value) {
                    settingsWereChanged = true;
                    break;
                }
            }
            if (i == 3) {
                if (lastIntensityVal != this.IntensityCfg.Value) {
                    settingsWereChanged = true;
                    break;
                }
            }
            if (i == 4) {
                if (lastInvertCamVal != this.InvertCameraCfg.Value) {
                    settingsWereChanged = true;
                    break;
                }
            }
            if (i == 5) {
                if (lastInvertMoveVal != this.InvertMovementCfg.Value) {
                    settingsWereChanged = true;
                    break;
                }
            }
            if (i == 6) {
                if (lastMoreDebugVal != this.MoreDebugCfg.Value) {
                    settingsWereChanged = true;
                    break;
                }
            }
        }

        this.ClearOrphanedEntries();

        this.cfg.SaveOnConfigSet = true;

        if (settingsWereChanged) {
            Plugin.Logger.LogDebug($"Reloaded ModConfig!");
            if (this.MoreDebugCfg.Value) {
                Plugin.Logger.LogDebug($"New config values: DampCam = {this.DampCameraCfg.Value}, Slippery = {this.SlipperyMovementCfg.Value}, RandHotbar = {this.RandomizeHotbarCfg.Value}, Intensity = {this.IntensityCfg.Value}, InvertCam = {this.InvertCameraCfg.Value}, InvertMove = {this.InvertMovementCfg.Value}, MoreDebug = {this.MoreDebugCfg.Value}");
            }
        }
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
