using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.UI;
using Kingmaker.UI.MVVM._PCView.Settings;

namespace WrathPatches.Patches
{
    [WrathPatch("Don't try to play missing settings UI sound types")]
    [HarmonyPatch]
    class MissingSettingsUISoundsFix
    {
        [HarmonyTargetMethods]
        static IEnumerable<MethodInfo> TargetMethods() =>
        [
            AccessTools.Method(typeof(SettingsPCView), nameof(SettingsPCView.Show)),
            AccessTools.Method(typeof(SettingsPCView), nameof(SettingsPCView.Hide))
        ];

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var iList = instructions.ToArray();

            for (var i = 0; i < iList.Length; i++)
            {
                if (iList[i].Calls(AccessTools.Method(typeof(UISoundController), nameof(UISoundController.Play), [typeof(UISoundType)])))
                {
#if DEBUG
                    var value = (UISoundType)(sbyte)(iList[i - 1].operand);
                    Main.Logger.Log($"{value} -> {UISoundType.None}");
#endif
                    iList[i - 1].operand = (int)UISoundType.None;
                }
            }

            return iList;
        }
    }
}
