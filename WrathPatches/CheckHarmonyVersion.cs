using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace WrathPatches
{
    static partial class Main
    {
        static Assembly GetHarmonyAss() => AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(ass => ass.GetName().Name == "0Harmony");

        static Version HarmonyVersion => GetHarmonyAss().GetName().Version;

        static Assembly GetUmmAss() => AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(ass => ass.GetName().Name == "UnityModManager");

        static string UmmHarmonyPath => Path.Combine(Path.GetDirectoryName(GetUmmAss().Location), "0Harmony.dll");

        static Version UmmHarmonyVersion => Version.Parse(FileVersionInfo.GetVersionInfo(UmmHarmonyPath).FileVersion);

        static void ReplaceHarmony(string harmonyPath, string ummHarmonyPath)
        {
            Logger.Warning($"Copying {ummHarmonyPath} -> {harmonyPath}. Please restart the game.");
            File.Copy(harmonyPath, Path.Combine(Path.GetDirectoryName(harmonyPath), $"0Harmony.{HarmonyVersion}.old"));
            File.Copy(ummHarmonyPath, harmonyPath, true);
        }
    }
}
