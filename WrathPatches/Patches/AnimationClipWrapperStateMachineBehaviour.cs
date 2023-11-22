using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Visual.Animation.Events;

namespace WrathPatches
{
    [HarmonyPatch(typeof(AnimationClipWrapperStateMachineBehaviour))]
    internal static class AnimationClipWrapperStateMachineBehaviour_Patch
    {
        [HarmonyPatch(nameof(AnimationClipWrapperStateMachineBehaviour.OnStateEnter))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> OnStateEnter_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matched = instructions.FindSequence(new Func<CodeInstruction, bool>[]
            {
                ci =>
                    ci.opcode == OpCodes.Ldflda &&
                    ((FieldInfo)ci.operand) == AccessTools.Field(typeof(AnimationClipWrapperStateMachineBehaviour),
                    nameof(AnimationClipWrapperStateMachineBehaviour.AnimationParameterIdleOffsetIsMissing)),
                ci =>
                    ci.opcode == OpCodes.Call &&
                    ((MethodInfo)ci.operand) == (AccessTools.PropertyGetter(typeof(bool?), nameof(Nullable<bool>.Value))),
                ci => ci.opcode == OpCodes.Brfalse_S
            }).ToArray();

            if (matched.Length == 3)
            {
                matched[1].opcode = OpCodes.Pop;
                matched[1].operand = null;

                matched[2].opcode = OpCodes.Br;
            }
            else
            {
                Main.Logger.Error($"{nameof(AnimationClipWrapperStateMachineBehaviour_Patch)}.{nameof(OnStateEnter_Transpiler)} failed to find instructions to patch");
            }

            return instructions;
        }
    }
}
