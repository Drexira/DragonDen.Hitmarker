using System.Collections.Generic;
using System.Linq;
using DragonDen.Hitmarker.Models;
using DragonDen.Hitmarker.Utilities;
using UnityEngine;

namespace DragonDen.Hitmarker.Features;

public class HitmarkerController : MonoBehaviour
{
    private Camera _cam;
    private readonly List<HitEntry> hits = new();
    private readonly List<Rect> _occupied = new();

    private void Start()
    {
        _cam = Camera.main;
    }

    private void Update()
    {
        var now = Time.unscaledTime;
        if (hits.Count > 0)
        {
            var keep = Mathf.Max(Settings.HitmarkerFadeSeconds.Value, Settings.NumbersLifetimeSeconds.Value);
            hits.RemoveAll(h => now - h.Evt.Time > keep);
        }
    }

    private void OnEnable()
    {
        EventBus.OnDamage += OnDamage;
        EventBus.OnHeadshot += OnHeadshot;
        EventBus.OnKill   += OnKill;
    }

    private void OnDisable()
    {
        EventBus.OnDamage -= OnDamage;
        EventBus.OnHeadshot -= OnHeadshot;
        EventBus.OnKill   -= OnKill;
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        _occupied.Clear();

        if (Settings.HitmarkerEnabled.Value) DrawHitmarker();
        if (Settings.NumbersEnabled.Value)   DrawNumbers();
    }

    private void OnDamage(DamageEvent e)
    {
        if (!e.IsLocalAttacker) return;
        hits.Add(new HitEntry { Evt = e });
        SoundBank.PlayHit();
    }
    
    private void OnHeadshot(DamageEvent e)
    {
        if (!e.IsLocalAttacker) return;
        hits.Add(new HitEntry { Evt = e });
        SoundBank.PlayHeadshot();
    }

    private void OnKill(DamageEvent e)
    {
        if (!e.IsLocalAttacker) return;
        hits.Add(new HitEntry { Evt = e, IsKill = true });
        SoundBank.PlayKill();
    }

    private static Color PickHitColor(DamageEvent e, bool isKill)
    {
        if (isKill) return Settings.HitmarkerKillColor.Value;
        if (e.IsHeadshot) return Settings.HeadshotColor.Value;
        if (e.IsArmorHit && e.BodyDamage <= 0.01f) return Settings.ArmorHitColor.Value;
        return Settings.HitmarkerColor.Value;
    }

    private void DrawHitmarker()
    {
        var last = hits.LastOrDefault();
        if (last == null) return;

        var e = last.Evt;
        var age = Time.unscaledTime - e.Time;
        if (age > Settings.HitmarkerFadeSeconds.Value) return;

        var t = Mathf.Clamp01(1f - age / Settings.HitmarkerFadeSeconds.Value);
        var baseCol = PickHitColor(e, last.IsKill);
        baseCol.a = Settings.HitmarkerOpacity.Value * t;

        var cx = Screen.width * 0.5f;
        var cy = Screen.height * 0.5f;
        var s = Settings.HitmarkerSizePx.Value;

        if (Settings.HitmarkerStyleMode.Value == HitmarkerStyle.Image)
        {
            Texture2D tex =
                last.IsKill ? TextureBank.HitmarkerKill() :
                e.IsHeadshot ? TextureBank.HitmarkerHeadshot() :
                TextureBank.Hitmarker();

            var size = s * 2f;
            if (tex)
            {
                var r = new Rect(cx - size, cy - size, size * 2f, size * 2f);
                var prev = GUI.color;
                GUI.color = baseCol;
                GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
                GUI.color = prev;
                return;
            }
        }

        var pulse = (e.IsHeadshot && Settings.HeadshotPulse.Value) || last.IsKill;
        GLDrawer.DrawCross(cx, cy, s, 2f, baseCol, Settings.HitmarkerStyleMode.Value,
            Settings.HeadshotColor.Value, pulse);
    }

    private void DrawNumbers()
    {
        if (hits.Count == 0) return;

        var now = Time.unscaledTime;
        var cx = Screen.width * 0.5f;
        var cy = Screen.height * 0.5f;

        var visible = hits.Where(h => now - h.Evt.Time <= Settings.NumbersLifetimeSeconds.Value)
            .OrderBy(h => h.Evt.Time)
            .ToList();

        if (!Settings.NumbersAtHitPosition.Value)
        {
            var row = 0;
            var offsetX = Settings.HitmarkerSizePx.Value + 12f;
            foreach (var h in visible)
            {
                DrawNumberLine(h.Evt, new Vector2(cx + offsetX, cy), row, true, TextAnchor.MiddleLeft);
                row++;
            }
            return;
        }

        foreach (var h in visible)
        {
            var pos = ComputeScreenPointForHit(h.Evt.WorldPos, Settings.NumbersEdgeClampPadding.Value);
            var font = Settings.NumbersFontSize.Value;
            var anchor = TextAnchor.MiddleCenter;
            var pref = MeasureFullLineRect(h.Evt, pos, font, anchor);
            var placed = PlaceWithoutOverlap(pref, stepY: font + 6f, maxSteps: 20);
            var drawPos = new Vector2(pos.x, placed.center.y);
            DrawNumberLine(h.Evt, drawPos, 0, true, anchor);
        }
    }

