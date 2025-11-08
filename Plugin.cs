using BepInEx;
using BepInEx.Logging;
using DragonDen.Hitmarker.Patches;
using DragonDen.Hitmarker.Utilities;
using UnityEngine;

namespace DragonDen.Hitmarker;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.SPT.custom", "4.0")]
public class Plugin : BaseUnityPlugin
{
    public new static ManualLogSource Logger { get; set; }

    public void Awake()
    {
        Logger ??= BepInEx.Logging.Logger.CreateLogSource("DragonDen.Hitmarker");
        Settings.Init(Config);
        PatchManager.EnablePatches();
    }
}