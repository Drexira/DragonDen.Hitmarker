using System.IO;
using System.Reflection;
using UnityEngine;

namespace DragonDen.Hitmarker.Utilities;

internal static class TextureBank
{
    private static string baseUiDir;
    private static Texture2D hitmarker;
    private static Texture2D hitmarkerHs;
    private static Texture2D hitmarkerKill;
    private static string cachedHitName;
    private static string cachedHsName;
    private static string cachedKillName;

    private static void EnsureBase()
    {
        if (baseUiDir != null) return;
        var dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
        baseUiDir = Path.Combine(dllDir, "UI");
    }

    private static Texture2D LoadPng(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false) { filterMode = FilterMode.Bilinear };
            if (!tex.LoadImage(bytes)) return null;
            return tex;
        }
        catch
        {
            return null;
        }
    }

    public static Texture2D Hitmarker()
    {
        EnsureBase();
        var name = Settings.HitmarkerImageFile.Value?.Trim() ?? "Hitmarker.png";
        if (!string.Equals(name, cachedHitName))
        {
            hitmarker = LoadPng(Path.Combine(baseUiDir, name));
            cachedHitName = name;
        }
        if (!hitmarker) hitmarker = LoadPng(Path.Combine(baseUiDir, "Hitmarker.png"));
        return hitmarker;
    }

    public static Texture2D HitmarkerHeadshot()
    {
        EnsureBase();
        var name = Settings.HitmarkerHeadshotImageFile.Value?.Trim() ?? "Hitmarker_Headshot.png";
        if (!string.Equals(name, cachedHsName))
        {
            hitmarkerHs = LoadPng(Path.Combine(baseUiDir, name));
            cachedHsName = name;
        }
        return hitmarkerHs ?? Hitmarker();
    }

    public static Texture2D HitmarkerKill()
    {
        EnsureBase();
        var name = Settings.HitmarkerKillImageFile.Value?.Trim() ?? "Hitmarker_Kill.png";
        if (!string.Equals(name, cachedKillName))
        {
            hitmarkerKill = LoadPng(Path.Combine(baseUiDir, name));
            cachedKillName = name;
        }
        return hitmarkerKill ?? Hitmarker();
    }
}