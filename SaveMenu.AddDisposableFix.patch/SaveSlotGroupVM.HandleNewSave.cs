using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.UI.MVVM._VM.SaveLoad;

namespace WrathPatches
{
    [HarmonyPatch(typeof(SaveSlotGroupVM), nameof(SaveSlotGroupVM.HandleNewSave))]
    internal static class SaveSlotGroupVM_HandleNewSave
    {
        static void Prefix(SaveSlotGroupVM __instance, SaveSlotVM slot)
        {
            if (slot.Owner == null) return;

            slot.Owner.RemoveDisposable(slot);
        }
    }
}
