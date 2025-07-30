using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Items;
using Kingmaker.UnitLogic.FactLogic;
using WrathPatches.TranspilerUtil;

namespace WrathPatches.Patches
{
    [WrathPatch("Remove unnecessary code in SetMagusFeatureActive.OnTurnOff")]
    [HarmonyPatch(typeof(SetMagusFeatureActive), nameof(SetMagusFeatureActive.OnTurnOff))]
    internal static class SetMagusFeatureActiveFix
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var HandsEquipmentSet_get_GripType = AccessTools.PropertyGetter(typeof(HandsEquipmentSet), nameof(HandsEquipmentSet.GripType));

            var toMatch = new Func<CodeInstruction, bool>[]
            {
                ci => ci.opcode == OpCodes.Ldarg_0,
                ci => ci.opcode == OpCodes.Call,
                ci => ci.opcode == OpCodes.Callvirt,
                ci => ci.opcode == OpCodes.Callvirt,
                ci => ci.Calls(HandsEquipmentSet_get_GripType),
                ci => ci.opcode == OpCodes.Pop
            };

            var match = instructions.FindInstructionsIndexed(toMatch).ToArray();

            if (match.Length != toMatch.Length)
                throw new Exception("Could not find target instructions");

            var iList = instructions.ToList();

            foreach (var (index, _) in match)
            {
                iList[index].opcode = OpCodes.Nop;
                iList[index].operand = null;
            }

            return iList;
        }
    }
}
