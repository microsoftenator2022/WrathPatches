using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;

namespace WrathPatches.Patches;

[WrathPatch("Non-matching BlueprintComponent.OwnerBlueprint warning")]
[HarmonyPatch]
internal class OwnerBlueprintWarning
{
    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Load))]
    [HarmonyPostfix]
    static void Postfix(SimpleBlueprint __result, BlueprintGuid guid)
    {
        if (__result is not BlueprintScriptableObject blueprint)
            return;

        foreach (var c in blueprint.ComponentsArray)
        {
            if (c.OwnerBlueprint != blueprint)
            {
                Main.Logger.Warning($"In blueprint {guid} \"{blueprint.name}\": " +
                    $"Non-matching OwnerBlueprint {c.OwnerBlueprint?.ToString() ?? "NULL"} on {c.GetType()} \"{c.name}\"");
            }
        }
    }
}
