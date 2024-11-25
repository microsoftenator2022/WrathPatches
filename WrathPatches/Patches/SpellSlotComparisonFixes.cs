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
using Kingmaker.Utility;

namespace WrathPatches.Patches;

[WrathPatch("Fix Spell Slot comparison.")]
[HarmonyPatch]
internal class SpellSlotComparisonFixes
{
    static int MetaComparator(AbilityData a1, AbilityData a2)
    {
        #if DEBUG
        Main.Logger.Log($"Compare {a1.Name} to {a2.Name}");
        Main.Logger.Log($"{a1} metamagic mask: {a1.MetamagicData?.MetamagicMask}");
        Main.Logger.Log($"{a2} metamagic mask: {a2.MetamagicData?.MetamagicMask}");
        #endif
        
        var result = (a2.MetamagicData != null ? (int)a2.MetamagicData.MetamagicMask : 0) - (a1.MetamagicData != null ? (int)a1.MetamagicData.MetamagicMask : 0);
        
        #if DEBUG
        Main.Logger.Log($"Result: {result}");
        #endif

        return result;
    }

    static int CompareSpellbooks(Spellbook sb1, Spellbook sb2)
    {
        #if DEBUG
        Main.Logger.Log($"Compare spellbooks: sb1 = {sb1} sb2 = {sb2}");
        #endif

        var sb2Index = sb2.Owner.Spellbooks.IndexOf(sb2);
        var sb1Index = sb1.Owner.Spellbooks.IndexOf(sb1);

        var result = sb2Index - sb1Index;

        #if DEBUG
        Main.Logger.Log($"Result: {sb2Index} - {sb1Index} = {result}");
        #endif

        return result;
    }

    static int Compare(AbilityData a1, AbilityData a2)
    {
        Main.Logger.Log($"{nameof(SpellSlotComparisonFixes)}.{nameof(Compare)}");

        if (a1.Spellbook is { } sb1 && a2.Spellbook is { } sb2)
        {
            var compareSpellbooks = CompareSpellbooks(sb1, sb2);

            if (compareSpellbooks != 0) return compareSpellbooks;
        }

        return MetaComparator(a1, a2);
    }

    [HarmonyPatch(typeof(ActionBarSpellbookHelper), nameof(ActionBarSpellbookHelper.Comparator))]
    [HarmonyPostfix]
    private static int Comparator_Postfix(int result, MechanicActionBarSlotSpell s1, MechanicActionBarSlotSpell s2)
    {
        if (result != 0) return result;

        var compare = Compare(s1.Spell, s2.Spell);

        #if DEBUG
        Main.Logger.Log($"{nameof(Comparator_Postfix)}: {compare}");
        #endif

        return compare;
    }

    [HarmonyPatch(typeof(ActionBarSpellbookHelper), nameof(ActionBarSpellbookHelper.IsEquals), [typeof(SpellSlot), typeof(SpellSlot)])]
    [HarmonyPostfix]
    static bool ActionBarSpellbookHelper_IsEquals_SpellSlot_Postfix(bool result, SpellSlot s1, SpellSlot s2)
    {
        var compare = Compare(s1.Spell, s2.Spell);

        #if DEBUG
        Main.Logger.Log($"{nameof(ActionBarSpellbookHelper_IsEquals_SpellSlot_Postfix)}: {compare}");
        #endif

        return result && compare == 0;
    }

    [HarmonyPatch(typeof(ActionBarSpellbookHelper), nameof(ActionBarSpellbookHelper.IsEquals), [typeof(AbilityData), typeof(AbilityData)])]
    [HarmonyPostfix]
    static bool ActionBarSpellbookHelper_IsEquals_AbilityData_Postfix(bool result, AbilityData a1, AbilityData a2)
    {
        var compare = Compare(a1, a2);

#if DEBUG
        Main.Logger.Log($"{nameof(ActionBarSpellbookHelper_IsEquals_AbilityData_Postfix)}: {compare}");
#endif

        return result && compare == 0;
    }

    [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.GetAvailableForCastSpellCount))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> ActionBarSpellbookHelper_TryAddSpell_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var applied = false;

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
                yield return CodeInstruction.Call((AbilityData a1, AbilityData a2) => Compare(a1, a2));
                yield return new(OpCodes.Brtrue_S, targetLabel);

                applied = true;
            }
        }

        if (!applied)
            throw new Exception("Failed to find target instruction");
    }
}
