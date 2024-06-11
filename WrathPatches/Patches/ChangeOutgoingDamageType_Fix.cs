using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;

namespace WrathPatches.Patches
{
#if DEBUG
    [WrathPatch("Fix ChangeOutgoingDamageType Component")]
    [HarmonyPatch(typeof(ChangeOutgoingDamageType), nameof(ChangeOutgoingDamageType.ChangeType))]
    static class ChangeOutgoingDamageType_Fix
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var index = instructions.FindIndex(ci => ci.opcode == OpCodes.And);

            if (index < 0)
            {
                Main.Logger.Error($"{nameof(ChangeOutgoingDamageType_Fix)}: Could not find target instruction.");
                return instructions;
            }

            var iList = instructions.ToList();

            iList[index].opcode = OpCodes.Nop; // bitwise AND -> nop

            iList.Insert(index - 2, iList[index + 1]); // brfalse.s

            return iList;
        }
    }
#endif
}
