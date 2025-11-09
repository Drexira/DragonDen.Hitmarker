using System;
using DragonDen.Hitmarker.Models;
using UnityEngine;

namespace DragonDen.Hitmarker.Utilities;

internal static class EventBus
{
    public static event Action<DamageEvent> OnDamage;
    public static event Action<DamageEvent> OnHeadshot;
    public static event Action<DamageEvent> OnKill;

    public static void RaiseDamage(DamageEvent e)
    {
        e.Time = Time.unscaledTime;
        OnDamage?.Invoke(e);
    }
    
    public static void RaiseHeadshot(DamageEvent e)
    {
        e.Time = Time.unscaledTime;
        OnHeadshot?.Invoke(e);
    }

    public static void RaiseKill(DamageEvent e)
    {
        e.Time = Time.unscaledTime;
        OnKill?.Invoke(e);
    }
}