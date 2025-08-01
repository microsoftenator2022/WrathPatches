using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;

using TurnBased.Controllers;

namespace WrathPatches.Patches;

[WrathPatch("Fix surprise round turns")]
[HarmonyPatch]
internal class ActingInSurpriseRoundFix
{
	static IEnumerable<MethodInfo> TargetMethods()
	{
		yield return AccessTools.Method(typeof(CombatController), nameof(CombatController.HandleCombatStart));
        foreach (var m in typeof(CombatController).GetNestedTypes(AccessTools.all)
			.SelectMany(AccessTools.GetDeclaredMethods)
			.Where(m => m.Name.Contains(nameof(CombatController.HandleCombatStart))))
			yield return m;
	}

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
	{
		var Timespan_get_Seconds = AccessTools.PropertyGetter(typeof(TimeSpan), nameof(TimeSpan.Seconds));

        foreach (var i in instructions)
		{
			if (i.opcode == OpCodes.Call && (MethodInfo)i.operand == Timespan_get_Seconds)
			{
				i.operand = AccessTools.PropertyGetter(typeof(TimeSpan), nameof(TimeSpan.TotalSeconds));
#if DEBUG
				Main.Logger.Log($"[{nameof(ActingInSurpriseRoundFix)}] patched method {__originalMethod.FullDescription()}");
#endif
			}


            yield return i;
		}
	}
}
