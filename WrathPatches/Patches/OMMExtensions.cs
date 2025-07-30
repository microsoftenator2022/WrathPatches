using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Modding;

using Newtonsoft.Json;

using UnityModManagerNet;

using WrathPatches.TranspilerUtil;

namespace WrathPatches.Patches;

//[HarmonyPatchCategory("Experimental")]
//[WrathPatch("OwlcatModificationManager Assemblies load patch")]
//[HarmonyPatch]
internal static class LoadAssembliesFromList
{
    //[HarmonyPatch(typeof(OwlcatModification), nameof(OwlcatModification.GetFilesFromDirectory))]
    //[HarmonyPostfix]
    static IEnumerable<string> GetFilesFromDirectory_Postfix(IEnumerable<string> result, string directory)
    {
        var dirName = new DirectoryInfo(directory).Name;

        var isAssemblies = dirName.Equals("Assemblies", StringComparison.InvariantCultureIgnoreCase);

        var assembliesListJson = Directory.EnumerateFiles(directory, "AssembliesList.json").FirstOrDefault();

        var assembliesList = assembliesListJson is not null ? JsonConvert.DeserializeObject<AssembliesList>(File.ReadAllText(assembliesListJson)) : null;

        foreach (var item in result)
        {
            if (!isAssemblies)
            {
                yield return item;
                continue;
            }

            if (!Path.GetExtension(item).Equals("dll", StringComparison.InvariantCultureIgnoreCase))
                continue;

            if (assembliesList is null)
            {
                yield return item;
                continue;
            }

            if (assembliesList.AssemblyNames.Any(assName => Path.GetFileNameWithoutExtension(item).Equals(assName, StringComparison.InvariantCultureIgnoreCase)))
                yield return item;
        }
    }

    class AssembliesList
    {
        public List<string> AssemblyNames = [];
    }
}

[WrathPatch("OwlcatModificationManager: UMM mods as dependencies for OMM mods")]
[HarmonyPatch(typeof(OwlcatModificationsManager))]
internal static class UMMDependency
{
    static IEnumerable<OwlcatModificationManifest> UMMModManifests
    {
        get
        {
            foreach (var modInfo in UnityModManager.modEntries.Select(me => me.Info))
            {
                var manifest = new OwlcatModificationManifest()
                {
                    UniqueName = modInfo.Id ?? "",
                    DisplayName = modInfo.DisplayName ?? "",
                    Version = modInfo.Version ?? "",
                    Description = "",
                    Author = modInfo.Author ?? "",
                    Repository = modInfo.Repository ?? "",
                    HomePage = modInfo.HomePage ?? "",
                    Dependencies = []
                };

                yield return manifest;
            }
        }
    }

    static IEnumerable<OwlcatModification> FakeOwlmods
    {
        get
        {
            foreach (var manifest in UMMModManifests)
            {
                var fakeMod = FormatterServices.GetUninitializedObject(typeof(OwlcatModification));

                typeof(OwlcatModification).GetField(nameof(OwlcatModification.Manifest)).SetValue(fakeMod, manifest);

                yield return (OwlcatModification)fakeMod;
            }
        }
    }

    [HarmonyPatch(nameof(OwlcatModificationsManager.CheckDependencies))]
    [HarmonyPrefix]
    static void InjectUMMMods(ref List<OwlcatModification> appliedModifications)
    {
        appliedModifications = appliedModifications.Concat(FakeOwlmods).ToList();
    }

    //static OwlcatModificationManifest? TryGetUMMMod(OwlcatModificationManifest.Dependency dependency) =>
    //    UMMModManifests.FirstOrDefault(m => m.UniqueName == dependency.Name);

    //static FieldInfo DependencyLocalValue =>
    //    typeof(OwlcatModificationsManager).GetNestedTypes(AccessTools.all)
    //        .Select(t => t.GetField("dependency", AccessTools.all))
    //        .SkipIfNull()
    //        .Single(fi => fi.FieldType == typeof(OwlcatModificationManifest.Dependency));

    //[HarmonyPatch(nameof(OwlcatModificationsManager.CheckDependencies))]
    //[HarmonyTranspiler]
    //static IEnumerable<CodeInstruction> CheckDependencies_Transpiler(IEnumerable<CodeInstruction> instructions)
    //{
    //    var match = instructions.FindInstructionsIndexed(
    //    [
    //        ci => ci.opcode == OpCodes.Stloc_S && ci.operand is LocalBuilder { LocalIndex: 5 },
    //        ci => ci.opcode == OpCodes.Ldloc_S && ci.operand is LocalBuilder { LocalIndex: 5 },
    //        ci => ci.opcode == OpCodes.Brtrue_S
    //    ]).ToArray();

    //    if (match.Length != 3)
    //        throw new Exception($"Could not find instructions to patch");

    //    var dependencyFound = match[2].instruction.operand;

    //    var iList = instructions.ToList();

    //    var getUMMDependency = new CodeInstruction[]
    //    {
    //        new(OpCodes.Ldloc_S, 4),
    //        new(OpCodes.Ldfld, DependencyLocalValue),
    //        CodeInstruction.Call((OwlcatModificationManifest.Dependency dependency) => TryGetUMMMod(dependency)),
    //        new(OpCodes.Dup),
    //        new(OpCodes.Stloc_S, 5),
    //        new(OpCodes.Brtrue_S, dependencyFound)
    //    };

    //    iList.InsertRange(match[2].index + 1, getUMMDependency);

    //    return iList;
    //}
}

[WrathPatch("OwlcatModificationManager: Compare OMM mod versions like UMM")]
[HarmonyPatch(typeof(OwlcatModificationsManager))]
static class OMMVersionCheck
{
    // Return true if the version is too low
    static bool VersionCheck(string? thisVersionString, string? otherVersionString)
    {
        if (thisVersionString is null || otherVersionString is null) return true;

        var thisVersion = UnityModManager.ParseVersion(thisVersionString);
        var otherVersion = UnityModManager.ParseVersion(otherVersionString);

        return otherVersion < thisVersion;
    }

    [HarmonyPatch(nameof(OwlcatModificationsManager.CheckDependencies))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> CheckDependencies_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
#if DEBUG
        var patched = false;
#endif
        var string_op_Inequality = AccessTools.Method(typeof(string), "op_Inequality", [typeof(string), typeof(string)]);

        foreach (var i in instructions)
        {
            if (i.Calls(string_op_Inequality))
            {
#if DEBUG
                patched = true;
#endif

                //yield return CodeInstruction.Call((string a, string b) => VersionCheck(a, b)).WithLabels(i.labels);
                //continue;

                i.operand = AccessTools.Method(typeof(OMMVersionCheck), nameof(VersionCheck));
            }
            
            yield return i;
        }

#if DEBUG
        if (!patched)
            throw new Exception("Could not find instructions to patch");
#endif
    }
}