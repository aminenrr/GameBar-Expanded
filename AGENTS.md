# Controls Hub: agent brief and plan

Repo: github.com/aminenrr/GameBar-Expanded (public). App name: **Controls Hub**.
Owner: Amine (aminenrr). Laptop: Alienware m15 R7, Windows 11, RTX 3070 Ti, 240 Hz panel. Controller: DualSense (used 99% of the time, never an Xbox pad).

Read this whole file before you change anything. It is the single source of truth for what we are building, what is decided, and what is left. Update the Status section at the end of every work session.

## Rules

* Never use em dashes or en dashes in anything written for Amine (docs, UI text, commit messages, toasts). Use commas, colons or "to".
* Zero idle cost beats feature count. No polling loops while nothing is visible. Event driven everywhere (pipes push, hooks fire). Sample only while a surface that shows the value is on screen.
* Stability and low RAM first. Target: broker under 15 MB working set in the tray, widget only alive while Game Bar is open.
* Legend look everywhere (same family as Alien Controls, Dolby Controls, DualSense Controls). Colors and type below.
* PlayStation prompts only, drawn in the Legend style (cyan ring, white shape). Never Sony art, never colored face buttons.
* Never inject into games. Anything that draws inside a game goes through RTSS (if the user has it) or a click through window.
* Mockups are signed off before UI is built. Mockup sources live in `design/mockups/` (Python SVG scripts, rendered PNGs).
* Small commits, one feature each. Tags `vX.Y.Z-pN` for prototypes, `vX.Y.Z-betaN` later.

## What it is

One Xbox Game Bar widget, "Controls", that controls Amine's three tray apps without leaving the game:

| Tab | App | Repo | Stack |
|---|---|---|---|
| ALIEN | Alien Controls | aminenrr/alien-control | C# / WPF (.NET) |
| DOLBY | Dolby Controls | aminenrr/Dolby-Controls | C# / WPF (.NET) |
| DUALSENSE | DualSense Controls | aminenrr/DualSense-Controls | Rust (fork of MaximumFreak/dsz, egui) |

