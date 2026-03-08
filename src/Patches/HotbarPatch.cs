using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player;
using Il2CppMonomiPark.SlimeRancher.UI;

namespace DynamicHud.Patches;

/// <summary>Slot switching - flash all slots.</summary>
[HarmonyPatch(typeof(AmmoSlotManager), nameof(AmmoSlotManager.SetSelectedSlot))]
public static class HotbarSwitchPatch
{
    public static void Postfix()
    {
        HudController.FlashAllSlots();
    }
}

/// <summary>
/// Any slot content change (pickup, shoot, clear) - flash that specific slot.
/// This covers all cases: vacuuming items in, shooting items out, slot clearing.
/// </summary>
[HarmonyPatch(typeof(AmmoSlotViewHolder), nameof(AmmoSlotViewHolder.UpdateAmmoDisplay))]
public static class AmmoSlotDisplayChangedPatch
{
    public static void Postfix(AmmoSlotViewHolder __instance)
    {
        HudController.FlashSlotByViewHolder(__instance);
    }
}
