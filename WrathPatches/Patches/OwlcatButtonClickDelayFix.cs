using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Owlcat.Runtime.UI.Controls.Button;

using WrathPatches.TranspilerUtil;

namespace WrathPatches.Patches;

[WrathPatch("Reduce button click delay from 300ms to 1 frame")]
[HarmonyPatch]
internal static class OwlcatButtonClickDelayFix
{
    internal static IEnumerable<MethodInfo> TargetMethods() => new Type[]
    {
        typeof(OwlcatButton),
        typeof(OwlcatMultiButton)
    }.SelectMany(t => t
        .GetNestedTypes(AccessTools.all)
        .Where(t => t.Name.Contains("LeftClickTime")))
    .Select(t => AccessTools.Method(t, "MoveNext"));

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var match = instructions.FindInstructionsIndexed(
        [
            ci => ci.opcode == OpCodes.Ldc_R4 && (float)ci.operand == 0.3f,
            ci => ci.opcode == OpCodes.Blt_S
        ]).ToArray();

        if (match.Length != 2)
        {
            Main.Logger.Log(string.Concat(instructions.Select(i => i.ToString() + "\n")));

            throw new Exception("Could not find patch target");
        }

        var iList = instructions.ToList();

        iList[match[0].index].operand = 0.0f;
        iList[match[1].index].opcode = OpCodes.Bgt_S;

        return iList;
    }
}
