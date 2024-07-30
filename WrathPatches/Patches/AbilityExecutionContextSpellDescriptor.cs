using HarmonyLib;

using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;

namespace WrathPatches.Patches
{
    [WrathPatch("Add AbilityData spell descriptor to AbilityExecutionContext")]
    [HarmonyPatch(typeof(AbilityExecutionContext))]
    static class AbilityExecutionContextSpellDescriptor
    {
        [HarmonyPatch(MethodType.Constructor,
        [
            typeof(AbilityData),
            typeof(AbilityParams),
            typeof(TargetWrapper),
            typeof(RulebookEventContext),
            typeof(UnitEntityData)
        ])]
        [HarmonyPostfix]
        static void Postfix(AbilityExecutionContext __instance) =>
            __instance.SpellDescriptor |= __instance.Ability.SpellDescriptor;
    }
}
