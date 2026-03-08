using System.Collections.Generic;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DynamicHud;

/// <summary>
/// Tracks the fade state of a single HUD element.
/// Uses two-tier alpha: Image components (backgrounds/panels) fade to BackgroundAlpha,
/// while text components remain more visible at ContentAlpha.
/// </summary>
public class HudElement
{
    public string Name { get; }
    public GameObject? GameObject { get; set; }

    // Images (backgrounds, panels, icons) vs Text (labels, counts)
    private readonly List<GraphicEntry> _backgroundGraphics = new();
    private readonly List<GraphicEntry> _contentGraphics = new();

    // Blend factor: 0 = idle (transparent), 1 = active (opaque)
    private float _blend;
    private float _targetBlend;
    private float _holdTimer;

    public HudElement(string name)
    {
        Name = name;
        _blend = 0f;
        _targetBlend = 0f;
    }

    /// <summary>
    /// Call after setting GameObject to discover and categorize all graphics.
    /// </summary>
    public void DiscoverGraphics()
    {
        if (GameObject == null) return;

        _backgroundGraphics.Clear();
        _contentGraphics.Clear();

        // Root Image on the tracked GameObject itself = always background
        var rootImage = GameObject.GetComponent<Image>();
        if (rootImage != null)
            _backgroundGraphics.Add(new GraphicEntry(rootImage, rootImage.color.a));

        var allGraphics = GameObject.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < allGraphics.Count; i++)
        {
            var g = allGraphics[i];
            if (g == null || g == rootImage) continue;

            // Text = content (stays readable)
            if (g.TryCast<TMP_Text>() != null || g.TryCast<Text>() != null)
            {
                _contentGraphics.Add(new GraphicEntry(g, g.color.a));
            }
            // Images on structural/container GameObjects = background
            else if (IsContainerName(g.gameObject.name))
            {
                _backgroundGraphics.Add(new GraphicEntry(g, g.color.a));
            }
            // Everything else (icons, small images) = content
            else
            {
                _contentGraphics.Add(new GraphicEntry(g, g.color.a));
            }
        }
    }

    public bool HasGraphics => _backgroundGraphics.Count > 0 || _contentGraphics.Count > 0;

    /// <summary>
    /// Call this to make the element opaque briefly (e.g. on damage, slot switch).
    /// </summary>
    public void Flash()
    {
        _targetBlend = 1f;
        _holdTimer = DynamicHudMod.OpaqueHoldDuration.Value;
    }

    /// <summary>
    /// Called every frame to update the alpha.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!HasGraphics) return;

        // Count down hold timer
        if (_holdTimer > 0f)
        {
            _holdTimer -= deltaTime;
            _targetBlend = 1f;
        }
        else
        {
            _targetBlend = 0f;
        }

        // Animate blend factor
        if (_blend < _targetBlend)
        {
            float speed = DynamicHudMod.FadeInDuration.Value > 0f
                ? deltaTime / DynamicHudMod.FadeInDuration.Value
                : 1f;
            _blend = Mathf.MoveTowards(_blend, _targetBlend, speed);
        }
        else if (_blend > _targetBlend)
        {
            float speed = DynamicHudMod.FadeOutDuration.Value > 0f
                ? deltaTime / DynamicHudMod.FadeOutDuration.Value
                : 1f;
            _blend = Mathf.MoveTowards(_blend, _targetBlend, speed);
        }

        // Apply two-tier alpha
        float bgAlpha = Mathf.Lerp(DynamicHudMod.BackgroundAlpha.Value, 1f, _blend);
        float contentAlpha = Mathf.Lerp(DynamicHudMod.ContentAlpha.Value, 1f, _blend);

        ApplyAlpha(_backgroundGraphics, bgAlpha);
        ApplyAlpha(_contentGraphics, contentAlpha);
    }

    /// <summary>
    /// Restore all graphics to their original alpha (used when mod is disabled).
    /// </summary>
    public void RestoreOriginal()
    {
        foreach (var entry in _backgroundGraphics)
        {
            if (entry.Graphic == null) continue;
            var c = entry.Graphic.color;
            c.a = entry.OriginalAlpha;
            entry.Graphic.color = c;
        }
        foreach (var entry in _contentGraphics)
        {
            if (entry.Graphic == null) continue;
            var c = entry.Graphic.color;
            c.a = entry.OriginalAlpha;
            entry.Graphic.color = c;
        }
    }

    private static void ApplyAlpha(List<GraphicEntry> entries, float alpha)
    {
        foreach (var entry in entries)
        {
            if (entry.Graphic == null) continue;
            var c = entry.Graphic.color;

            // If the game changed the alpha since we last wrote it,
            // update OriginalAlpha to respect the game's intent
            // (e.g. showing/hiding "EMPTY" text or item counts).
            if (entry.LastAppliedAlpha >= 0f &&
                System.Math.Abs(c.a - entry.LastAppliedAlpha) > 0.001f)
            {
                entry.OriginalAlpha = c.a;
            }

            float target = entry.OriginalAlpha * alpha;
            c.a = target;
            entry.LastAppliedAlpha = target;
            entry.Graphic.color = c;
        }
    }

    private static bool IsContainerName(string goName)
    {
        string lower = goName.ToLowerInvariant();
        return lower.Contains("container") || lower.Contains("frame") ||
               lower.Contains("border") || lower.Contains("fill") ||
               lower.Contains("outer") || lower.Contains("inner") ||
               lower.Contains("background") || lower.Contains("bg") ||
               lower.Contains("panel") || lower.Contains("behind") ||
               lower.Contains("bar") || lower.Contains("mask") ||
               lower.Contains("overlay") || lower.Contains("drop");
    }

    private class GraphicEntry
    {
        public readonly Graphic Graphic;
        public float OriginalAlpha;
        public float LastAppliedAlpha = -1f;

        public GraphicEntry(Graphic graphic, float originalAlpha)
        {
            Graphic = graphic;
            OriginalAlpha = originalAlpha;
        }
    }
}
