using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.PubSubSystem;
using Kingmaker.UI.MVVM._VM.Slots;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;

namespace WrathPatches.Patches
{
    #if false
    //[HarmonyPatchCategory("Experimental")]
    [WrathPatch("Add null check to WeaponSetGripVM.HandleUnitChangedGripAutoMode")]
    [HarmonyPatch]
    internal static class WeaponSetGripVMFix
    {
        [HarmonyTargetMethod]
        static MethodInfo TargetMethod()
        {
            var interfaceMap = typeof(WeaponSetGripVM).GetInterfaceMap(typeof(IUnityChangedGripAutoModeHandler));

            if (interfaceMap.InterfaceMethods
                .Zip(interfaceMap.TargetMethods)
                .TryFind((pair) =>
                    pair.Item1.Name == nameof(IUnityChangedGripAutoModeHandler.HandleUnitChangedGripAutoMode),
                    out var pair))
            {
                return pair.Item2;
            }

            throw new Exception("Missing interface method");
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            var index = instructions.FindIndex(ci =>
                ci.LoadsField(AccessTools.Field(typeof(WeaponSetGripVM), nameof(WeaponSetGripVM.m_HandsEquipmentSet))));

            if (index < 0)
            {
                throw new Exception("Could not find target instruction");
            }

            var iList = instructions.ToList();

            var label = ilGen.DefineLabel();

            iList.InsertRange(index + 1,
            [
                new(OpCodes.Dup),
                new(OpCodes.Brtrue_S, label),
                new(OpCodes.Pop),
                new(OpCodes.Pop),
                new(OpCodes.Ret),
                new(OpCodes.Nop) { labels = [label] }
            ]);

            return iList;
        }
    }
    #endif
}
