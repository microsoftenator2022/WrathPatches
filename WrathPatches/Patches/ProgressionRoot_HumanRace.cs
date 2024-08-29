using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root;

namespace WrathPatches.Patches;

[WrathPatch("Make ProgressionRoot.HumanRace not break if added new races")]
[HarmonyPatch(typeof(ProgressionRoot), nameof(ProgressionRoot.HumanRace), MethodType.Getter)]
internal static class ProgressionRoot_HumanRace
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _)
    {
        yield return new CodeInstruction(OpCodes.Ldnull);
        yield return new CodeInstruction(OpCodes.Ret);
    }

    static BlueprintRace Postfix(BlueprintRace _, ProgressionRoot __instance)
    {
        __instance.m_HumanRaceCached ??= ResourcesLibrary.TryGetBlueprint<BlueprintRace>("0a5d473ead98b0646b94495af250fdc4");

        return __instance.m_HumanRaceCached;
    }
}