    private Rect MeasureFullLineRect(DamageEvent e, Vector2 basePos, int font, TextAnchor anchor)
    {
        BuildLineStrings(e, out var main, out var arm, out var ric, out _);

        var wMain = GLDrawer.MeasureTextWidth(main, font, Settings.Font);
        var wArm  = string.IsNullOrEmpty(arm) ? 0f : GLDrawer.MeasureTextWidth(arm, font, Settings.Font);
        var wRic  = string.IsNullOrEmpty(ric) ? 0f : GLDrawer.MeasureTextWidth(ric, font, Settings.Font);

        float padMainToArm = 16f;
        float padAfterArm  = 8f;

        var lineW = wMain
                    + (wArm > 0f ? padMainToArm + wArm : 0f)
                    + (wRic > 0f ? padAfterArm + wRic : 0f)
                    + 8f;

        var lineH = font + 6f;
        var rect = new Rect(basePos.x, basePos.y, lineW, lineH);

        switch (anchor)
        {
            case TextAnchor.MiddleCenter:
                rect.x -= rect.width * 0.5f;
                rect.y -= rect.height * 0.5f;
                break;
            case TextAnchor.MiddleLeft:
                rect.y -= rect.height * 0.5f;
                break;
            case TextAnchor.MiddleRight:
                rect.x -= rect.width;
                rect.y -= rect.height * 0.5f;
                break;
        }
        return rect;
    }

    private Rect PlaceWithoutOverlap(Rect preferred, float stepY, int maxSteps)
    {
        if (!IntersectsAny(preferred))
        {
            _occupied.Add(preferred);
            return preferred;
        }

        for (int i = 1; i <= maxSteps; i++)
        {
            var dy = (i % 2 == 1 ? +1 : -1) * Mathf.Ceil(i * 0.5f) * stepY;
            var candidate = preferred;
            candidate.y += dy;
            candidate.y = Mathf.Clamp(candidate.y, 0f, Screen.height - candidate.height);

            if (!IntersectsAny(candidate))
            {
                _occupied.Add(candidate);
                return candidate;
            }
        }
        _occupied.Add(preferred);
        return preferred;
    }

    private bool IntersectsAny(Rect r)
    {
        for (int i = 0; i < _occupied.Count; i++)
            if (r.Overlaps(_occupied[i]))
                return true;
        return false;
    }

    private static void BuildLineStrings(DamageEvent e, out string main, out string armor, out string ricochet, out int dmgInt)
    {
        int fleshInt = Mathf.RoundToInt(Mathf.Max(0f, e.BodyDamage));
        int armorInt = Mathf.RoundToInt(Mathf.Max(0f, e.ArmorDamage));

        var template = Settings.NumbersTemplate.Value ?? "{dmg}";
        bool usesTokens = template.Contains("{flesh}") || template.Contains("{armor}");

        if (usesTokens)
        {
            string fleshStr = fleshInt > 0 ? fleshInt.ToString() : string.Empty;
            string armorStr = armorInt > 0 ? armorInt.ToString() : string.Empty;

            string baseMain = template
                .Replace("{flesh}", fleshStr)
                .Replace("{bp}", e.BodyPart ?? string.Empty)
                .Replace("{dmg}", (fleshInt + armorInt).ToString());

            if (template.Contains("{armor}"))
            {
                main  = baseMain.Replace("{armor}", string.Empty);
                armor = armorInt > 0 ? "(" + armorStr + ")" : string.Empty;
            }
            else
            {
                main  = baseMain;
                armor = string.Empty;
            }

            main = main.Replace("( )", "").Replace("()", "");
            while (main.Contains("  ")) main = main.Replace("  ", " ");
            main = main.Trim();

            ricochet = e.Ricochet ? "Ricochet" : string.Empty;
            dmgInt = fleshInt;
            return;
        }

        dmgInt = Mathf.RoundToInt(Mathf.Max(0f, e.BodyDamage > 0f ? e.BodyDamage : e.DamageAmount));
        main = template
            .Replace("{dmg}", dmgInt.ToString())
            .Replace("{bp}", e.BodyPart ?? string.Empty);

        armor = armorInt >= 1 ? "(" + armorInt + ")" : string.Empty;
        ricochet = e.Ricochet ? "Ricochet" : string.Empty;
    }

