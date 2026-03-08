using System.Collections.Generic;
using Il2CppMonomiPark.SlimeRancher.UI;
using Il2CppMonomiPark.SlimeRancher.UI.HUD;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DynamicHud;

public static class HudController
{
    public static HudElement HealthBar { get; private set; } = new("HealthBar");
    public static HudElement EnergyBar { get; private set; } = new("EnergyBar");
    public static HudElement RadBar { get; private set; } = new("RadBar");
    public static HudElement Crosshair { get; private set; } = new("Crosshair");
    public static HudElement Compass { get; private set; } = new("Compass");
    public static HudElement Currency { get; private set; } = new("Currency");
    public static HudElement Clock { get; private set; } = new("Clock");

    private static readonly Dictionary<int, HudElement> _ammoSlots = new();
    private static bool _slotsResolved;

    private static readonly List<HudElement> AllElements = new();
    private static bool _initialized;
    private static int _initAttempts;
    private static float _nextInitAttemptAt;

    public static void EnsureInitAndUpdate()
    {
        if (!DynamicHudMod.EnableDynamicHud.Value)
        {
            RestoreOriginal();
            return;
        }

        // Reset if tracked objects were destroyed (e.g. returned to main menu)
        if (_initialized && !IsUICoreLoaded())
        {
            WriteLog("UICore unloaded - resetting initialization.");
            _initialized = false;
            _slotsResolved = false;
            AllElements.Clear();
            _ammoSlots.Clear();
        }

        if (!_initialized && IsUICoreLoaded() && Time.unscaledTime >= _nextInitAttemptAt)
            TryInitialize();

        if (!_initialized) return;

        if (!_slotsResolved)
            TryResolveSlots();

        float dt = Time.deltaTime;
        foreach (var element in AllElements)
            element.Update(dt);
    }

    public static void FlashAllSlots()
    {
        foreach (var kv in _ammoSlots)
            kv.Value.Flash();
    }

    public static void FlashSlotByViewHolder(AmmoSlotViewHolder viewHolder)
    {
        if (viewHolder == null) return;
        var go = viewHolder.gameObject;
        foreach (var kv in _ammoSlots)
        {
            if (kv.Value.GameObject == go)
            {
                kv.Value.Flash();
                return;
            }
        }
        // Unknown slot — flash everything as fallback
        FlashAllSlots();
    }

    private static bool IsUICoreLoaded()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.name == "UICore" && s.isLoaded)
                return true;
        }
        return false;
    }

    private static void TryInitialize()
    {
        _initAttempts++;
        _nextInitAttemptAt = Time.unscaledTime + 2f;

        AllElements.Clear();
        _ammoSlots.Clear();
        _slotsResolved = false;
        HealthBar = new("HealthBar");
        EnergyBar = new("EnergyBar");
        RadBar = new("RadBar");
        Crosshair = new("Crosshair");
        Compass = new("Compass");
        Currency = new("Currency");
        Clock = new("Clock");

        try
        {
            var hudUI = Object.FindObjectOfType<HudUI>();
            if (hudUI == null)
            {
                WriteLog($"HudUI not found on attempt #{_initAttempts}.");
                return;
            }

            var hudRoot = hudUI.gameObject;
            WriteLog($"Found HudUI: '{hudRoot.name}'");

            SetupFromType<HealthMeter>(HealthBar, "HealthMeter");
            SetupFromType<EnergyMeter>(EnergyBar, "EnergyMeter");
            SetupFromType<RadMeter>(RadBar, "RadMeter");
            SetupFromType<CrosshairUI>(Crosshair, "CrosshairUI");
            SetupFromType<CompassBarUI>(Compass, "CompassBarUI");
            SetupFromType<HudCurrencyDisplay>(Currency, "HudCurrencyDisplay");

            var timePanel = FindChild(hudRoot, "TimePanel");
            if (timePanel != null)
                SetupElement(Clock, timePanel);
            else
                WriteLog("  TimePanel: not found");

            if (AllElements.Count > 0)
            {
                _initialized = true;
                WriteLog($"Tracking {AllElements.Count} elements (attempt #{_initAttempts}).");
            }
            else
            {
                WriteLog($"No elements matched on attempt #{_initAttempts}.");
            }
        }
        catch (System.Exception ex)
        {
            WriteLog($"ERROR during init: {ex}");
            _initialized = false;
        }
    }

    private static void TryResolveSlots()
    {
        var slotViews = Object.FindObjectsOfType<AmmoSlotViewHolder>(true);
        if (slotViews == null || slotViews.Length == 0) return;

        _slotsResolved = true;
        WriteLog($"Resolved {slotViews.Length} ammo slot views.");
        for (int i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] == null) continue;
            var slotElement = new HudElement($"AmmoSlot_{i}");
            SetupElement(slotElement, slotViews[i].gameObject);
            _ammoSlots[i] = slotElement;
        }
    }

    private static void SetupFromType<T>(HudElement element, string typeName) where T : Component
    {
        var component = Object.FindObjectOfType<T>(true);
        if (component != null)
        {
            SetupElement(element, component.gameObject);
        }
        else
        {
            WriteLog($"  {typeName}: not found");
        }
    }

    private static void RestoreOriginal()
    {
        foreach (var element in AllElements)
            element.RestoreOriginal();
    }

    private static void SetupElement(HudElement element, GameObject go)
    {
        element.GameObject = go;
        element.DiscoverGraphics();
        AllElements.Add(element);
        WriteLog($"  Tracking: {element.Name} -> '{go.name}' (hasGraphics={element.HasGraphics})");
    }

    private static GameObject? FindChild(GameObject parent, string exactName)
    {
        var transforms = parent.GetComponentsInChildren<Transform>(true);
        foreach (var t in transforms)
        {
            if (t != null && t.gameObject.name == exactName)
                return t.gameObject;
        }
        return null;
    }

    internal static string LogPath { get; set; } = "DynamicHud_debug.log";

    internal static void WriteLog(string msg)
    {
        if (!DynamicHudMod.DebugLogging.Value) return;
        try
        {
            System.IO.File.AppendAllText(LogPath,
                $"[{System.DateTime.Now:HH:mm:ss.fff}] {msg}\n");
        }
        catch { }
    }
}
