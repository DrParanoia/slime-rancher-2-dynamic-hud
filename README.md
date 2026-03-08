# Dynamic HUD for Slime Rancher 2

A MelonLoader mod that makes HUD elements fade to near-transparency when idle and briefly become opaque when relevant events occur. Designed to reduce static bright UI elements - especially useful for OLED displays that suffer from Auto Brightness Limiting (ABL).

## Features

- **Two-tier transparency** - backgrounds fade more aggressively (default 15% opacity) while icons and text stay more readable (default 40% opacity)
- **Event-driven flash** - each HUD element becomes fully opaque only when something relevant happens:
  - Health bar: taking damage or healing
  - Energy bar: spending energy
  - Radiation meter: gaining rads
  - Currency display: earning or spending newbucks
  - Hotbar: switching slots or picking up / shooting / clearing items (per-slot tracking)
  - Clock, compass, crosshair: faded with everything else
- **Smooth transitions** - configurable fade-in and fade-out durations
- **Session-safe** - correctly resets when returning to the main menu and reloading a save
- **Fully configurable** - all values adjustable via MelonLoader preferences

## Installation

1. Install [MelonLoader](https://melonwiki.xyz/) v0.7+ for Slime Rancher 2
2. Download `DynamicHud.dll` from the [Releases](../../releases) page
3. Place it in your `SlimeRancher2/Mods/` folder
4. Launch the game

## Configuration

Settings are in `UserData/MelonPreferences.cfg` under the `[DynamicHud]` section after first launch:

| Setting | Default | Description |
|---------|---------|-------------|
| Enabled | true | Master toggle for the mod |
| BackgroundAlpha | 0.15 | Opacity of HUD backgrounds when idle (0–1) |
| ContentAlpha | 0.4 | Opacity of icons and text when idle (0–1) |
| FadeInDuration | 0.2 | Seconds to fade in to full opacity |
| FadeOutDuration | 1.5 | Seconds to fade back to idle transparency |
| OpaqueHoldDuration | 1.3 | Seconds to stay fully opaque after an event |
| DebugLogging | false | Write debug info to `DynamicHud_debug.log` in the Mods folder |

## Building from source

### Prerequisites

- .NET 6.0 SDK
- A copy of Slime Rancher 2 with MelonLoader installed

### Steps

1. Clone this repository
2. Copy (or symlink) your Slime Rancher 2 game folder into the repo root as `game/`:
   ```
   slime-rancher-dynamic-hud/
     game/              <- your SR2 install with MelonLoader
     src/
     slime-rancher-dynamic-hud.sln
   ```
3. Build:
   ```
   dotnet build src/DynamicHud.csproj
   ```
   The output DLL is placed directly into `game/Mods/`.

## License

[MIT](LICENSE)
