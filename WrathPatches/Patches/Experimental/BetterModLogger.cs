using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Localization.Shared;
using Kingmaker.Modding;
using Kingmaker.UnitLogic.FactLogic;

using Newtonsoft.Json;

using Owlcat.Runtime.Core.Logging;

namespace WrathPatches.Patches.Experimental;

[WrathPatch("Add per-OwlMod log sinks")]
[HarmonyPatchCategory("Experimental")]
[HarmonyPatch]
static class BetterModLogger
{
    [HarmonyPatch(typeof(OwlcatModification), MethodType.Constructor, [typeof(string), typeof(string), typeof(OwlcatModificationManifest), typeof(Exception)])]
    [HarmonyPostfix]
    static void AddModLogSink(OwlcatModification __instance, string dataFolderPath)
    {
        Main.Logger.Log($"{__instance.Manifest.UniqueName}.Logger.Name = {__instance.Logger?.Name}");

        if (__instance.Logger is null || __instance.Logger.Name != __instance.Manifest.UniqueName)
            return;

        var path = Path.GetDirectoryName(__instance.DataFilePath);

        var fileName = $"{OwlcatModification.InvalidPathCharsRegex.Replace(__instance.Manifest.UniqueName, "")}_Log.txt";

        Main.Logger.Log($"Log file path: {Path.Combine(path, fileName)}");

        var sink = new UberLoggerFilter(new UberLoggerFile(fileName, path), LogSeverity.Disabled, [__instance.Manifest.UniqueName]);

        Owlcat.Runtime.Core.Logging.Logger.Instance.AddLogger(sink, false);
    }
}

[WrathPatch("Use OwlMod logger in place of PFLog.Mods")]
[HarmonyPatchCategory("Experimental")]
[HarmonyPatch]
static class PatchModLogInvocations
{
    static IEnumerable<MethodInfo> TargetMethods() =>
        AccessTools.GetDeclaredMethods(typeof(OwlcatModification)).Where(m => !m.IsStatic && !m.IsGenericMethod);

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var i in instructions)
        {
            if (i.Calls(AccessTools.PropertyGetter(typeof(PFLog), nameof(PFLog.Mods))))
            {
                yield return new(OpCodes.Ldarg_0);

                i.opcode = OpCodes.Ldfld;
                i.operand = AccessTools.Field(typeof(OwlcatModification), nameof(OwlcatModification.Logger));
            }

            yield return i;
        }
    }
}

[WrathPatch("Catch and log Blueprint patch exceptions")]
[HarmonyPatchCategory("Experimental")]
[HarmonyPatch(typeof(OwlcatModification), nameof(OwlcatModification.TryPatchBlueprint))]
static class CatchBlueprintPatchExceptions
{
    static Exception? Finalizer(Exception __exception, OwlcatModification __instance)
    {
        if (__exception is not null)
        __instance.Logger.Exception(__exception);

        return null;
    }
}
