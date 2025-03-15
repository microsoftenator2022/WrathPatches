using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Armies.TacticalCombat.Data;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;

namespace WrathPatches.Patches.Experimental;

[HarmonyPatchCategory("Experimental")]
[WrathPatch("Fix NRE in SelectionCharacterController.CurrentSelectedCharacter")]
[HarmonyPatch(
    typeof(SelectionCharacterController),
    nameof(SelectionCharacterController.CurrentSelectedCharacter),
    MethodType.Getter)]
static class CurrentSelectedCharacterNREFix
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
    {
        var matcher = new CodeMatcher(instructions, ilGen)
            .MatchStartForward(
                CodeMatch.Calls(
                    AccessTools.PropertyGetter(
                        typeof(TurnState), nameof(TurnState.Unit))),
                CodeMatch.StoresLocal(),
                CodeMatch.LoadsLocal(),
                CodeMatch.Calls(
                    AccessTools.PropertyGetter(
                        typeof(UnitEntityData), nameof(UnitEntityData.IsDirectlyControllable))))
            .Advance(1)
            .InsertAndAdvance([new(OpCodes.Dup)]);

        return matcher
            .InsertBranchAndAdvance(OpCodes.Brtrue_S, matcher.Pos)
            .InsertAndAdvance([new(OpCodes.Pop), new(OpCodes.Ldnull), new(OpCodes.Ret)])
            .InstructionEnumeration();
    }
}
