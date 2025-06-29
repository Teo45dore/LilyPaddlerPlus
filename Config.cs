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
    public ConfigEntry<bool> MoreDebugCfg { get; private set; }
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

        this.MoreDebugCfg = this.cfg.Bind(
            "Other",
            "MoreDebug",
            false,
            "Enables more extensive logging for debugging purposes. (When turned on the \"Debug\" logging channel must be enabled in the BepInEx.cfg file!)"
        );

        this.watcher = new FileSystemWatcher(Paths.ConfigPath) {
            Filter = "com.Teo45dore.LilyPaddlerPlus.cfg",
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        this.watcher.Changed += this.OnConfigChanged;

        this.LoadConfig(true);
    }


    void ClearOrphanedEntries(ConfigFile cfg)
    {
        PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
        Dictionary<ConfigDefinition, string> orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg);
        orphanedEntries.Clear();
    }

    void OnConfigChanged(object sender, FileSystemEventArgs e)
    {
        if (!this.isDisposing) {
            this.reloadQueue.Enqueue(true);
        }
    }

    void LoadConfig(bool firstLoad = false)
    {
        bool lastDampCameraVal = this.DampCameraCfg.Value;
        bool lastSlipperyMoveVal = this.SlipperyMovementCfg.Value;
        bool lastRandHotbarVal = this.RandomizeHotbarCfg.Value;
        bool lastMoreDebugVal = this.MoreDebugCfg.Value;

        this.cfg.SaveOnConfigSet = false;

        this.cfg.Reload();

        bool settingsWereChanged = false;

        this.DampCameraCfg.Value = this.cfg.Bind(this.DampCameraCfg.Definition.Section, this.DampCameraCfg.Definition.Key, (bool)this.DampCameraCfg.DefaultValue, this.DampCameraCfg.Description).Value;
        this.SlipperyMovementCfg.Value = this.cfg.Bind(this.SlipperyMovementCfg.Definition.Section, this.SlipperyMovementCfg.Definition.Key, (bool)this.SlipperyMovementCfg.DefaultValue, this.SlipperyMovementCfg.Description).Value;
        this.RandomizeHotbarCfg.Value = this.cfg.Bind(this.RandomizeHotbarCfg.Definition.Section, this.RandomizeHotbarCfg.Definition.Key, (bool)this.RandomizeHotbarCfg.DefaultValue, this.RandomizeHotbarCfg.Description).Value;
        this.MoreDebugCfg.Value = this.cfg.Bind(this.MoreDebugCfg.Definition.Section, this.MoreDebugCfg.Definition.Key, (bool)this.MoreDebugCfg.DefaultValue, this.MoreDebugCfg.Description).Value;

        for (int i = 0; i < 4; i++) {
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
                if (lastMoreDebugVal != this.MoreDebugCfg.Value) {
                    settingsWereChanged = true;
                    break;
                }
            }
        }

        this.ClearOrphanedEntries(this.cfg);

        this.cfg.SaveOnConfigSet = true;

        if (firstLoad) {
            Plugin.Logger.LogDebug($"Loaded ModConfig! DampCam = {this.DampCameraCfg.Value}, Slippery = {this.SlipperyMovementCfg.Value}, RandHotbar = {this.RandomizeHotbarCfg.Value}, MoreDebug = {this.MoreDebugCfg.Value}");
        } else if (settingsWereChanged) {
            Plugin.Logger.LogDebug($"Reloaded ModConfig! DampCam = {this.DampCameraCfg.Value}, Slippery = {this.SlipperyMovementCfg.Value}, RandHotbar = {this.RandomizeHotbarCfg.Value}, MoreDebug = {this.MoreDebugCfg.Value}");
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
            this.watcher.Changed -= this.OnConfigChanged;
            this.watcher.Dispose();
        }

        this.isDisposing = true;
    }

    internal void ProcessConfigReloadQueue()
    {
        while (this.reloadQueue.TryDequeue(out _)) {
            this.LoadConfig();
        }
    }
}
