using System;
using System.IO;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using Kingmaker;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Modding;

using UnityModManagerNet;

namespace WrathPatches;

[WrathPatch("Add components from mod assemblies to binder cache")]
[HarmonyPatch(typeof(OwlcatModification), "LoadAssemblies")]
internal static class OwlcatModification_LoadAssemblies_Patch
{
    [HarmonyPostfix]
    static void Postfix(OwlcatModification __instance)
    {
        var binder = (GuidClassBinder)Json.Serializer.Binder;

        foreach (var assembly in __instance.LoadedAssemblies)
        {
            foreach (var (type, guid) in assembly.GetTypes()
                .Select(type => (type, type.GetCustomAttribute<TypeIdAttribute>()?.GuidString))
                .Where(t => t.GuidString is not null))
            {

                var logMessage = $"Adding {type} with TypeId {guid} to binder cache";
                WrathPatches.Main.Logger.Log(logMessage);
                __instance.Logger.Log(logMessage);

                binder.AddToCache(type, guid);
            }
        }
    }
}

[HarmonyPatch(typeof(OwlcatModificationsManager), nameof(OwlcatModificationsManager.Start))]
static class UmmModsToGuidClassBinder
{
    static void Prefix()
    {
        var binder = (GuidClassBinder)Json.Serializer.Binder;

        foreach (var mod in UnityModManager.modEntries.Where(me => me.Started))
        {
            Main.Logger.Log($"Adding types to binder from {mod.Info.DisplayName}");

            foreach (var f in Directory.EnumerateFiles(mod.Path, "*.dll", SearchOption.AllDirectories))
            {
                Type[] types = [];

                try
                {
                    types = Assembly.LoadFrom(f).GetTypes();
                }
                catch (ReflectionTypeLoadException rtle)
                {
                    types = rtle.Types;
                }
                catch (Exception e)
                { 
                    Main.Logger.LogException(e);
                    continue;
                }

                foreach (var (type, guid) in types
                    .Select(type => (type, type.GetCustomAttribute<TypeIdAttribute>()?.GuidString))
                    .Where(t => t.GuidString is not null))
                {
                    Main.Logger.Log($"Adding {type} with TypeId {guid} to binder cache");

                    if (binder.m_GuidToTypeCache.ContainsKey(guid))
                    {
                        PFLog.Mods.Error("I told kuru this would happen");
                        PFLog.Mods.Error($"Duplicate typeid {guid} for type {type.FullName} in assembly {type.Assembly}");
                        continue;
                    }

                    binder.m_GuidToTypeCache.Add(guid, type);
                    binder.m_TypeToGuidCache.Add(type, guid);
                }
            }
        }
    }
}