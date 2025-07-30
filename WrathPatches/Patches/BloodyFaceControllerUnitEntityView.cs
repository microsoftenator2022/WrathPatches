using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.View;
using Kingmaker.Visual.MaterialEffects;

using Owlcat.Runtime.Core;

namespace WrathPatches.Patches
{
    [WrathPatch("No BloodyFaceController for non-UnitEntityView")]
    [HarmonyPatch(typeof(StandardMaterialController), nameof(StandardMaterialController.Awake))]
    internal class BloodyFaceControllerUnitEntityView
    {
        static bool HasUnitEntityView(StandardMaterialController smc) => smc.gameObject.GetComponent<UnitEntityView>() is not null;

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var Application_isPlaying =
                AccessTools.PropertyGetter(typeof(UnityEngine.Application), nameof(UnityEngine.Application.isPlaying));

            var index = instructions.FindIndex(ci => ci.Calls(Application_isPlaying));

            if (index < 0)
                throw new Exception("Could not find target instruction");

            var iList = instructions.ToList();

            var ifFalse = iList[index + 1];

            iList.InsertRange(index - 2,
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call((StandardMaterialController x) => HasUnitEntityView(x)),
                ifFalse
            ]);

            return iList;
        }
    }
}
