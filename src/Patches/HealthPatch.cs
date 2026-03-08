using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI;

namespace DynamicHud.Patches;

[HarmonyPatch(typeof(Il2Cpp.PlayerState), nameof(Il2Cpp.PlayerState.Damage))]
public static class HealthDamagePatch
{
    public static void Postfix()
    {
        HudController.HealthBar.Flash();
    }
}

[HarmonyPatch(typeof(Il2Cpp.PlayerState), nameof(Il2Cpp.PlayerState.Heal))]
public static class HealthHealPatch
{
    public static void Postfix()
    {
        HudController.HealthBar.Flash();
    }
}

// HealthMeter.Update checks oldCurrentHealth vs current each frame -
// flash when health actually changes
[HarmonyPatch(typeof(HealthMeter), nameof(HealthMeter.Update))]
public static class HealthMeterUpdatePatch
{
    private static float _lastHealth = -1f;

    public static void Postfix(HealthMeter __instance)
    {
        if (__instance.statusBar == null) return;
        float cur = __instance.statusBar.currValue;
        if (_lastHealth >= 0f && System.Math.Abs(cur - _lastHealth) > 0.01f)
        {
            HudController.HealthBar.Flash();
        }
        _lastHealth = cur;
    }
}
