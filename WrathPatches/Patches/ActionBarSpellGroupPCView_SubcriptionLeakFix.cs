using System.Collections.Generic;

using HarmonyLib;

using Kingmaker.UI._ConsoleUI.TurnBasedMode;
using Kingmaker.UI.MVVM._PCView.ActionBar;
using Kingmaker.UI.MVVM._VM.ActionBar;

namespace WrathPatches.Patches;

[WrathPatch("Event subscription leak fixes")]
[HarmonyPatch]
static partial class EventSubscriptionLeakFixes
{
    [HarmonyPatch(typeof(ActionBarSpellGroupPCView), nameof(ActionBarSpellGroupPCView.AddEmptySlots))]
    [HarmonyPostfix]
    static void PatchAddEmptySlots(ActionBarSpellGroupPCView __instance, List<ActionBarSlotVM> slotVms) =>
        slotVms.ForEach(__instance.AddDisposable);

    [HarmonyPatch(typeof(InitiativeTrackerUnitVM), nameof(InitiativeTrackerUnitVM.UpdateBuffs))]
    [HarmonyPrefix]
    static void PatchUpdateBuffs(InitiativeTrackerUnitVM __instance) =>
        __instance.UnitBuffs.ForEach(x => x.Dispose());
}