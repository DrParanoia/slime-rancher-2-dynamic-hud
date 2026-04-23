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

- **Namespaces are prefixed with `Il2Cpp`** for types that would clash with .NET BCL types - `Il2CppTMPro` (not `TMPro`), `Il2CppMonomiPark.SlimeRancher.*`, etc. But types in `Unity*`-prefixed assemblies keep their original namespaces - e.g. `UnityEngine.InputSystem.InputAction` (NOT `Il2CppUnityEngine.InputSystem`).
- **`HudUI.Instance` does not exist** - use `Object.FindObjectOfType<HudUI>()`.
- **Inactive objects need `FindObjectOfType<T>(true)`** - `RadMeter` in particular is often inactive and won't be found with the default overload.
- **Type casting uses `TryCast<T>()`**, not C# `as` - e.g. `graphic.TryCast<TMP_Text>()`.
- **`NullableAttribute` polyfill required** - the IL2CPP-generated `Il2Cppmscorlib.dll` shadows the real `System.Runtime.CompilerServices.NullableAttribute`. The compiler can't find matching constructors and fails with CS0656 across every nullable-reference-type annotation. [NullableAttributes.cs](src/NullableAttributes.cs) polyfills the attribute in our own assembly so the compiler finds it first. Do not delete that file even though it looks unused.

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

[HudElement.DiscoverGraphics()](src/HudElement.cs) classifies each `Graphic` as either background or content, then applies `BackgroundAlpha` or `ContentAlpha` respectively:
- Text (`TMP_Text`, `Text`) → content
- Root `Image` on the tracked GameObject → background
- Other `Image` with a "container-ish" name (container/frame/border/fill/bg/panel/bar/mask/overlay/etc.) → background
- Everything else → content

This heuristic works for all currently tracked elements but may need tweaking if new HUD types are added.

### Per-element alpha overrides

Elements can opt out of the global alpha values via `BackgroundAlphaOverride` and `ContentAlphaOverride` (both `Func<float>?`). The pinned recipe list uses this pattern to have its own `PinnedRecipeAlpha` setting. To override, set both funcs to read the same preference - the two-tier classification still happens, but both tiers end up at the same value.

## InputSystem bindings

Peek uses Unity's InputSystem (`UnityEngine.InputSystem.InputAction`) with separate actions per device to keep the call sites simple:

```csharp
var action = new InputAction("Name", InputActionType.Button, "<Keyboard>/leftAlt");
action.Enable();
// Each frame: action.IsPressed()  or  action.WasPressedThisFrame()
```

Bindings are path strings like `<Keyboard>/leftAlt`, `<Gamepad>/rightStickPress`. For future bindings, create one `InputAction` per device path rather than one action with multiple bindings - avoids IL2CPP extension-method quirks with `AddBinding`. Bindings are read at mod init, so the user must restart the game after editing preferences.

## Handling SR2 game updates

When a new SR2 version drops:

1. Back up `game/UserData/` (your MelonPreferences + saves) and `game/steam_appid.txt`.
2. Delete the entire `game/` folder. Stale IL2CPP assemblies cause weird compile/runtime mismatches.
3. Copy the updated SR2 install into `game/`, then run the MelonLoader installer against `game/SlimeRancher2.exe`.
4. Restore `steam_appid.txt` and `UserData/`.
5. Launch once - MelonLoader regenerates IL2CPP assemblies against the new game build.
6. `dotnet build src/DynamicHud.csproj` - any compile errors point to renamed/removed types or methods.
7. Test in-game with `DebugLogging = true` and check elements still resolve.

Expected breakage that isn't your fault: CS0656 `NullableAttribute..ctor` errors if the new `Il2Cppmscorlib.dll` shadows `System.Runtime`. Already handled by [NullableAttributes.cs](src/NullableAttributes.cs) - don't delete it even if the build temporarily succeeds without it.

## Release checklist

Version is defined in exactly one place: the third arg to `MelonInfo` in [AssemblyInfo.cs](src/Properties/AssemblyInfo.cs). The csproj has no `<Version>` element.

1. Bump the MelonInfo version in [AssemblyInfo.cs](src/Properties/AssemblyInfo.cs).
2. `dotnet build src/DynamicHud.csproj`.
3. Commit, push `main`.
4. Zip just `game/Mods/DynamicHud.dll` as `DynamicHud-vX.Y.Z.zip`.
5. `gh release create vX.Y.Z DynamicHud-vX.Y.Z.zip --title vX.Y.Z --notes "..."`.
6. Upload the same zip to NexusMods as a new file (don't overwrite old versions - users may want to roll back).

The version convention is to match the target SR2 game version.

## Debug logging

Controlled by the `DebugLogging` MelonPreference (default false). Log file is written to `DynamicHud_debug.log` next to the mod DLL inside the `Mods/` folder. `HudController.WriteLog()` is a no-op when disabled, so there's no performance cost to leaving log calls in the code.
