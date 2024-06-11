using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using UnityEngine;

using UnityModManagerNet;

namespace WrathPatches
{
    static partial class Main
    {
        static void OnGUI(UnityModManager.ModEntry _)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.Label("Patches Status");

                    var font = GUI.skin.label.font;

                    foreach (var (t, pc) in PatchClasses.Value)
                    {
                        var name = t.Name;

                         AppliedPatches.TryGetValue(t, out var applied);

                        if (t.GetCustomAttribute<WrathPatchAttribute>() is { } attr)
                            name = attr.Name;

                        if (IsExperimental(pc))
                            name = $"(Experimental) {name}";

                        GUILayout.Toggle(applied is true, name);
                    }
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                {
                    static bool IsVersionMisMatch() => HarmonyVersion < UmmHarmonyVersion;

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label($"Harmony version: {HarmonyVersion}");

                        if (IsVersionMisMatch() && ModEntry!.Active)
                        {
                            if (GUILayout.Button("Update"))
                            {
                                ModEntry!.OnToggle = (_, value) => !value;
                                ModEntry!.Active = false;
                                ModEntry!.Info.DisplayName = $"{ModEntry!.Info.DisplayName} - RESTART REQUIRED";

                                ReplaceHarmony(GetHarmonyAss().Location, UmmHarmonyPath);
                            }
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label($"UMM Harmony Version: {UmmHarmonyVersion}");
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private static UnityModManager.ModEntry? ModEntry;

        public static UnityModManager.ModEntry.ModLogger Logger => ModEntry!.Logger;

        static Harmony HarmonyInstance = null!;

        static readonly Lazy<(Type t, PatchClassProcessor pc)[]> PatchClasses = new(() =>
            AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly())
                .Select(t => (t, pc: HarmonyInstance!.CreateClassProcessor(t)))
                .Where(tuple => tuple.pc.HasPatchAttribute())
                .ToArray());

        static readonly Dictionary<Type, bool?> AppliedPatches = new();

        static bool IsExperimental(PatchClassProcessor pc) => pc.GetCategory() == "Experimental";

        static void RunPatches(IEnumerable<(Type, PatchClassProcessor)> typesAndPatches)
        {
            foreach (var (t, pc) in typesAndPatches)
            {
                try
                {
                    Logger.Log($"Running patch class {t.Name}");
                    pc.Patch();

                    AppliedPatches[t] = true;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Exception in patch class {t.Name}");
                    Logger.LogException(ex);

                    AppliedPatches[t] = false;
                }
            }
        }

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;

            ModEntry.OnGUI = OnGUI;

            ModEntry.OnToggle = (_, value) => value;

            //static bool replaceHarmonyVersion(Version harmonyVersion)
            //{
            //    var ignoreFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "IgnoreHarmonyVersion.txt");
            //    if (File.Exists(ignoreFilePath))
            //    {
            //        var text = File.ReadAllText(ignoreFilePath).Trim();

            //        return  !Version.TryParse(text, out var ignoreVersion) || ignoreVersion <= harmonyVersion;
            //    }

            //    return true;
            //}

            //if (!CheckHarmonyVersion(replaceHarmonyVersion))
            //    return false;

            HarmonyInstance = new Harmony(modEntry.Info.Id);

            RunPatches(PatchClasses.Value.Where(tuple => !IsExperimental(tuple.pc)));

#if DEBUG
            Logger.Log("Running experimental patches");
            try
            {
                RunPatches(PatchClasses.Value.Where(tuple => IsExperimental(tuple.pc)));
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
