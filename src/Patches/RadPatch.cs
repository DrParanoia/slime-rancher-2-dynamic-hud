using HarmonyLib;

namespace DynamicHud.Patches;

[HarmonyPatch(typeof(Il2Cpp.PlayerState), nameof(Il2Cpp.PlayerState.AddRads))]
public static class RadAddPatch
{
    public static void Postfix()
    {
        HudController.RadBar.Flash();
    }
}
