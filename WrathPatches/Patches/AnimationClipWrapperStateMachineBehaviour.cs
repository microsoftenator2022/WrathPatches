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
    [WrathPatch("Silence missing animator state errors")]
    [HarmonyPatch(typeof(AnimationClipWrapperStateMachineBehaviour))]
    internal static class AnimationClipWrapperStateMachineBehaviour_Patch
    {
        [HarmonyPatch(nameof(AnimationClipWrapperStateMachineBehaviour.OnStateEnter))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> OnStateEnter_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var AnimationClipWrapperStateMachineBehaviour_AnimationParameterIdleOffsetIsMissing =
                AccessTools.Field(typeof(AnimationClipWrapperStateMachineBehaviour),
                    nameof(AnimationClipWrapperStateMachineBehaviour.AnimationParameterIdleOffsetIsMissing));

            var Nullable_bool_Value = AccessTools.PropertyGetter(typeof(bool?), nameof(Nullable<bool>.Value));

            var matched = instructions.FindSequence(
            [
                ci => ci.opcode == OpCodes.Ldflda &&
                    ((FieldInfo)ci.operand) == AnimationClipWrapperStateMachineBehaviour_AnimationParameterIdleOffsetIsMissing,
                ci =>
                    ci.opcode == OpCodes.Call && ((MethodInfo)ci.operand) == Nullable_bool_Value,
                ci => ci.opcode == OpCodes.Brfalse_S
            ]).ToArray();

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
