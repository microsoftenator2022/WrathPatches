using System;

using HarmonyLib;

using Kingmaker.Blueprints.JsonSystem;

namespace WrathPatches.Patches;

[HarmonyPatchCategory("Experimental")]
[WrathPatch("Make BlueprintBeingRead ThreadStatic")]
[HarmonyPatch(typeof(Json), nameof(Json.BlueprintBeingRead))]
internal static class MakeBlueprintBeingReadThreadStatic
{
    [ThreadStatic]
    static BlueprintJsonWrapper? blueprintBeingRead;

    [HarmonyPatch(MethodType.Setter)]
    [HarmonyPostfix]
    static void SetterPatch(BlueprintJsonWrapper value) =>
        blueprintBeingRead = value;

    [HarmonyPatch(MethodType.Getter)]
    [HarmonyPostfix]
    static void GetterPatch(ref BlueprintJsonWrapper? __result) => 
        __result = blueprintBeingRead;
}
