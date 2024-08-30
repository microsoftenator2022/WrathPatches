using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.Modding;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using WrathPatches;

namespace OwlModPatcherEnhancer;

[WrathPatch("Owlmod Enhancer")]
[HarmonyPatch]
public static class TheEnhance
{
    //static JsonSerializerSettings settings = new JsonSerializerSettings()
    //{

    //    TypeNameHandling = TypeNameHandling.Auto,
    //    NullValueHandling = NullValueHandling.Ignore,
    //    DefaultValueHandling = DefaultValueHandling.Ignore,
    //    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
    //    ReferenceLoopHandling = ReferenceLoopHandling.Error,
    //    Formatting = Formatting.Indented,
    //    Binder = new GuidClassBinder(),
    //    Converters = DefaultJsonSettings.CommonConverters.ToList<JsonConverter>(),
    //    ContractResolver = new OptInContractResolver(),

    //};

    static BlueprintJsonWrapper WrapperInstance = new();

    static JsonMergeSettings MergeSettings = default!;
    [HarmonyPrepare]
    static bool Prepare()
    {
        MergeSettings ??= (JsonMergeSettings)AccessTools.DeclaredField(typeof(OwlcatModificationBlueprintPatcher), "MergeSettings").GetValue(null);
#if DEBUG
        PFLog.Mods.Log($"OwlModPatcherEnhancer Prepare - MergeSettings is null? {MergeSettings == null}");
#endif
        return MergeSettings != null;
    }


    [HarmonyPatch(typeof(OwlcatModificationBlueprintPatcher), "ApplyPatchEntry", new Type[] { typeof(JObject), typeof(JObject) })]
    [HarmonyPostfix]
    public static void PostfixComponent(JObject jsonBlueprint, JObject patchEntry)
    {
        if (jsonBlueprint["Components"] is not JArray components)
            return;

        if (patchEntry["ComponentPatches"] is not JArray componentPatches)
            return;

        using IEnumerator<JToken> enumerator = componentPatches.GetEnumerator();

        while (enumerator.MoveNext())
        {
            if (enumerator.Current is not JObject currentComponentPatch)
                continue;
            var componentName = currentComponentPatch["Name"].ToString();

            try
            {
                if (componentName.IsNullOrEmpty())
                {
                    PFLog.Mods.Warning($"OwlModPatcherEnhance - a component patch with empty Name");
                    continue;
                }
                JObject? componentToPatch = null;

                using (var comps = components.GetEnumerator())
                {
                    while (comps.MoveNext())
                    {
                        var name = (comps.Current as JObject)?["name"]?.ToString();
                        {
                            if (name.IsNullOrEmpty())
                                PFLog.Mods.Warning("OwlModPatcherEnhance Found a component with null or empty name: \n" + (comps.Current as JObject));
                        }
                        if (name != componentName)
                            continue;
                        componentToPatch = comps.Current as JObject;
                    }
                }

                if (componentToPatch == null)
                {
                    PFLog.Mods.Warning($"Failed to find component with the name '{componentName}' when patching blueprint {jsonBlueprint["name"]} (guid {jsonBlueprint["AssetGuid"]}");
                }
                var patchString = currentComponentPatch["Data"];
                componentToPatch!.Merge(patchString, MergeSettings);
            }
            catch (Exception ex)
            {
                PFLog.Mods.Exception(ex, $"Error on patching blueprint {jsonBlueprint["name"]} (guid {jsonBlueprint["AssetGuid"]}) with component {componentName}");
            }
        }
    }

    [HarmonyPatch(typeof(OwlcatModificationBlueprintPatcher), nameof(OwlcatModificationBlueprintPatcher.ApplyPatch), new Type[] { typeof(SimpleBlueprint), typeof(JObject)})]
    [HarmonyPostfix]
    public static void PostfixElement(SimpleBlueprint __result, JObject patch)
    {
        if (__result is not BlueprintScriptableObject blueprint)
        {
            return;
        }

        if (patch["ElementsPatches"] is not JArray elementPatches)
        {
            return;
        }

        using IEnumerator<JToken> enumerator = elementPatches.GetEnumerator();

        while (enumerator.MoveNext())
        {
            var currentPatch = (JObject)enumerator.Current;
            var elementName = currentPatch["Name"].ToString();
            if (elementName.IsNullOrEmpty())
            {
                PFLog.Mods.Warning($"OwlModPatcherEnhancer found a patch with empty element name when patching blueprint {blueprint.name} (guid {blueprint.AssetGuid})");
                continue;
            }
            var element = blueprint.ElementsArray.FirstOrDefault(el => el.name == elementName);
            if (element == null)
            {
                PFLog.Mods.Warning($"OwlModPatcherEnhancer failed to find an element with name {elementName} when patching blueprint {blueprint.name} (guid {blueprint.AssetGuid})");
                continue;
            }
            try
            {

                var elementPatch = currentPatch["Data"].ToString();
                WrapperInstance.Data = blueprint;
                Json.BlueprintBeingRead = WrapperInstance;
                JsonConvert.PopulateObject(elementPatch, element);
                Json.BlueprintBeingRead = null;
                blueprint.RemoveFromElementsList(element);
            }
            catch (Exception ex)
            {
                PFLog.Mods.Exception(ex, $"Exception when using OwlModPatcherEnhancer to patch element {elementName} on blueprint {blueprint.name} (guid {blueprint.AssetGuid}");
            }
        }
    }

    //Disable old PatchEnhancer
    [HarmonyPatch(typeof(OwlcatModificationsManager), MethodType.Constructor, [])]
    [HarmonyPostfix]
    static void OMM_Constructor_Postfix(OwlcatModificationsManager __instance)
    {
        if (__instance.m_Settings is null)
            return;

        var enabledMods = __instance.m_Settings.EnabledModifications.Where(m => m != "PatchEnhancer").ToArray();

        if (enabledMods.Length < __instance.m_Settings.EnabledModifications.Length)
        {
            PFLog.Mods.Warning("Disabling old PatchEnhancer");

            AccessTools.Field(typeof(OwlcatModificationsManager.SettingsData), nameof(OwlcatModificationsManager.SettingsData.EnabledModifications))
                .SetValue(__instance.m_Settings, enabledMods);
        }
    }

}
