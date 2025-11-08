using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SPT.Reflection.Patching;

namespace DragonDen.Hitmarker.Patches;

public static class PatchManager
{
    public static void EnablePatches()
    {
        foreach (var patch in GetAllPatches()) ((ModulePatch)Activator.CreateInstance(patch)).Enable();
    }

    public static void DisablePatches()
    {
        foreach (var patch in GetAllPatches()) ((ModulePatch)Activator.CreateInstance(patch)).Disable();
    }

    public static void EnableTargetPatch(Type type)
    {
        ((ModulePatch)Activator.CreateInstance(type)).Enable();
    }

    public static void DisableTargetPatch(Type type)
    {
        ((ModulePatch)Activator.CreateInstance(type)).Disable();
    }

    private static IEnumerable<Type> GetAllPatches()
    {
        return Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.BaseType == typeof(ModulePatch) &&
                        t.GetCustomAttribute(typeof(DisablePatchAttribute)) == null);
    }
}

public class DisablePatchAttribute : Attribute
{
}