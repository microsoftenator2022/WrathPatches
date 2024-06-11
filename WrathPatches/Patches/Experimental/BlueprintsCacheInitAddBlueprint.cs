using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;

namespace WrathPatches.Experimental
{
    //[HarmonyPatchCategory("Experimental")]
    //[WrathPatch("Allow add existing in BlueprintsCache.Init")]
    //[HarmonyPatch(typeof(BlueprintsCache))]
    internal static class BlueprintsCacheInitAddBlueprint
    {
        [HarmonyPatch(nameof(BlueprintsCache.Init))]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Init_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Main.Logger.Log($"{nameof(BlueprintsCacheInitAddBlueprint)}.{nameof(Init_Transpiler)}");

            var (_, instruction) = instructions.Indexed()
                .First(i => i.item.Calls(
                    AccessTools.Method(typeof(Dictionary<BlueprintGuid, BlueprintsCache.BlueprintCacheEntry>),
                        nameof(Dictionary<BlueprintGuid, BlueprintsCache.BlueprintCacheEntry>.Add))));

            instruction.operand = AccessTools.PropertySetter(typeof(Dictionary<BlueprintGuid, BlueprintsCache.BlueprintCacheEntry>), "Item");

            return instructions;
        }
    }
}
