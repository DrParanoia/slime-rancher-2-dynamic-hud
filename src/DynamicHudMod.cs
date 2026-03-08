using System.IO;
using System.Reflection;
using MelonLoader;

namespace DynamicHud;

public class DynamicHudMod : MelonMod
{
    public static DynamicHudMod Instance { get; private set; } = null!;

    // Preferences
    internal static MelonPreferences_Category Category = null!;
    internal static MelonPreferences_Entry<bool> EnableDynamicHud = null!;
    internal static MelonPreferences_Entry<float> BackgroundAlpha = null!;
    internal static MelonPreferences_Entry<float> ContentAlpha = null!;
    internal static MelonPreferences_Entry<float> FadeInDuration = null!;
    internal static MelonPreferences_Entry<float> FadeOutDuration = null!;
    internal static MelonPreferences_Entry<float> OpaqueHoldDuration = null!;
    internal static MelonPreferences_Entry<bool> DebugLogging = null!;

    public override void OnInitializeMelon()
    {
        Instance = this;

        Category = MelonPreferences.CreateCategory("DynamicHud", "Dynamic HUD");

        EnableDynamicHud = Category.CreateEntry("Enabled", true,
            "Enable Dynamic HUD",
            "When enabled, HUD elements fade out when idle and fade in on activity.");

        BackgroundAlpha = Category.CreateEntry("BackgroundAlpha", 0.15f,
            "Background Alpha",
            "Opacity of HUD element backgrounds when idle (0 = invisible, 1 = fully opaque).");

        ContentAlpha = Category.CreateEntry("ContentAlpha", 0.4f,
            "Content Alpha",
            "Opacity of HUD icons and text when idle (0 = invisible, 1 = fully opaque).");

        FadeInDuration = Category.CreateEntry("FadeInDuration", 0.2f,
            "Fade In Duration",
            "How quickly HUD elements become opaque (seconds).");

        FadeOutDuration = Category.CreateEntry("FadeOutDuration", 1.5f,
            "Fade Out Duration",
            "How quickly HUD elements fade back to idle transparency (seconds).");

        OpaqueHoldDuration = Category.CreateEntry("OpaqueHoldDuration", 1.3f,
            "Opaque Hold Duration",
            "How long HUD elements stay fully opaque after an event (seconds).");

        DebugLogging = Category.CreateEntry("DebugLogging", false,
            "Debug Logging",
            "Write detailed debug info to DynamicHud_debug.log next to the mod DLL.");

        // Log file lives next to the mod DLL (inside the Mods folder)
        var dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        HudController.LogPath = Path.Combine(dllDir, "DynamicHud_debug.log");

        HudController.WriteLog("===== New Run =====");
        HudController.WriteLog("OnInitializeMelon fired");

        HarmonyInstance.PatchAll();
        HudController.WriteLog("Harmony patches applied.");

        LoggerInstance.Msg("Dynamic HUD initialized.");
    }

    public override void OnUpdate()
    {
        HudController.EnsureInitAndUpdate();
    }
}
