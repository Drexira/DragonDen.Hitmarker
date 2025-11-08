using Comfort.Common;
using EFT;
using UnityEngine;

namespace DragonDen.Hitmarker.Utilities;

internal static class State
{
    public static GameWorld World => Singleton<GameWorld>.Instance;
    public static Player LocalPlayer => World?.MainPlayer;
}