Plus per game profiles (one press saves the three apps' settings for the game in front, applied on every launch), a glance strip, and later a SteamOS style performance overlay.

The widget is a remote control. The three apps keep doing the real work; they gain a small pipe server each. If an app is not running its tab shows a Start button.

## Decisions (signed off by Amine, 2026-10-23)

1. **One widget, three tabs.** L1 / R1 switch ALIEN, DOLBY, DUALSENSE. Remembers the last tab.
2. **Glance strip on every tab** under the tabs: Alien preset (in its color), Atmos state and profile, DualSense battery. States: normal, live change, warnings (G-Mode on orange, Atmos off dim, battery 20% or less orange), app closed (grey, "NOT RUNNING"). D pad up from content reaches it, Cross jumps to that tab, Square quick toggles (next preset, Atmos on / off).
3. **ALIEN tab is a preset changer**, not a performance mode picker: Battery (amber #FFB020), Quiet (green #40DC78), Balanced (cyan #00E5FF), Performance (red #FF4D5E), Custom (violet #9B7BFF). Same presets as the Alien Controls tray menu (power mode + CPU boost + refresh rate; Custom changes nothing; never touches GPU mode). Sensors card: CPU / GPU temp, CPU / GPU fan. Quick tiles: G-Mode (F1), Refresh rate, Key light, Clean RAM. GPU mode is NOT in the widget (needs a restart).
4. **DOLBY tab**: output list (one press sets default, Pixel Buds shown with L / R battery when in the case), Atmos toggle + Dynamic, Movie, Music, Game, Voice, Custom, PRO X volume and mic level while plugged in.
5. **DUALSENSE tab**: controller card (battery, link Hz, haptic lag), mode Native / DualSense / Xbox, FEEL card (haptics gain, rumble gain, trigger cap).
6. **In game controls only.** Fans, GPU mode, lighting effects, EQ bands, mappings, lightbar stay in the full apps behind "Open <app>".
7. **Prompts: Legend style** (cyan ring, white shape, since the DualSense buttons have no colors). Cross = select, Circle = back, Square = quick toggle, Triangle = save for this game, L1 / R1 = tabs, D pad = move, Options = open the full app, PS = open overlay.
8. **Game Bar with a DualSense** (Game Bar only reads Xbox pads; tested 2026-10-20 on Game Bar 7.326.10141):
   * PS button opens Game Bar in every mode: DualSense Controls sends Win+G (SendInput) and hides the press from the game. Built in setting, on by default.
   * Our widget does not depend on Game Bar input: DualSense Controls streams nav events (L1, R1, D pad, Cross, Circle, Square, Triangle, Options) to the broker, which forwards them to the widget. Works in any mode.
   * Rest of Game Bar (home bar, other widgets): a hidden Xbox pad plugs in only while the overlay is open, in every mode, gets the whole pad, and unplugs when it closes. The game gets an idle pad (DualSense / Xbox mode). DSZ already has the open detection (`virt/gamebar.rs`) and routing (`pipeline.rs` `to_gamebar`); extend it.
   * **Native mode: option 1.** While the overlay is open, hide the real DualSense from games with HidHide; unhide on close. The game sees a short unplug (Amine usually pauses before opening Game Bar anyway).
9. **Per game profiles**:
   * Open the overlay in game once, set each tab, press Triangle. A sheet shows the game's full name (renamable, on screen keyboard), its exe, and one checked row per app (Alien preset, Dolby Atmos profile, DualSense mode). Uncheck what should be left alone. Toggle "Restore when the game closes" (on). Cross saves.
   * On every launch (game window takes the foreground), the broker applies it and shows a toast. **Title is the game name only** (e.g. "Call of Duty: Modern Warfare 4", no word "profile"); body lists only what it changed (chips: PERFORMANCE in red, ATMOS GAME, NATIVE).
   * On quit: toast "Back to your setup" listing what was restored.
   * Two toast styles, setting in the broker: **Pill** (DualSense Controls pill style, top center, drops in 220 ms, holds 2 s, slides up 160 ms) or **Classic** (Windows app notification). Default Pill.
   * **No clashing toasts:** commands sent by the hub carry `"quiet": true`; the apps apply silently (no own pill, chime, or tray flash). Only the hub's toast shows.
   * Overlay shows a green GAME PROFILE chip next to the game name while one is active. Triangle again updates it.
10. **Name**: Controls Hub (all of Amine's apps have "Controls" in the name). Widget title in Game Bar: "Controls".

## Architecture

```
 Game Bar (XboxGameBar.exe)
   └─ Controls widget  (UWP XAML, C#, AppContainer, alive only while Game Bar shows it)
          │  AppService "ControlsHub.Broker" (in package)
          ▼
 Controls Hub broker  (full trust exe in the same MSIX, starts at sign in via StartupTask)
   ├─ pipe client  \\.\pipe\ControlsHub.Alien      → Alien Controls (pipe server, new)
   ├─ pipe client  \\.\pipe\ControlsHub.Dolby      → Dolby Controls (pipe server, new)
   ├─ pipe client  \\.\pipe\ControlsHub.DualSense  → DualSense Controls (pipe server, new)
   ├─ game watcher (SetWinEventHook EVENT_SYSTEM_FOREGROUND, no polling)
   ├─ profiles     %LOCALAPPDATA%\ControlsHub\profiles.json
   ├─ toasts       pill window (Win32 layered) or Windows app notification
   └─ later: perf overlay (PresentMon ETW, NVML, RTSS shared memory)
```

Why a broker: the widget is sandboxed and dies when Game Bar closes; profiles must fire when no overlay is open. The broker is the only always on piece and must stay tiny.

Broker language: **C# (.NET 8, NativeAOT)** so it shares the protocol types with the widget and the two WPF apps; NativeAOT keeps RAM low and start fast. If NativeAOT blocks AppService or notifications, fall back to framework dependent .NET 8 and measure.

### Repo layout (target)

```
AGENTS.md                this file
README.md
design/mockups/          SVG scripts + PNGs (hub tabs, glance strip, prompts, profile flow)
docs/PROTOCOL.md         pipe protocol, the contract the three apps implement
docs/ROADMAP.md          future items (perf overlay etc.)
src/ControlsHub.Protocol/   shared C# messages (netstandard2.0 so UWP can use it)
src/ControlsHub.Broker/     full trust broker (.NET 8)
src/ControlsHub.Widget/     UWP Game Bar widget (XAML, C#)
src/ControlsHub.Package/    Windows Application Packaging Project (MSIX, manifest, assets)
integrations/alien/      patch notes + code to drop into alien-control
integrations/dolby/      same for Dolby-Controls
integrations/dualsense/  Rust module for DualSense-Controls (crates/dsz/src/legend/hub.rs)
tests/
```

### Package manifest essentials

* `uap3:Extension Category="windows.appExtension"` Name `microsoft.gameBarUIExtension`, Id `ControlsWidget`, DisplayName "Controls", with `<GameBarWidget Type="Standard">`, `<HomeMenuVisible>true</HomeMenuVisible>`, `<PinningSupported>true</PinningSupported>`, `<SettingsSupported>false</SettingsSupported>`, window size about 470 x 650, resizable false.
* `uap:Extension Category="windows.appService"` Name `ControlsHub.Broker`.
* `desktop:Extension Category="windows.fullTrustProcess"` for the broker, `uap5:StartupTask` so it runs at sign in.
* Capability `runFullTrust`. Reference `Microsoft.Gaming.XboxGameBar` NuGet in the widget.
* Sideload with a self signed dev cert (script in `dev/`). Store publishing is out of scope.

### Pipe protocol (docs/PROTOCOL.md is authoritative once written)

UTF 8 JSON, one message per line. The app is the pipe server; the broker connects, says hello, subscribes, then everything is pushed.

```
→ {"t":"hello","client":"ControlsHub","v":1}
← {"t":"hello","app":"alien","v":1,"version":"1.3.0"}
→ {"t":"sub"}
← {"t":"state", ...full state...}           once
← {"t":"state", ...only changed keys...}    on every change, never on a timer
→ {"t":"set","key":"preset","value":"performance","quiet":true,"id":7}
← {"t":"ack","id":7,"ok":true}
→ {"t":"open"}                               show the app's main window
```

Keys per app (v1):
* alien: `preset` (battery|quiet|balanced|performance|custom), `gmode` (bool), `refresh_hz`, `refresh_options`, `key_light` (0..100), `cpu_temp`, `gpu_temp`, `cpu_fan`, `gpu_fan` (sensors pushed at most 1 Hz and only while the broker says `{"t":"watch","sensors":true}`, i.e. the widget is visible), `power_w`, `ac` (bool), `clean_ram` (action).
* dolby: `outputs` [{id,name,kind,active,available,battery?}], `output` (id), `atmos` (bool), `atmos_profile` (dynamic|movie|music|game|voice|custom), `prox` {present, volume, mic}.
* dualsense: `connected`, `link` (bt|usb), `battery`, `charging`, `link_hz`, `haptic_lag_ms`, `mode` (native|dualsense|xbox), `haptics_gain`, `rumble_gain`, `trigger_cap`, plus a nav stream `{"t":"nav","btn":"l1|r1|up|down|left|right|cross|circle|square|triangle|options","down":true}` sent only while the broker says `{"t":"nav","on":true}` (overlay open).

Security: pipes created with an ACL for the current user only (plus the package SID if the widget ever connects directly). Reject anything not on the key list. Clamp values like the apps already do on load.

## Plan

Each phase ends with a build Amine tests on the laptop. Do not start the next phase before his OK.

### P0: Spike (prove the risky parts)
1. Minimal Game Bar widget (XAML "hello") + packaging project + dev cert script; sideload; widget appears in the Game Bar menu and pins.
2. Widget to broker AppService round trip; broker launched by FullTrustProcessLauncher and by StartupTask.
3. Broker connects to a dummy pipe server; push a value; widget shows it within one frame, no polling.
4. DualSense nav: a fake nav stream moves focus in the widget while Game Bar is open and the widget is not focused.
5. Win+G via SendInput from a normal desktop process opens Game Bar over MW4 (borderless) and over a fullscreen exclusive game.
6. HidHide hide / unhide of the DualSense while a game runs: measure time, confirm DualSense Controls keeps reading the pad (its own process must be on the HidHide allow list).
7. Measure: broker RAM idle, widget RAM open.
Deliverable: `v0.1.0-p0` and a short findings section here.

### P1: Protocol + app pipe servers
1. `docs/PROTOCOL.md`, `ControlsHub.Protocol` types, tests (round trip, unknown key rejected, clamping).
2. Alien Controls: `HubServer` (NamedPipeServerStream, async, one client), maps keys to existing commands, `quiet` flag suppresses its pill / sound. Sensors only while watched.
3. Dolby Controls: same.
4. DualSense Controls (Rust): `legend/hub.rs`, a pipe server thread (windows-sys, overlapped IO, blocks on the pipe, no timers), state pushed from the places that already announce changes (`mode::announce`, `events.rs` battery, settings saves). `quiet` suppresses pill + chime.
5. Broker: reconnect on pipe broken (wait on the app starting, use a pipe wait with backoff 1 s to 30 s, never a tight loop).
Deliverable: three app builds + broker console mode that prints live state.

### P2: Widget UI
1. Legend resource dictionary for XAML (colors, mono small caps, tick bars, cards, segmented buttons) matching `alien-control/src/AlienControl/Themes/Legend.xaml`.
2. Header tabs with L1 / R1 keycaps, glance strip (all four states), game row with GAME PROFILE chip.
3. ALIEN, DOLBY, DUALSENSE pages exactly as the mockups `design/mockups/hub-*.png`.
4. Focus model: white 2 px focus ring, D pad grid navigation, Cross / Circle / Square / Triangle actions, footer with Legend prompts.
5. Not running state per tab: Start button (broker launches the app's exe from a path the app registers on its first hub hello, stored in broker settings).
6. Open <app> button (Options) sends `open`.
Deliverable: `v0.2.0-p2`.

### P3: DualSense integration
1. DualSense Controls setting "PS button opens Game Bar" (on by default, Settings section, Advanced window): short press sends Win+G, hidden from the game.
2. Nav stream to the broker while the overlay is open (reuse `virt/gamebar.rs` open detection; broker also knows from the widget being visible).
3. Hidden Xbox pad on demand while the overlay is open, every mode, unplugged on close (extend `to_gamebar` path; keep "one hidden Xbox pad" rule).
4. Native mode: HidHide cloak while open, uncloak on close and on any crash path (also on app exit and in `prepare()` at start as a safety net).
5. Test matrix: Native / DualSense / Xbox x USB / Bluetooth x borderless / fullscreen.
Deliverable: `v0.3.0-p3` (DualSense Controls + hub).

### P4: Game profiles + toasts
1. Game watcher: foreground hook, resolve exe path, ignore non games (heuristic: fullscreen or borderless window covering the monitor, or exe already has a profile; never act on explorer, browsers, the hub, the three apps).
2. Game name: Steam `appmanifest_*.acf` name for exes under a Steam library, Xbox / MS Store package DisplayName, else exe version info ProductName or FileDescription, else window title, else file name. Renamable in the save sheet; the rename is stored.
3. Save sheet in the widget (Triangle), profiles.json (keyed by full exe path, includes display name, enabled rows, values, restore flag).
4. Apply on focus (debounced 1.5 s so alt tab flicker does nothing), restore on process exit (wait on the process handle, no polling), snapshot of what was there before.
5. Toasts: Pill (Win32 layered window, Legend, chips) and Classic (AppNotificationBuilder: title = game name, body = changed items). Setting chooses; default Pill. Respect Windows Do Not Disturb and fullscreen exclusive (Pill cannot draw over exclusive; Classic is queued by Windows).
6. Quiet mode end to end: no app toasts when the hub applies.
Deliverable: `v0.4.0-p4`.

### P5: Polish
1. Pinned mini widget "Controls strip" (second widget in the package): preset, CPU / GPU temp, controller battery; click through when pinned.
2. Fluent theme option for the widget (follows the apps' theme setting if they report it).
3. Reduce motion, interface size, first run tips.
4. README with screenshots, install guide, Security and Privacy section, AI disclosure (same pattern as the other repos).
Deliverable: `v1.0.0-beta1`.

## Roadmap (after 1.0)

### R1: Performance overlay (SteamOS / MangoHud style)
Goal: a toggle (quick tile in the widget + hotkey) for an in game stats overlay like SteamOS's performance overlay, levels 0 to 4:
* 1: FPS
* 2: FPS, frametime graph
* 3: + CPU % and temp, GPU % and temp, RAM, VRAM
* 4: + CPU / GPU / total power draw (W), clocks, battery W and time left, 1% low

Sources (all read only, no injection by us):
* FPS, frametime, 1% low: PresentMon style ETW consumer (Microsoft-Windows-DxgKrnl present events) in the broker, session started only while the overlay is on.
* GPU: NVML (`nvml.dll` from System32 only), already used by Alien Controls.
* CPU power: Alien Controls already reads `\EsifDeviceInformation(*)\RAPL Power`; get it over the pipe instead of a second reader. CPU temp / fans from Alien Controls too.
* CPU %, RAM: PDH or GetSystemTimes / GlobalMemoryStatusEx.
* Battery W: battery API (Alien Controls has it).

Drawing, two backends, user picks:
* **RTSS** (if installed): write our text and graphs into an RTSS OSD slot through its shared memory (`RTSSSharedMemoryV2`), the way HWiNFO does. Works over fullscreen exclusive; RTSS does the drawing.
* **Native**: click through, topmost, layered window (borderless and windowed games only). No injection, safe with anti cheat.
MangoHud's layout is the visual reference; we do not ship MangoHud. Cost rule: off = zero (no ETW session, no NVML calls); on = 2 Hz sensors, frametime graph from the ETW stream.

### R2: other ideas (not decided)
* Controller battery and preset in the Windows taskbar flyout.
* Quick record / screenshot shortcuts on Create button (Game Bar already does this; only if Amine wants it).

## Design tokens (Legend)

Window #090A0C, cards #0E1013, borders #1C2127 / #16191E, text #F2F6F9 / #A9B4BF / #6E7A86, cyan #00E5FF, purple #7C4DFF, orange #FF8A3D, green #40DC78. Thin cyan to purple top edge, faint hex pattern in the header, mono small caps labels with wide letter spacing, segmented tick bars. Hover fade 150 ms, press shrink 90 ms, selected underline 220 ms. Focus ring white 2 px, radius 7.

## Status

* 2026-10-23: concept, mockups and decisions signed off (see Decisions). Repo created. Nothing built yet. Next: P0 spike.
