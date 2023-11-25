using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using UnityModManagerNet;

namespace WrathPatches
{
    static partial class Main
    {
        static bool CheckHarmonyVersion(Func<Version, bool> shouldReplace)
        {
            var harmonyAss = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(ass => ass.GetName().Name == "0Harmony");
            var harmonyPath = harmonyAss.Location;

            var harmonyVersion = harmonyAss.GetName().Version;
            Logger.Log($"Harmony version: {harmonyVersion}");

            var ummAss = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(ass => ass.GetName().Name == "UnityModManager");

            var ummHarmonyPath = Path.Combine(Path.GetDirectoryName(ummAss.Location), "0Harmony.dll");

            var ummHarmonyVersion = Version.Parse(FileVersionInfo.GetVersionInfo(ummHarmonyPath).FileVersion);
            Logger.Log($"UMM Harmony version: {ummHarmonyVersion}");

            if (harmonyVersion < ummHarmonyVersion)
            {
                Logger.Warning($"Harmony version {harmonyVersion} is lower than UMM Harmony version {ummHarmonyVersion}");

                if (shouldReplace(harmonyVersion))
                {
                    Logger.Warning($"Copying {ummHarmonyPath} -> {harmonyPath}. Please restart the game.");

                    File.Copy(harmonyPath, Path.Combine(Path.GetDirectoryName(harmonyPath), $"0Harmony.{harmonyVersion}.old"));
                    File.Copy(ummHarmonyPath, harmonyPath, true);

                    typeof(UnityModManager.UI).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[0]);

                    UnityModManager.UI.Instance.ToggleWindow(true);

                    ModEntry!.Info.DisplayName = $"{ModEntry!.Info.DisplayName} - RESTART REQUIRED";

                    return false;
                }
            }

            return true;
        }
    }
}
