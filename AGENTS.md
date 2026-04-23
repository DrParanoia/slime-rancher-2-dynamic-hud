# Agent notes

Non-obvious findings for working on this codebase. Read before making changes.

## Development setup

- The `game/` folder (gitignored) must contain the full Slime Rancher 2 install with MelonLoader. The csproj references DLLs from `game/MelonLoader/net6/` and `game/MelonLoader/Il2CppAssemblies/`.
- Build output goes directly to `game/Mods/DynamicHud.dll` via the csproj's `OutputPath`. No copy step needed.
- `dotnet build src/DynamicHud.csproj` is the only build command.

## Critical: `steam_appid.txt`

`game/steam_appid.txt` contains `1657630` (SR2's Steam app ID). Without this file, launching the modded executable triggers Steam to relaunch from the original unmodded install, and the mod process silently dies after ~7 seconds in the `Bootstrap` scene. All update loops stop, Harmony patches never fire, and debugging looks like the mod simply doesn't work.

If the mod appears to "stop working" shortly after launch with no errors, check this file exists first.

## IL2CPP quirks

- **Namespaces are prefixed with `Il2Cpp`**: use `Il2CppTMPro` (not `TMPro`), `Il2CppMonomiPark.SlimeRancher.*`, etc.
- **`HudUI.Instance` does not exist** - use `Object.FindObjectOfType<HudUI>()`.
- **Inactive objects need `FindObjectOfType<T>(true)`** - `RadMeter` in particular is often inactive and won't be found with the default overload.
- **Type casting uses `TryCast<T>()`**, not C# `as` - e.g. `graphic.TryCast<TMP_Text>()`.

## HUD lifecycle

- The HUD only exists when the `UICore` scene is loaded. During `Bootstrap`, `SystemCore`, or the main menu, `HudUI` does not exist and `FindObjectOfType` returns null.
- Init must be deferred until `UICore` is loaded. [HudController.IsUICoreLoaded()](src/HudController.cs) checks this.
- When the player quits to main menu, `UICore` is unloaded and all tracked GameObjects are destroyed. The mod must detect this and reset `_initialized = false`, clear `AllElements`, and clear `_ammoSlots`. Otherwise on reload the mod holds dead references and does nothing.
- `AmmoSlotViewHolder` instances are created **after** `HudUI` init completes. They need lazy resolution on a subsequent frame via `TryResolveSlots()`, not during initial setup.

## The alpha-fighting pattern (v1.0.1 fix)

The game dynamically mutates `Graphic.color.a` to show/hide UI elements:
- "EMPTY" text on an empty ammo slot: alpha toggles between visible and 0 as items enter/leave
- Item count text: alpha goes from 0 to visible when the slot has items

Since the mod writes alpha every frame based on a cached `OriginalAlpha`, it would overwrite the game's changes and either leave "EMPTY" permanently visible or hide item counts permanently.

**Solution in [HudElement.ApplyAlpha()](src/HudElement.cs)**: each `GraphicEntry` tracks `LastAppliedAlpha` (the value the mod last wrote). Before applying the new alpha, compare the graphic's current alpha to `LastAppliedAlpha`. If they differ, the game changed it - update `OriginalAlpha` to the new value before applying the fade multiplier. `0 * anything = 0`, so hidden stays hidden.

**Do not switch to `CanvasGroup.alpha`** - a parent CanvasGroup multiplies with child CanvasGroups (e.g. parent 0.15 × slot 0.15 = 0.02 effective alpha), which was the original approach and produced near-invisible hotbars.

## Event sources (which Harmony target to patch)

Choosing the right method matters - patching `PlayerState` alone often misses events because some flow only through the UI-side callback.

| Event | Patched method(s) | Why |
|-------|-------------------|-----|
| Damage/heal | `PlayerState.Damage`, `PlayerState.Heal`, `HealthMeter.Update` (fallback) | `PlayerState` patches can miss e.g. passive regen |
| Energy spend | `PlayerState.SpendEnergy` **and** `EnergyMeter.OnCurrentEnergyChanged` | `PlayerState.SpendEnergy` alone does not fire in all cases |
| Radiation | `PlayerState.AddRads` | Sufficient |
| Currency | `PlayerState.AddCurrency`, `PlayerState.SpendCurrency` | Sufficient |
| Hotbar switch | `AmmoSlotManager.SetSelectedSlot` | Covers selection changes |
| Hotbar slot content | `AmmoSlotViewHolder.UpdateAmmoDisplay` | One patch covers pickup, shoot, and clear. **Do not** use `MaybeAddToSlot`/`MaybeAddToSelectedSlot` - they fire spuriously and cause random flashes |

## Two-tier alpha heuristic

[HudElement.DiscoverGraphics()](src/HudElement.cs) classifies each `Graphic` as either background or content:
- Text (`TMP_Text`, `Text`) → content (40% idle)
- Root `Image` on the tracked GameObject → background (15% idle)
- Other `Image` with a "container-ish" name (container/frame/border/fill/bg/panel/bar/mask/overlay/etc.) → background
- Everything else → content

This heuristic works for all currently tracked elements but may need tweaking if new HUD types are added.

## Debug logging

Controlled by the `DebugLogging` MelonPreference (default false). Log file is written to `DynamicHud_debug.log` next to the mod DLL inside the `Mods/` folder. `HudController.WriteLog()` is a no-op when disabled, so there's no performance cost to leaving log calls in the code.
