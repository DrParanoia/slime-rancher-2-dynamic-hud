using HarmonyLib;

namespace DynamicHud.Patches;

[HarmonyPatch(typeof(Il2Cpp.PlayerState), nameof(Il2Cpp.PlayerState.AddCurrency))]
public static class CurrencyAddPatch
{
    public static void Postfix()
    {
        HudController.Currency.Flash();
    }
}

[HarmonyPatch(typeof(Il2Cpp.PlayerState), nameof(Il2Cpp.PlayerState.SpendCurrency))]
public static class CurrencySpendPatch
{
    public static void Postfix()
    {
        HudController.Currency.Flash();
    }
}
