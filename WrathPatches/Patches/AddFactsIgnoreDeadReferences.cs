using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;

using WrathPatches.TranspilerUtil;

namespace WrathPatches
{
    internal static class AddFactsIgnoreDeadReferences
    {
        //[HarmonyPatch(typeof(AddFacts), nameof(AddFacts.Facts), MethodType.Getter)]
        //static class AddFacts_Facts_Patch
        //{
        //    static ReferenceArrayProxy<BlueprintUnitFact, BlueprintUnitFactReference>
        //        Postfix(ReferenceArrayProxy<BlueprintUnitFact, BlueprintUnitFactReference> arr)
        //    {
        //        return arr.m_Array.Where(bpRef => ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints.ContainsKey(bpRef.deserializedGuid)).ToArray();
        //    }
        //}

        static readonly MethodInfo HasItemMethod =
            typeof(Kingmaker.Utility.LinqExtensions).GetMethods()
                .First(mi =>
                {
                    if (mi.Name != nameof(Kingmaker.Utility.LinqExtensions.HasItem))
                        return false;

                    var ps = mi.GetParameters();

                    return ps.Length == 2 &&
                        ps[1].ParameterType.IsGenericType &&
                        ps[1].ParameterType.GetGenericTypeDefinition() ==
                            typeof(Func<object, object>).GetGenericTypeDefinition();
                }).MakeGenericMethod(typeof(UnitFact));

        [WrathPatch("AddFacts.UpdateFacts NRE fix")]
        [HarmonyPatch(typeof(AddFacts), nameof(AddFacts.UpdateFacts))]
        static class AddFacts_UpdateFacts_Patch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                Func<CodeInstruction, bool>[] toMatch =
                [
                    ci => ci.opcode == OpCodes.Ldloc_S,  // V_6 - IEnumerator state machine object
                    ci => ci.opcode == OpCodes.Ldloca_S, // V_5 - ReferenceArrayProxy.Enumerator struct
                    ci => ci.opcode == OpCodes.Call,     // ReferenceArrayProxy.Enumerator.Current (getter) :: ReferenceArrayProxy.Enumerator -> BlueprintUnitFact
                    ci => ci.opcode == OpCodes.Stfld,    // store to state machine "local"                  :: (V_6 * BlueprintUnitFact) -> ()
                    ci => ci.opcode == OpCodes.Ldarg_0,  // this
                    ci => ci.opcode == OpCodes.Call,     // this.Data (getter)                              :: AddFacts -> AddFactsData
                    ci => ci.opcode == OpCodes.Ldfld,    // this.Data.AppliedFacts                          :: AddFactsData -> List<UnitFact>
                    ci => ci.opcode == OpCodes.Ldloc_S,  // V_6 - IEnumerator state machine object
                    ci => ci.opcode == OpCodes.Ldftn,    // lambda body pointer?
                    ci => ci.opcode == OpCodes.Newobj,   // Func<UnitFact, bool> constructor                :: (object * nativeint) -> Func<UnitFact, bool>
                    ci => ci.Calls(HasItemMethod),       // Kingmaker.Utility.LinqExtensions.HasItem        :: (IEnumerable<UnitFact> * Func<UnitFact, bool>) -> bool
                    ci => ci.opcode == OpCodes.Brtrue_S, //                                                 :: bool -> ()
                    ci => ci.opcode == OpCodes.Ldloc_1,  // TempList (List<BlueprintUnitFact)
                    ci => ci.opcode == OpCodes.Ldloc_S,  // V_6 - IEnumerator state machine object
                    ci => ci.opcode == OpCodes.Ldfld,    // load state machine local                        :: V_6 -> BlueprintUnitFact
                    ci => ci.opcode == OpCodes.Callvirt, // List.Add                                        :: (List<BlueprintUnitFact> * BlueprintUnitFact) -> ()
                    ci => ci.opcode == OpCodes.Ldloca_S, // V_5 - ReferenceArrayProxy.Enumerator struct
                    // ci => ci.opcode == OpCodes.Call,  // ReferenceArrayProxy.Enumerator.MoveNext()       :: ReferenceArrayProxy.Enumerator -> bool
                ];

                var match = instructions.FindInstructionsIndexed(toMatch).ToArray();

                if (match.Length != toMatch.Length)
                {
                    Main.Logger.Error($"Failed to match in Transpiler for {nameof(AddFacts_UpdateFacts_Patch)}");
                    return instructions;
                }

                var ifNull = generator.DefineLabel();
                match.Last().instruction.labels.Add(ifNull);

                CodeInstruction[] toInsert =
                [
                    new(match[1].instruction),
                    new(match[2].instruction),
                    new(OpCodes.Brfalse_S, ifNull)
                ];

                var iList = instructions.ToList();

                iList.InsertRange(match[0].index, toInsert);

                return iList;
            }
        }
    }
}
