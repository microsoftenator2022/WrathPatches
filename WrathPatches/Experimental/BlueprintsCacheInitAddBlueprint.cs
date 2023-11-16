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
    [HarmonyPatch(typeof(BlueprintsCache))]
    internal static class BlueprintsCacheInitAddBlueprint
    {
        [HarmonyPatch(nameof(BlueprintsCache.Init))]
        internal static IEnumerable<CodeInstruction> Init_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var (_, instruction) = instructions.Indexed()
                .First(i => i.item.Calls(
                    AccessTools.Method(typeof(Dictionary<BlueprintGuid, BlueprintsCache.BlueprintCacheEntry>),
                        nameof(Dictionary<BlueprintGuid, BlueprintsCache.BlueprintCacheEntry>.Add))));

            instruction.operand = AccessTools.PropertySetter(typeof(BlueprintsCache), "Item");

            return instructions;
        }
    }
}
