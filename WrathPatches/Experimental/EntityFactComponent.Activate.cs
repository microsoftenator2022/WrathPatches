using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.EntitySystem;

using Owlcat.Runtime.Core.Logging;

using WrathPatches.TranspilerUtil;

namespace WrathPatches
{
    [HarmonyPatch]
    public static class EntityFactComponent_ExceptionMessages
    {
        public static string ExceptionMessage(object? entityFactComponent) =>
            $"Exception occured in {entityFactComponent?.GetType()}.{nameof(EntityFactComponent.Activate)} ({entityFactComponent})";

        static IEnumerable<CodeInstruction> PatchExceptionLog(IEnumerable<CodeInstruction> instructions, CodeInstruction exceptionMessage)
        {
            var iList = instructions.ToList();

            //var Kingmaker_PFLog_EntityFact = typeof(Kingmaker.PFLog).GetField("EntityFact", AccessTools.all);
            var Owlcat_Runtime_Core_Logging_LogChannel_Exception = typeof(LogChannel).GetMethod(
                nameof(LogChannel.Exception),
                new[] { typeof(Exception), typeof(string), typeof(object[]) });

            var iMatch = new Func<CodeInstruction, bool>[]
            {
                ci => ci.IsStloc(),
                ci => ci.opcode == OpCodes.Ldsfld && ci.operand is FieldInfo fi && fi.FieldType == typeof(LogChannel),
                ci => ci.IsLdloc(),
                ci => ci.opcode == OpCodes.Ldnull,
                ci => ci.opcode == OpCodes.Call && ci.OperandIs(CodeInstruction.Call(() => Array.Empty<object>()).operand),
                ci => ci.Calls(Owlcat_Runtime_Core_Logging_LogChannel_Exception)
            };

            var toInsert = new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                exceptionMessage
            };

            var x = instructions.FindInstructionsIndexed(iMatch);

            //Main.Logger.Log("BEFORE");

            //foreach (var i in x)
            //{
            //    Main.Logger.Log($"{i.index}: {i.instruction}");
            //}

            if (x.Count() != iMatch.Count())
            {
#if DEBUG
                Main.Logger.Log($"Could not find match");
#endif

                return instructions;
            }

            var ldnullOffset = x.First(ci => ci.instruction.opcode == OpCodes.Ldnull).index;

            var exceptionBlocks = iList[ldnullOffset].blocks;

            iList.RemoveAt(ldnullOffset);
            //iList.InsertRange(ldnullOffset, toInsert.Select(ci => { ci.blocks = exceptionBlocks; return ci; }));
            iList.InsertRange(ldnullOffset, toInsert);

            //Main.Logger.Log("AFTER");

            //foreach (var i in iList.Indexed().Skip(x.First().index))
            //{
            //    Main.Logger.Log($"{i.index}: {i.item}");
            //}

            return iList;
        }

        [HarmonyPatch(typeof(EntityFactComponent), nameof(EntityFactComponent.Activate))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> EntityFactComponent_Activate_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
#if DEBUG
            Main.Logger.Log($"{nameof(EntityFactComponent_Activate_Transpiler)}");
#endif

            return PatchExceptionLog(instructions, CodeInstruction.Call<EntityFactComponent, string>(obj => ExceptionMessage(obj)));
        }

        static string ExceptionMessageDelegate(object? componentRuntime) =>
            $"Exception occured in {componentRuntime?.GetType()}.{nameof(EntityFactComponentDelegate.ComponentRuntime.OnActivate)} ({componentRuntime})";

        //[HarmonyPatch(typeof(EntityFactComponentDelegate.ComponentRuntime),
        //    nameof(EntityFactComponentDelegate.ComponentRuntime.OnActivate))]
        //[HarmonyTranspiler]
        static IEnumerable<CodeInstruction> EntityFactComponentDelegate_ComponentRuntime_OnActivate_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
#if DEBUG
            Main.Logger.Log($"{nameof(EntityFactComponentDelegate_ComponentRuntime_OnActivate_Transpiler)}");
#endif

            return PatchExceptionLog(instructions,
                CodeInstruction.Call<object, string>(obj => ExceptionMessageDelegate(obj)));
        }
    }
}
