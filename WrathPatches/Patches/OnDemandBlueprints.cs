using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Modding;

using Newtonsoft.Json;

using UnityModManagerNet;

namespace WrathPatches.Patches;

[WrathPatch("Load Blueprints on demand")]
[HarmonyPatch]
public static class OnDemandBlueprints
{
    public delegate object? ResourceLoadEvent(string guid, object? resource);

    public static event ResourceLoadEvent? BeforeResourceLoad;
    public static event ResourceLoadEvent? AfterResourceLoad;

    static bool Prepare() => !UnityModManager.modEntries.Where(me => me.Enabled).Select(me => me.Info.Id).Contains("0ToyBox0");

    static object? OnResourceLoad(ResourceLoadEvent? e, string guid, object? resource)
    {
        var subscribers = e?.GetInvocationList().OfType<ResourceLoadEvent>();

        if (subscribers is null)
            return null;

        var original = resource;

        foreach (var f in subscribers)
            resource = f(guid, resource) ?? resource;

        return resource != original ? resource : null;
    }
    
    [HarmonyPatch(typeof(OwlcatModificationsManager), nameof(OwlcatModificationsManager.OnResourceLoaded))]
    [HarmonyPrefix]
    static void OwlcatModificationsManager_OnResourceLoaded_Prefix(ref object? resource, string guid, out object? __state)
    {
        __state = OnResourceLoad(BeforeResourceLoad, guid, resource);

        if (__state != null)
            resource = __state;
    }

    // replacement will be null unless an owlmod did something
    [HarmonyPatch(typeof(OwlcatModificationsManager), nameof(OwlcatModificationsManager.OnResourceLoaded))]
    [HarmonyPostfix]
    static void OwlcatModificationsManager_OnResourceLoaded_Postfix(object? resource, string guid, ref object? replacement, object? __state)
    {
        var result = OnResourceLoad(AfterResourceLoad, guid, replacement ?? __state ?? resource);

        if (result != null)
            replacement = result;
        else if (replacement == null)
            replacement = __state;
    }

#if DEBUG
    static readonly Stopwatch timer = new();
    [HarmonyPatch(typeof(OwlcatModification), nameof(OwlcatModification.LoadBlueprints))]
    [HarmonyPrefix]
    static void OwlcatModification_LoadBlueprints_Prefix(OwlcatModification __instance)
    {
        Main.Logger.Log($"{__instance.Manifest.UniqueName} {nameof(OwlcatModification.LoadBlueprints)} start");
        timer.Restart();
    }

    [HarmonyPatch(typeof(OwlcatModification), nameof(OwlcatModification.LoadBlueprints))]
    [HarmonyPostfix]
    static void OwlcatModification_LoadBlueprints_Postfix(OwlcatModification __instance)
    {
        timer.Stop();
        Main.Logger.Log($"{__instance.Manifest.UniqueName} {nameof(OwlcatModification.LoadBlueprints)} time = {timer.ElapsedMilliseconds}ms");
    }
#endif
    static readonly MethodInfo LoadedBlueprints_TryGetValue = typeof(Dictionary<BlueprintGuid, BlueprintsCache.BlueprintCacheEntry>).GetMethod(nameof(Dictionary<BlueprintGuid, BlueprintsCache.BlueprintCacheEntry>.TryGetValue));

