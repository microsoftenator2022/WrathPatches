using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;

using TurnBased.Controllers;

namespace WrathPatches.Patches;

[WrathPatch("Fix surprise round turns")]
[HarmonyPatch(typeof(CombatController), nameof(CombatController.HandleCombatStart))]
internal class ActingInSurpriseRoundFix
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var Timespan_get_Seconds = AccessTools.PropertyGetter(typeof(TimeSpan), nameof(TimeSpan.Seconds));

        foreach (var i in instructions)
		{
			if (i.opcode == OpCodes.Call && (MethodInfo)i.operand == Timespan_get_Seconds)
				i.operand = AccessTools.PropertyGetter(typeof(TimeSpan), nameof(TimeSpan.TotalSeconds));

			yield return i;
		}
	}
}
