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

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;

            var harmony = new Harmony(modEntry.Info.Id);

            foreach (var assFile in Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.patch.dll"))
            {
                Assembly? ass = null;

                try
                {
                    Logger.Log($"Loading {assFile}");

                    ass = Assembly.LoadFrom(assFile);

                    if (ass is null)
                    {
                        Logger.Log($"Failed to load assembly");
                        continue;
                    }

                    Logger.Log($"Running patches from {ass}");

                    harmony.PatchAll(ass);
                }
                catch (HarmonyException hex)
                {
                    Logger.Error($"Exception while executing patch from {ass}");
                    Logger.LogException(hex);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Exception while trying to run patches from {ass?.ToString() ?? assFile.ToString()}");
                    Logger.LogException(ex);
                }
            }

#if DEBUG
            Logger.Log("Running experimental patches");
            try
            {
                harmony.PatchAll();
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
