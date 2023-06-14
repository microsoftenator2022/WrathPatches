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
using Kingmaker.EntitySystem.Entities;

using Owlcat.Runtime.Core.Logging;

using WrathPatches.TranspilerUtil;

namespace WrathPatches
{
    [HarmonyPatch]
    public static class EntityFactComponent_ExceptionMessages
    {
        // TODO: Make this better (share with DelegateExceptionMessage?)
        public static string ExceptionMessage(object? entityFactComponent) =>
            $"Exception occured in {entityFactComponent?.GetType()}.{nameof(EntityFactComponent.Activate)} ({entityFactComponent})";

        static IEnumerable<CodeInstruction> PatchExceptionLog(IEnumerable<CodeInstruction> instructions, CodeInstruction callExceptionMessage)
        {
            var iList = instructions.ToList();

            //var Kingmaker_PFLog_EntityFact = typeof(Kingmaker.PFLog).GetField("EntityFact", AccessTools.all);
            var Owlcat_Runtime_Core_Logging_LogChannel_Exception = typeof(LogChannel).GetMethod(
                nameof(LogChannel.Exception),
                new[] { typeof(Exception), typeof(string), typeof(object[]) });

            var matchLogChannelException = new Func<CodeInstruction, bool>[]
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
                callExceptionMessage
            };

            var matched = instructions.FindInstructionsIndexed(matchLogChannelException);

            if (matched.Count() != matchLogChannelException.Count())
            {
#if DEBUG
                Main.Logger.Log($"Could not find match");
#endif

                return instructions;
            }

            var ldnullOffset = matched.First(ci => ci.instruction.opcode == OpCodes.Ldnull).index;

            var exceptionBlocks = iList[ldnullOffset].blocks;

            iList.RemoveAt(ldnullOffset);
            //iList.InsertRange(ldnullOffset, toInsert.Select(ci => { ci.blocks = exceptionBlocks; return ci; }));
            iList.InsertRange(ldnullOffset, toInsert);

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

        static string DelegateExceptionMessage(object? componentRuntime)
        {
            if (componentRuntime?.GetType() is not { } type) return null!;

            try
            {
//#if DEBUG
//                Main.Logger.Log($"{type} Properties:");

//                foreach (var p in type.GetProperties(AccessTools.all))
//                {
//                    Main.Logger.Log($"{p.PropertyType} {p.DeclaringType}.{p.Name}");
//                }
//#endif


                //var maybeOwner = type.GetProperty("Owner", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)?.GetValue(componentRuntime);
                var maybeOwner = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                    ?.FirstOrDefault(pi => pi.Name == "Owner" && pi.PropertyType == typeof(UnitEntityData))
                    ?.GetValue(componentRuntime);

                var maybeFact = type.GetProperty("Fact", AccessTools.all)?.GetValue(componentRuntime);

                var blueprint = (maybeFact as EntityFact)?.Blueprint;

                var sb = new StringBuilder();

                sb.AppendLine($"Exception occured in {type}.{nameof(EntityFactComponentDelegate.ComponentRuntime.OnActivate)} ({componentRuntime})");

                sb.Append("  Blueprint: ");
                if (blueprint is { })
                {
                    sb.Append($"{blueprint.AssetGuid}");
                    if (blueprint.name is not null)
                        sb.Append($" ({blueprint.name})");
                }
                else
                    sb.Append("<null>");
                sb.AppendLine();

                sb.Append("  Owner: ");
                if (maybeOwner is UnitEntityData owner)
                    sb.Append($"{owner?.CharacterName}");
                else
                    sb.Append("<null>");
                sb.AppendLine();

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Main.Logger.Error("Exception occured generating exception message");
                Main.Logger.LogException(ex);

                return null!;
            }
        }

        static void ComponentRuntime_Delegate_OnActivate(object instance)
        {
            //Main.Logger.Log(nameof(ComponentRuntime_Delegate_OnActivate));

            var t = instance.GetType();

            var delegateProperty = t?.GetProperty("Delegate", AccessTools.all);
            var delegateType = delegateProperty?.PropertyType;
            var delegateOnActivate = delegateType?.GetMethod("OnActivate", AccessTools.all);

            var @delegate = delegateProperty?.GetValue(instance);

            //if (@delegate is null)
            //{
            //    var sb = new StringBuilder();
            //    sb.AppendLine($"Could not get delegate");

            //    sb.AppendLine($"Type: {t}");
            //    sb.AppendLine($"Property Type: {delegateProperty}");
            //    sb.AppendLine($"Property: {@delegate?.ToString() ?? "<null>"}");
            //    sb.AppendLine($"Delegate.OnActivate Method: {delegateOnActivate}");

            //    Main.Logger.Error(sb.ToString());
            //}

            delegateOnActivate!.Invoke(@delegate, null);
        }

        [HarmonyPatch(typeof(EntityFactComponentDelegate.ComponentRuntime),
            nameof(EntityFactComponentDelegate.ComponentRuntime.OnActivate))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> EntityFactComponentDelegate_ComponentRuntime_OnActivate_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
#if DEBUG
            Main.Logger.Log($"{nameof(EntityFactComponentDelegate_ComponentRuntime_OnActivate_Transpiler)}");
#endif
            var matchDelegateOnActivate = new Func<CodeInstruction, bool>[]
            {
                ci => ci.opcode == OpCodes.Ldarg_0,
                ci => ci.opcode == OpCodes.Call, // instance method of generic type, throws InvalidCastException
                ci => ci.opcode == OpCodes.Callvirt
            };

            var matched = instructions.FindInstructionsIndexed(matchDelegateOnActivate).Select(i => i.instruction).ToArray();

            if (!matched.Any()) return instructions;

            matched[1].operand = CodeInstruction.Call((object instance) => ComponentRuntime_Delegate_OnActivate(instance)).operand;
            matched[2].opcode = OpCodes.Nop;
            matched[2].operand = null;

            return PatchExceptionLog(instructions,
                CodeInstruction.Call<object, string>(obj => DelegateExceptionMessage(obj)));
        }
    }
}
