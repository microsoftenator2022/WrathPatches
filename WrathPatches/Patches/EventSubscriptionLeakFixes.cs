using HarmonyLib;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.UI._ConsoleUI.TurnBasedMode;
using Kingmaker.UI.MVVM._PCView.ActionBar;
using Kingmaker.UI.MVVM._VM.ActionBar;
using Kingmaker.UnitLogic.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using WrathPatches.TranspilerUtil;

namespace WrathPatches.Patches
{
    [HarmonyPatch()]
    partial class EventSubscriptionLeakFixes
    {
        [HarmonyPatch(typeof(ActionBarVM), MethodType.Constructor)]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> DoubleSubscribe_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var toMatch = new Func<CodeInstruction, bool>[]
            {
                ci => ci.opcode == OpCodes.Ldarg_0,
                ci => ci.Calls(AccessTools.Method(typeof(EventBus), nameof(EventBus.Subscribe), [typeof(object)])),
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
