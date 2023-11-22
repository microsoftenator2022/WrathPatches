using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using UnityModManagerNet;

namespace WrathPatches
{
    public static class Main
    {
        private static UnityModManager.ModEntry? ModEntry;

        public static UnityModManager.ModEntry.ModLogger Logger => ModEntry!.Logger;

        static void RunPatches(IEnumerable<(Type, PatchClassProcessor)> typesAndPatches)
        {
            foreach (var (t, pc) in typesAndPatches)
            {
                try
                {
                    Logger.Log($"Running patch class {t.Name}");
                    pc.Patch();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Exception in patch class {t.Name}");
                    Logger.LogException(ex);
                }
            }
        }

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;

            var harmony = new Harmony(modEntry.Info.Id);

            var patchClasses = AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly())
                .Select(t => (t, pc: harmony.CreateClassProcessor(t)))
                .Where(tuple => tuple.pc.HasPatchAttribute())
                .ToArray();

            RunPatches(patchClasses.Where(tuple => tuple.pc.GetCategory() != "Experimental"));

            //foreach (var (t, pc) in patchClasses.Where(tuple => tuple.pc.GetCategory() != "Experimental"))
            //{
            //    try
            //    {
            //        Logger.Log($"Running patch class {t.Name}");
            //        pc.Patch();
            //    }
            //    catch (Exception ex)
            //    {
            //        Logger.Error($"Exception in patch class {t.Name}");
            //        Logger.LogException(ex);
            //    }
            //}

            //foreach (var assFile in Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.patch.dll"))
            //{
            //    Assembly? ass = null;

            //    try
            //    {
            //        Logger.Log($"Loading {assFile}");

            //        ass = Assembly.LoadFrom(assFile);

            //        if (ass is null)
            //        {
            //            Logger.Log($"Failed to load assembly");
            //            continue;
            //        }

            //        Logger.Log($"Running patches from {ass}");

            //        harmony.PatchAll(ass);
            //    }
            //    catch (HarmonyException hex)
            //    {
            //        Logger.Error($"Exception while executing patch from {ass}");
            //        Logger.LogException(hex);
            //    }
            //    catch (Exception ex)
            //    {
            //        Logger.Error($"Exception while trying to run patches from {ass?.ToString() ?? assFile.ToString()}");
            //        Logger.LogException(ex);
            //    }
            //}

#if DEBUG
            Logger.Log("Running experimental patches");
            try
            {
                RunPatches(patchClasses.Where(tuple => tuple.pc.GetCategory() == "Experimental"));
                //harmony.PatchCategory("Experimental");
                //harmony.PatchAll();
            }
            catch(Exception ex)
            {
                Logger.LogException(ex);
            }
#endif

            return true;
        }
    }
}
