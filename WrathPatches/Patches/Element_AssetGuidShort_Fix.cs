using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker;
using Kingmaker.ElementsSystem;

namespace WrathPatches
{
    [HarmonyPatch(typeof(Element))]
    static class Element_AssetGuidShort_Fix
    {
        static bool handleNullName(Element __instance, ref string __result)
        {
            __result = "anonymous";

            return !string.IsNullOrEmpty(__instance.name);
        }

        [HarmonyPatch(nameof(Element.AssetGuidShort), MethodType.Getter)]
        [HarmonyPrefix]
        static bool AssetGuidShort_Prefix(Element __instance, ref string __result) =>
            handleNullName(__instance, ref __result);

        [HarmonyPatch(nameof(Element.AssetGuid), MethodType.Getter)]
        [HarmonyPrefix]
        static bool AssetGuid_Prefix(Element __instance, ref string __result) =>
            handleNullName(__instance, ref __result);
    }
}