    private static void DrawNumberLine(DamageEvent e, Vector2 basePos, int rowOffset, bool riseByAge, TextAnchor anchor)
    {
        var now = Time.unscaledTime;
        var age = now - e.Time;
        var t = Mathf.Clamp01(age / Settings.NumbersLifetimeSeconds.Value);
        var fade = 1f - t;
        var rise = riseByAge ? Mathf.Lerp(0f, Settings.NumbersRisePixels.Value, t) : 0f;

        var font = Settings.NumbersFontSize.Value;

        var mainCol = PickHitColor(e, e.VictimIsDead);
        mainCol.a *= fade;

        BuildLineStrings(e, out var mainText, out var armText, out var ricText, out _);

        var pos = new Vector2(basePos.x, basePos.y - rise + rowOffset * (font + 4f));
        GLDrawer.DrawText(mainText, pos, font, mainCol, anchor, Settings.Font, Settings.NumbersBackdrop.Value);

        float mainW = GLDrawer.MeasureTextWidth(mainText, font, Settings.Font);
        float armW  = string.IsNullOrEmpty(armText) ? 0f : GLDrawer.MeasureTextWidth(armText, font, Settings.Font);
        float padMainToArm = 16f;
        float padAfterArm  = 8f;

        if (!string.IsNullOrEmpty(armText))
        {
            var armCol = Settings.ArmorHitColor.Value;
            armCol.a *= fade;

            Vector2 armPos;
            TextAnchor armAnchor;

            if (anchor == TextAnchor.MiddleRight)
            {
                float x = pos.x - mainW - padMainToArm;
                armPos = new Vector2(x, pos.y);
                armAnchor = TextAnchor.MiddleRight;
            }
            else if (anchor == TextAnchor.MiddleCenter)
            {
                float x = pos.x + mainW * 0.5f + padMainToArm;
                armPos = new Vector2(x, pos.y);
                armAnchor = TextAnchor.MiddleLeft;
            }
            else
            {
                float x = pos.x + mainW + padMainToArm;
                armPos = new Vector2(x, pos.y);
                armAnchor = TextAnchor.MiddleLeft;
            }

            GLDrawer.DrawText(armText, armPos, font, armCol, armAnchor, Settings.Font, Settings.NumbersBackdrop.Value);

            if (!string.IsNullOrEmpty(ricText))
            {
                var tagCol = Settings.NumbersColor.Value;
                tagCol.a *= fade;

                Vector2 ricPos;
                TextAnchor ricAnchor;

                if (anchor == TextAnchor.MiddleRight)
                {
                    float x = armPos.x - padAfterArm;
                    ricPos = new Vector2(x, pos.y);
                    ricAnchor = TextAnchor.MiddleRight;
                }
                else
                {
                    float x = armPos.x + armW + padAfterArm;
                    ricPos = new Vector2(x, pos.y);
                    ricAnchor = TextAnchor.MiddleLeft;
                }

                GLDrawer.DrawText(ricText, ricPos, font, tagCol, ricAnchor, Settings.Font, Settings.NumbersBackdrop.Value);
            }
        }
        else if (!string.IsNullOrEmpty(ricText))
        {
            var tagCol = Settings.NumbersColor.Value;
            tagCol.a *= fade;

            Vector2 ricPos;
            TextAnchor ricAnchor;

            if (anchor == TextAnchor.MiddleRight)
            {
                float x = pos.x - mainW - padAfterArm;
                ricPos = new Vector2(x, pos.y);
                ricAnchor = TextAnchor.MiddleRight;
            }
            else if (anchor == TextAnchor.MiddleCenter)
            {
                float x = pos.x + mainW * 0.5f + padAfterArm;
                ricPos = new Vector2(x, pos.y);
                ricAnchor = TextAnchor.MiddleLeft;
            }
            else
            {
                float x = pos.x + mainW + padAfterArm;
                ricPos = new Vector2(x, pos.y);
                ricAnchor = TextAnchor.MiddleLeft;
            }

            GLDrawer.DrawText(ricText, ricPos, font, tagCol, ricAnchor, Settings.Font, Settings.NumbersBackdrop.Value);
        }
    }

    private Vector2 ComputeScreenPointForHit(Vector3? world, float edgePad)
    {
        if (!_cam || !world.HasValue)
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        var sp = _cam.WorldToViewportPoint(world.Value);

        if (sp.z < 0f)
        {
            var x = sp.x < 0.5f ? edgePad : 1f - edgePad;
            var y = 0.5f;
            return new Vector2(x * Screen.width, (1f - y) * Screen.height);
        }

        sp.x = Mathf.Clamp(sp.x, edgePad, 1f - edgePad);
        sp.y = Mathf.Clamp(sp.y, edgePad, 1f - edgePad);

        return new Vector2(sp.x * Screen.width, (1f - sp.y) * Screen.height);
    }

    private class HitEntry
    {
        public DamageEvent Evt;
        public bool IsKill;
    }
}