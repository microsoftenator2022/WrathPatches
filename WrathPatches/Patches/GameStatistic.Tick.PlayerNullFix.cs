using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker;

namespace WrathPatches
{
    [WrathPatch("GameStatistic.Tick null Player fix")]
    [HarmonyPatch(typeof(GameStatistic), nameof(GameStatistic.Tick))]
    internal static class GameStatistic_Tick_PlayerNullFix
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var insertAfter = instructions.Indexed().FirstOrDefault(i => i.item.opcode == OpCodes.Nop);

            if (insertAfter == default)
            {
                return instructions;
            }

            var insertAtIndex = insertAfter.index + 1;

            var iList = instructions.ToList();

            var targetNop = new CodeInstruction(OpCodes.Nop);
            var jumpLabel = generator.DefineLabel();
            targetNop.labels.Add(jumpLabel);

            iList.InsertRange(insertAtIndex, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Game), nameof(Game.Player))),
                new CodeInstruction(OpCodes.Brtrue_S, jumpLabel),
                new CodeInstruction(OpCodes.Ret),
                targetNop
            });

            return iList;
        }
    }
}
