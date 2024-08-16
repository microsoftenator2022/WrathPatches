using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker;
using Kingmaker.Cheats;

using UnityEngine;

namespace WrathPatches.Patches;

[WrathPatch("OpenGameLogFull opens Player.log too")]
[HarmonyPatch(typeof(CheatsDebug), nameof(CheatsDebug.OpenGameLogFull), [typeof(string)])]
internal static class LogsHotkey
{
    public static void EnableHotKey()
    {

        Main.Logger.Log("Enabling GameLogFull keybind");

        Game.Instance.Keyboard.Bind("OpenGameLogFull", CheatsDebug.OpenGameLogFull);
    }

    [HarmonyPostfix]
    [HarmonyPatch]
    static void OpenGameLogFull_Postfix()
    {
        try
        {
            Application.OpenURL(Path.Combine(Application.persistentDataPath, "Player.log"));
        }
        catch
        {
        }
    }
}
