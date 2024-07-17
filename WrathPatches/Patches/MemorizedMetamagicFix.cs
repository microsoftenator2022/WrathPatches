using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.UI.MVVM._VM.ActionBar;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;

namespace WrathPatches.Patches;

[WrathPatch("Fix Memorized Caster Metamagic")]
[HarmonyPatch]
internal class MemorizedMetamagicFix
{
    static int MetaComparator(AbilityData a1, AbilityData a2)
    {
        //Main.Logger.Log($"Compare {a1.Name} to {a2.Name}");
        //Main.Logger.Log($"{a1} metamagic mask: {a1.MetamagicData?.MetamagicMask}");
        //Main.Logger.Log($"{a2} metamagic mask: {a2.MetamagicData?.MetamagicMask}");

        return (a2.MetamagicData != null ? (int)a2.MetamagicData.MetamagicMask : 0) - (a1.MetamagicData != null ? (int)a1.MetamagicData.MetamagicMask : 0);
    }

    [HarmonyPatch(typeof(ActionBarSpellbookHelper), nameof(ActionBarSpellbookHelper.Comparator))]
    [HarmonyPostfix]
    private static int Comparator_Postfix(int result, MechanicActionBarSlotSpell s1, MechanicActionBarSlotSpell s2)
    {
        if (result != 0) return result;

        return MetaComparator(s1.Spell, s2.Spell);
    }

    [HarmonyPatch(typeof(ActionBarSpellbookHelper), nameof(ActionBarSpellbookHelper.IsEquals), [typeof(SpellSlot), typeof(SpellSlot)])]
    [HarmonyPostfix]
    static bool ActionBarSpellbookHelper_IsEquals_Postfix(bool result, SpellSlot s1, SpellSlot s2) =>
        result && MetaComparator(s1.Spell, s2.Spell) == 0;

    [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.GetAvailableForCastSpellCount))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> ActionBarSpellbookHelper_TryAddSpell_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var i in instructions)
        {
            yield return i;

            if (i.opcode == OpCodes.Bne_Un_S)
            {
                var targetLabel = i.operand;

                Main.Logger.Log(nameof(ActionBarSpellbookHelper_TryAddSpell_Transpiler));

                yield return new(OpCodes.Ldloc_S, 4);
                yield return new(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(SpellSlot), nameof(SpellSlot.SpellShell)));
                yield return new(OpCodes.Ldarg_1);
                yield return CodeInstruction.Call((AbilityData a1, AbilityData a2) => MetaComparator(a1, a2));
                yield return new(OpCodes.Brfalse_S, targetLabel);
            }
        }
    }
}