    // BlueprintsCache.Load will now call OwlcatModificationsManager.Instance.OnResourceLoaded when a blueprint's guid is not found in the cache.
    // This allows OMM mods to add their blueprints when they are requested instead of all at once.
    // UMM mods can also do this by subscribing to the OnDemandBlueprints.BeforeResourceLoad event
    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Load))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> BlueprintsCache_Load_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
    {
        var matcher = new CodeMatcher(instructions, ilGen);
        matcher = matcher
            .MatchStartForward(
                CodeMatch.LoadsLocal(),
                CodeMatch.Branches(),
                new(OpCodes.Ldnull),
                CodeMatch.StoresLocal(),
                new(ci => ci.opcode == OpCodes.Leave_S || ci.opcode == OpCodes.Leave));

        var leaveAndReturnNull = matcher.InstructionsWithOffsets(2, 4);

        matcher = matcher.SetAndAdvance(OpCodes.Nop, null);

        var keyNotInCacheTarget = (Label)matcher.Operand;

        matcher = matcher
            .SetOpcodeAndAdvance(OpCodes.Br_S)
            .MatchEndBackwards(
                CodeMatch.Calls(LoadedBlueprints_TryGetValue),
                CodeMatch.Branches())
            .SetOperandAndAdvance(keyNotInCacheTarget)
            .MatchEndForward(
                CodeMatch.LoadsLocal(),
                new(ci => ci.opcode == OpCodes.Isinst),
                new(ci => ci.opcode == OpCodes.Dup),
                CodeMatch.Branches(),
                new(ci => ci.opcode == OpCodes.Pop),
                CodeMatch.LoadsLocal(),
                CodeMatch.StoresLocal())
            .Advance(1);

        matcher = matcher
            .InsertAndAdvance([new(matcher.InstructionAt(-2))]);

        matcher = matcher
            .InsertBranchAndAdvance(OpCodes.Brtrue_S, matcher.Pos)
            .Insert(leaveAndReturnNull);

        return matcher.InstructionEnumeration();
    }

    public static readonly Dictionary<OwlcatModification, Dictionary<string, string>> ModBlueprints = [];
    public static string? GetModBlueprintGuid(string path, OwlcatModification mod)
    {
        try
        {
            using var file = File.OpenText(path);
            using var reader = new JsonTextReader(file);

            while (reader.Read() && reader.Value is not "AssetId") { }

            var assetId = reader.ReadAsString();
#if DEBUG
            mod.Logger.Log($"Register blueprint {assetId} = {path}");
#endif

            if (string.IsNullOrEmpty(assetId))
                return null;

            ModBlueprints.GetOrAdd(mod).Add(assetId, path);
            return assetId;
        }
        catch(Exception ex)
        {
            mod.Logger.Error($"Exception while reading blueprint {path}");
            mod.Logger.Exception(ex);
        }

        return null;
    }

    static readonly MethodInfo BlueprintJsonWrapper_Load = AccessTools.Method(typeof(BlueprintJsonWrapper), nameof(BlueprintJsonWrapper.Load));
    static readonly FieldInfo OwlcatModification_Blueprints_Field = AccessTools.Field(typeof(OwlcatModification), nameof(OwlcatModification.Blueprints));
    static readonly MethodInfo HashSet_Add = typeof(HashSet<string>).GetMethod(nameof(HashSet<string>.Add));

    // Don't try to deserialize blueprints here, just read the AssetId and add to the ModBlueprints dictionary
    [HarmonyPatch(typeof(OwlcatModification), nameof(OwlcatModification.LoadBlueprints))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> OwlcatModification_LoadBlueprints_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
    {
        var matcher = new CodeMatcher(instructions, ilGen);

        matcher = matcher
            .MatchStartForward(
                CodeMatch.IsLdarg(0),
                CodeMatch.LoadsField(OwlcatModification_Blueprints_Field),
                CodeMatch.LoadsLocal(),
                new(_ => true),
                CodeMatch.Calls(HashSet_Add));

        var end = matcher.Pos;

        matcher = matcher
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call((string path, OwlcatModification mod) => GetModBlueprintGuid(path, mod)),
                CodeInstruction.StoreLocal(1))
            .Advance(2)
            .SetAndAdvance(OpCodes.Ldloc_1, null)
            .SetAndAdvance(OpCodes.Nop, null)
            .Advance(1);
        
        // Might be unnecessary to add this null check here?
        // ---
        matcher = matcher
            .InsertBranchAndAdvance(OpCodes.Br_S, matcher.Pos)
            .Insert(new CodeInstruction(OpCodes.Pop));
        var ifNull = matcher.Pos;
        matcher = matcher
            .Advance(-2)
            .InsertBranch(OpCodes.Brfalse_S, ifNull)
            .Insert(new CodeInstruction(OpCodes.Dup));
        // ---

        matcher = matcher
            .MatchStartBackwards(CodeMatch.Calls(BlueprintJsonWrapper_Load));

        while (matcher.Pos < end)
            matcher = matcher.SetAndAdvance(OpCodes.Nop, null);

        return matcher.InstructionEnumeration();
    }

    [HarmonyPatch(typeof(OwlcatModification), nameof(OwlcatModification.OnResourceLoaded))]
    [HarmonyPrefix]
    static void OwlcatModification_OnResourceLoaded_Prefix(ref object? resource, string guid, OwlcatModification __instance, out SimpleBlueprint? __state)
    {
        __state = null;

        if (resource is null
            && __instance.Blueprints.Contains(guid)
            && ModBlueprints.GetOrAdd(__instance).TryGetValue(guid, out var path))
        {
            __instance.Logger.Log($"Adding blueprint {path}");

            try
            {
                var blueprint = BlueprintJsonWrapper.Load(path).Data;
                blueprint.OnEnable();

                resource = __state = ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(BlueprintGuid.Parse(guid), blueprint);
            }
            catch (Exception ex)
            {
                __instance.Logger.Error($"Exception while loading blueprint {path}");
                __instance.Logger.Exception(ex);
            }
        }
    }

    // Need to use __state here because the original method sets replacement to null
    [HarmonyPatch(typeof(OwlcatModification), nameof(OwlcatModification.OnResourceLoaded))]
    [HarmonyPostfix]
    static void OwlcatModification_OnResourceLoaded_Postfix(ref object? replacement, SimpleBlueprint? __state) => replacement ??= __state;
}
