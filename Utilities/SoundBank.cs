using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace DragonDen.Hitmarker.Utilities;

internal static class SoundBank
{
    private static AudioSource source;
    private static AudioClip hitClip;
    private static AudioClip headshotClip;
    private static AudioClip killClip;
    private static string baseSoundsDir;
    private static string cachedHitName;
    private static string cachedHeadshotName;
    private static string cachedKillName;

    private static void Ensure()
    {
        if (source != null) return;
        var go = new GameObject("DragonDen.Hitmarker.Audio");
        source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = Settings.MasterVolume.Value;
        baseSoundsDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Sounds");
        LoadClips();
    }

    public static void Reload()
    {
        if (source == null)
        {
            Ensure();
            return;
        }

        LoadClips();
    }

    private static void LoadClips()
    {
        var hitName = Settings.HitSoundFile.Value?.Trim() ?? "";
        var hitNameHeadshot = Settings.HeadshotSoundFile.Value?.Trim() ?? "";
        var killName = Settings.KillSoundFile.Value?.Trim() ?? "";

        if (!string.Equals(hitName, cachedHitName, StringComparison.OrdinalIgnoreCase))
        {
            hitClip = TryLoadWavSafe(Path.Combine(baseSoundsDir, hitName));
            cachedHitName = hitName;
        }
        
        if (!string.Equals(hitNameHeadshot, cachedHeadshotName, StringComparison.OrdinalIgnoreCase))
        {
            headshotClip = TryLoadWavSafe(Path.Combine(baseSoundsDir, hitNameHeadshot));
            cachedHeadshotName = hitNameHeadshot;
        }

        if (!string.Equals(killName, cachedKillName, StringComparison.OrdinalIgnoreCase))
        {
            killClip = TryLoadWavSafe(Path.Combine(baseSoundsDir, killName));
            cachedKillName = killName;
        }
    }

    public static void PlayHit()
    {
        Ensure();
        if (!Settings.PlaySoundOnHit.Value) return;
        if (hitClip == null) return;
        source.volume = Settings.MasterVolume.Value * Settings.HitSoundVolume.Value;
        source.PlayOneShot(hitClip);
    }
    
    public static void PlayHeadshot()
    {
        Ensure();
        if (!Settings.PlaySoundOnHeadshot.Value) return;
        if (headshotClip == null) return;
        source.volume = Settings.MasterVolume.Value * Settings.HeadshotSoundVolume.Value;
        source.PlayOneShot(headshotClip);
    }

    public static void PlayKill()
    {
        Ensure();
        if (!Settings.PlaySoundOnKill.Value) return;
        if (killClip == null) return;
        source.volume = Settings.MasterVolume.Value * Settings.KillSoundVolume.Value;
        source.PlayOneShot(killClip);
    }

    private static AudioClip TryLoadWavSafe(string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (!File.Exists(path)) return null;
            return LoadWav(path);
        }
        catch
        {
            return null;
        }
    }

    private static AudioClip LoadWav(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;

        var ext = Path.GetExtension(path)?.ToLowerInvariant();
        var type = ext == ".ogg" ? AudioType.OGGVORBIS : AudioType.WAV;
        var uri = path.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? path
            : "file://" + path.Replace("\\", "/");

        using var req = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(uri, type);
        var op = req.SendWebRequest();
        while (!op.isDone) {}

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success) return null;

        var clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(req);
        if (clip != null) clip.name = Path.GetFileNameWithoutExtension(path);
        return clip;
    }
}