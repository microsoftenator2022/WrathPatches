using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using UnityModManagerNet;

namespace WrathPatches
{
    static partial class Main
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

            static bool replaceHarmonyVersion(Version harmonyVersion)
            {
                var ignoreFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "IgnoreHarmonyVersion.txt");
                if (File.Exists(ignoreFilePath))
                {
                    var text = File.ReadAllText(ignoreFilePath).Trim();

                    return  !Version.TryParse(text, out var ignoreVersion) || ignoreVersion <= harmonyVersion;
                }

                return true;
            }

            if (!CheckHarmonyVersion(replaceHarmonyVersion))
                return false;

            var harmony = new Harmony(modEntry.Info.Id);

            var patchClasses = AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly())
                .Select(t => (t, pc: harmony.CreateClassProcessor(t)))
                .Where(tuple => tuple.pc.HasPatchAttribute())
                .ToArray();

            RunPatches(patchClasses.Where(tuple => tuple.pc.GetCategory() != "Experimental"));

#if DEBUG
            Logger.Log("Running experimental patches");
            try
            {
                RunPatches(patchClasses.Where(tuple => tuple.pc.GetCategory() == "Experimental"));
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
