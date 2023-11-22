using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.UI.Selection;

using WrathPatches.TranspilerUtil;

namespace WrathPatches
{
    [HarmonyPatch(typeof(UnitMarkManager), nameof(UnitMarkManager.LateUpdate))]
    internal static class UnitMarkManager_LateUpdate
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var match = new Func<CodeInstruction, bool>[]
            {
                ci => ci.opcode == OpCodes.Ldarg_0,
                ci => ci.LoadsField(typeof(UnitMarkManager).GetField(nameof(UnitMarkManager.m_Marks), AccessTools.all)),
                ci => ci.Calls(AccessTools.PropertyGetter(typeof(Dictionary<string, UIDecalBase>), nameof(Dictionary<string, UIDecalBase>.Values))),
                ci => ci.Calls(typeof(Dictionary<string, UIDecalBase>.ValueCollection).GetMethod(nameof(Dictionary<string, UIDecalBase>.ValueCollection.GetEnumerator)))
            };

            var iList = instructions.ToList();

            var iMatch = instructions.FindInstructionsIndexed(match);

            if (!iMatch.Any()) return instructions;

            var index = iMatch.First().index + 2;

            iList.Insert(index, new CodeInstruction(OpCodes.Newobj,
                AccessTools.Constructor(typeof(Dictionary<string, UIDecalBase>), new[] { typeof(Dictionary<string, UIDecalBase>) } )));

            return iList;
        }
    }
}
