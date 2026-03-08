using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI;

namespace DynamicHud.Patches;

[HarmonyPatch(typeof(Il2Cpp.PlayerState), nameof(Il2Cpp.PlayerState.SpendEnergy))]
public static class EnergySpendPatch
{
    public static void Postfix()
    {
        HudController.EnergyBar.Flash();
    }
}

[HarmonyPatch(typeof(EnergyMeter), nameof(EnergyMeter.OnCurrentEnergyChanged))]
public static class EnergyMeterChangedPatch
{
    public static void Postfix()
    {
        HudController.EnergyBar.Flash();
    }
}
