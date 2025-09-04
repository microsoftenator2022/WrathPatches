using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

using Kingmaker.Blueprints;
using Kingmaker.Modding;

namespace WrathPatches.Patches;

[WrathPatch("Clear BlueprintReference caches when reloading OMM mods")]
[HarmonyPatch]
public static class ModReloadClearBlueprintReferenceCache
{
    static readonly HashSet<BlueprintReferenceBase> references = [];

    [HarmonyPatch(typeof(OwlcatModification), nameof(OwlcatModification.Reload))]
    [HarmonyPrefix]
    public static void ClearCaches(OwlcatModification __instance)
    {
        var rs = references
            .Where(r => __instance.Blueprints.Contains(r.guid))
            .ToArray();

#if DEBUG
        Main.Logger.Log($"Clearing {rs.Length} blueprint reference caches for {__instance.Manifest.UniqueName}");
#endif

        foreach (var r in rs)
        {
            r.Cached = null;
            _ = references.Remove(r);
        }
    }

    [HarmonyPatch(
        typeof(BlueprintReferenceBase),
        nameof(BlueprintReferenceBase.Cached),
        MethodType.Setter), HarmonyPrefix]
    static void SetCached_Prefix(BlueprintReferenceBase __instance, SimpleBlueprint value)
    {
        if (value is null)
            return;

        var assetId = __instance.Guid.ToString();

        foreach (var mod in OwlcatModificationsManager.Instance.AppliedModifications)
            if (mod.Blueprints.Contains(assetId))
            {
#if DEBUG
                Main.Logger.Log($"Tracking reference {__instance} to mod {mod.Manifest.UniqueName} blueprint {value}");
#endif

                _ = references.Add(__instance);
                break;
            }
    }
}
