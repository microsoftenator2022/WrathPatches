using System;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Modding;

namespace WrathPatches
{
    [WrathPatch("Add components from mod assemblies to binder cache")]
    [HarmonyPatch(typeof(OwlcatModification), "LoadAssemblies")]
    internal static class OwlcatModification_LoadAssemblies_Patch
    {
        [HarmonyPostfix]
        static void Postfix(OwlcatModification __instance)
        {
            foreach (var assembly in __instance.LoadedAssemblies)
            {
                var binder = (GuidClassBinder)Json.Serializer.Binder;

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
}
