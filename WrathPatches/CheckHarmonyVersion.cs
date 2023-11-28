using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityModManagerNet;

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

        //static bool CheckHarmonyVersion(Func<Version, bool> shouldReplace)
        //{
        //    //var harmonyAss = HarmonyAss;
        //    var harmonyPath = GetHarmonyAss().Location;

        //    var harmonyVersion = HarmonyVersion;

        //    Logger.Log($"Harmony version: {harmonyVersion}");

        //    //var ummAss = GetUmmAss();

        //    var ummHarmonyPath = UmmHarmonyPath;

        //    var ummHarmonyVersion = UmmHarmonyVersion;

        //    Logger.Log($"UMM Harmony version: {ummHarmonyVersion}");

        //    if (harmonyVersion < ummHarmonyVersion)
        //    {
        //        Logger.Warning($"Harmony version {harmonyVersion} is lower than UMM Harmony version {ummHarmonyVersion}");

        //        if (shouldReplace(harmonyVersion))
        //        {
        //            //Logger.Warning($"Copying {ummHarmonyPath} -> {harmonyPath}. Please restart the game.");

        //            //File.Copy(harmonyPath, Path.Combine(Path.GetDirectoryName(harmonyPath), $"0Harmony.{harmonyVersion}.old"));
        //            //File.Copy(ummHarmonyPath, harmonyPath, true);

        //            ReplaceHarmony(harmonyPath, ummHarmonyPath);

        //            typeof(UnityModManager.UI).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[0]);

        //            UnityModManager.UI.Instance.ToggleWindow(true);

        //            ModEntry!.Info.DisplayName = $"{ModEntry!.Info.DisplayName} - RESTART REQUIRED";

        //            return false;
        //        }
        //    }

        //    return true;
        //}
    }
}
