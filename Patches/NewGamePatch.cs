using System.Reflection;
using Comfort.Common;
using DragonDen.Hitmarker.Features;
using DragonDen.Hitmarker.Utilities;
using EFT;
using SPT.Reflection.Patching;
using UnityEngine;

namespace DragonDen.Hitmarker.Patches;

internal class NewGamePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
    }

    [PatchPrefix]
    private static void PatchPrefix()
    {
        var gameWorld = Singleton<GameWorld>.Instance;

        if (gameWorld == null) return;
        gameWorld.gameObject.AddComponent<HitmarkerController>();
    }
}