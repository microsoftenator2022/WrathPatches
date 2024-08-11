using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.AreaLogic.Capital;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Armies.Components;
using Kingmaker.Armies.TacticalCombat.LeaderSkills;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Experience;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Craft;
using Kingmaker.Designers.EventConditionActionSystem.Events;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Dungeon.FactLogic;
using Kingmaker.Kingdom.Settlements.BuildingComponents;
using Kingmaker.Tutorial;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.Utility;

namespace WrathPatches.Patches;

[WrathPatch("Non-matching BlueprintComponent.OwnerBlueprint warning")]
[HarmonyPatch]
public class OwnerBlueprintWarning
{
    /// <summary>
    /// List of BlueprintComponent types that use OwnerBlueprint
    /// </summary>
    public static readonly IEnumerable<Type> ErrorComponentTypes =
    [
        typeof(CapitalCompanionLogic),
        typeof(EtudeBracketMusic),
        typeof(EtudeBracketSetCompanionPosition),
        typeof(ArmyUnitComponent),
        typeof(LeaderPercentAttributeBonus),
        typeof(MaxArmySquadsBonusLeaderComponent),
        typeof(SquadsActionOnTacticalCombatStart),
        typeof(Experience),
        typeof(PrerequisiteArchetypeLevel),
        typeof(PrerequisiteClassLevel),
        typeof(PrerequisiteFeature),
        typeof(CraftInfoComponent),
        typeof(EvaluatedUnitCombatTrigger),
        typeof(ControlledProjectileHolder),
        typeof(DungeonAddLootToVendor),
        typeof(BuildingUpgradeBonus),
        typeof(TutorialPage),
        typeof(AbilityCustomDimensionDoor),
        typeof(AbilityDeliverProjectileOnGrid),
        typeof(AbilityIsBomb),
        typeof(AbilityDeliverEffect),
        typeof(MarkUsableWhileCan),
        typeof(ActivatableAbilitySet),
        typeof(ActivatableAbilitySetItem),
        typeof(AddAbilityUseTrigger),
        typeof(AddFeaturesFromSelectionToDescription),
        typeof(AddTriggerOnActivationChanged),
        typeof(AddVendorItems),
        typeof(NenioSpecialPolymorphWhileEtudePlaying),
        typeof(ChangeSpellElementalDamage),
        typeof(ContextCalculateAbilityParams),
        typeof(ContextRankConfig),
        typeof(ContextSetAbilityParams),
        typeof(UnitPropertyComponent)
    ];

#if DEBUG
    public static IEnumerable<Type> GenericComponentTypes =
    [
        typeof(EtudeBracketTrigger<object>)
    ];
#endif

    static readonly HashSet<(BlueprintGuid, string)> AlreadyWarned = [];

    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Load))]
    [HarmonyPostfix]
    static void Postfix(SimpleBlueprint __result, BlueprintGuid guid)
    {
        if (__result is not BlueprintScriptableObject blueprint)
            return;

        foreach (var c in blueprint.ComponentsArray)
        {
            if (c.OwnerBlueprint != blueprint)
            {
#if !DEBUG
                if (AlreadyWarned.Contains((blueprint.AssetGuid, c.name)))
                    continue;
#endif

                if (ErrorComponentTypes.Any(t => t.IsAssignableFrom(c.GetType()))
#if DEBUG
                    || GenericComponentTypes.Any(t => c.GetType().GetAllBaseTypes(true).Where(t => t.IsGenericType).Select(t => t.GetGenericTypeDefinition()).Contains(t))
#endif
                    )
                {
                    Main.Logger.Error($"In blueprint {guid} \"{blueprint.name}\": " +
                        $"Non-matching OwnerBlueprint {c.OwnerBlueprint?.ToString() ?? "NULL"} on {c.GetType()} \"{c.name}\". " +
                        $"THIS COMPONENT MAY NOT WORK AS EXPECTED!");
                }
#if DEBUG
                else
                {
                    Main.Logger.Warning($"In blueprint {guid} \"{blueprint.name}\": " +
                        $"Non-matching OwnerBlueprint {c.OwnerBlueprint?.ToString() ?? "NULL"} on {c.GetType()} \"{c.name}\"");
                }
#endif
            }
        }
    }
}
