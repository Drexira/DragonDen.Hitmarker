using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace DragonDen.Hitmarker.Utilities;

internal static class SoundBank
{
    private static AudioSource source;
    private static AudioClip hitClip;
    private static AudioClip killClip;
    private static string baseSoundsDir;
    private static string cachedHitName;
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
        var killName = Settings.KillSoundFile.Value?.Trim() ?? "";

        if (!string.Equals(hitName, cachedHitName, StringComparison.OrdinalIgnoreCase))
        {
            hitClip = TryLoadWavSafe(Path.Combine(baseSoundsDir, hitName));
            cachedHitName = hitName;
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
            return LoadPcmWav(path);
        }
        catch
        {
            return null;
        }
    }

    private static AudioClip LoadPcmWav(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);

        var riff = new string(br.ReadChars(4));
        if (riff != "RIFF") return null;
        br.ReadInt32();
        var wave = new string(br.ReadChars(4));
        if (wave != "WAVE") return null;

        var channels = 0;
        var sampleRate = 0;
        var bitsPerSample = 0;
        byte[] pcm = null;

        while (br.BaseStream.Position < br.BaseStream.Length)
        {
            var id = new string(br.ReadChars(4));
            var size = br.ReadInt32();

            if (id == "fmt ")
            {
                var audioFormat = br.ReadInt16();
                channels = br.ReadInt16();
                sampleRate = br.ReadInt32();
                br.ReadInt32();
                br.ReadInt16();
                bitsPerSample = br.ReadInt16();
                var remain = size - 16;
                if (remain > 0) br.ReadBytes(remain);
                if (audioFormat != 1) return null;
            }
            else if (id == "data")
            {
                pcm = br.ReadBytes(size);
            }
            else
            {
                br.ReadBytes(size);
            }
        }

        if (pcm == null) return null;
        if (channels < 1) return null;
        if (sampleRate < 8000) return null;
        if (bitsPerSample != 16 && bitsPerSample != 8) return null;

        float[] samples;

        if (bitsPerSample == 16)
        {
            var count = pcm.Length / 2;
            samples = new float[count];
            for (var i = 0; i < count; i++)
            {
                var s = BitConverter.ToInt16(pcm, i * 2);
                samples[i] = s / 32768f;
            }
        }
        else
        {
            var count = pcm.Length;
            samples = new float[count];
            for (var i = 0; i < count; i++) samples[i] = (pcm[i] - 128) / 128f;
        }

        var ch = Mathf.Clamp(channels, 1, 2);
        var clip = AudioClip.Create(Path.GetFileNameWithoutExtension(path), samples.Length / ch, ch, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}