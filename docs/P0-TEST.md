# P0 spike: test checklist

Goal: prove the risky parts on the real laptop before any UI work. Tick each step and fill the results table at the end. If a step fails, write what you saw and move on; the broker log helps a lot (see "Where the logs are").

You need two zips from the CI run (or the v0.1.0-p0 prerelease):

* **ControlsHub-package**: `ControlsHub_0.1.0.0_x64.msix`, `ControlsHub-dev.cer`, `Dependencies\x64\`, `install.ps1`, `uninstall.ps1`, this file.
* **ControlsHub-tools**: `DummyApp\DummyApp.exe`, `SpikeTests\SpikeTests.exe`, `Broker\ControlsHub.Broker.exe` (an unpackaged copy for console mode).

Extract both somewhere simple, e.g. `C:\ControlsHub\package` and `C:\ControlsHub\tools`.

## 1. Install

1. Open **Windows PowerShell as administrator** (Start, type PowerShell, Run as administrator).
2. `cd C:\ControlsHub\package`
3. `powershell -ExecutionPolicy Bypass -File .\install.ps1`
   * It trusts the test certificate (LocalMachine, TrustedPeople) and installs the app with its framework packages.
   * Every CI build has a new certificate, so run install.ps1 again for each new build.
4. Expected: "Installed aminenrr.ControlsHub 0.1.0.0". "Controls Hub" also appears in Start.

## 2. Widget appears and pins

1. Press **Win+G**. Open the **Widgets** menu (top bar).
2. Find **Controls** in the list. Click it.
3. Expected: a dark panel, title `CONTROLS HUB · P0`, cyan to purple line on top, about 470 x 650, not resizable.
4. Click the pin icon on the widget. Close Game Bar (Win+G or Esc). Expected: the widget stays on screen pinned. Unpin it again.
5. The top right of the widget shows **BROKER** with a green dot once the broker is connected (the widget starts it on open). Grey **NO BROKER** means the broker did not start or did not connect: note it and check the log.

## 3. Push from an app, no polling

1. Run `C:\ControlsHub\tools\DummyApp\DummyApp.exe` (a normal terminal is fine).
2. Open Game Bar with the Controls widget visible. The LINK card should say `app: dummy connected` within a second or two (the broker retries 1 s, 2 s, 4 s ... up to 30 s, so if DummyApp was started late it can take up to 30 s).
3. In the DummyApp window press **1** to **5**. Expected: the big preset text changes at once (BATTERY amber, QUIET green, BALANCED cyan, PERFORMANCE red, CUSTOM violet) and "pushed HH:mm:ss.fff" updates. Note if you can see any delay.
4. Click a chip in the widget (e.g. QUIET). Expected: DummyApp prints `preset -> quiet (hub, quiet)` and the widget shows `set #N applied by the app`. This is the widget to broker to app round trip.
5. Close DummyApp. Expected: the widget shows NOT RUNNING and `app: not running`. Start it again: it reconnects by itself.

## 4. Nav without widget focus

1. Keep Game Bar open with the widget visible, but click somewhere else in Game Bar (another widget or the home bar) so the Controls widget does **not** have focus.
2. Click the DummyApp terminal window so it has keyboard focus (Game Bar may hide when you do; if it does, pin the widget first and use it pinned).
3. Press the arrows, **x** (cross), **o** (circle), **q** (L1), **e** (R1).
4. Expected: the NAV EVENTS list shows each press with a timestamp; left / right moves the white highlight across the chips; x applies the highlighted preset; q / e switch the TAB label; o clears the list.
5. DummyApp prints `nav ... (ignored: no client asked for nav)` when no widget is connected. That is correct: nav only flows while the widget is open.

## 5. Broker console mode

1. Run `C:\ControlsHub\tools\Broker\ControlsHub.Broker.exe --console` (a new console window opens).
2. With DummyApp running, press keys in DummyApp. Expected: lines like `state preset=performance (change 12)` and `nav cross down`.
3. The first line shows `working set` at start; every 50 state changes another `working set ... after 50 state changes` line appears. Hold a number key in DummyApp to get there quickly.
4. Ctrl+C quits.

## 6. Win+G from a desktop process over games

1. Start **MW4** in **borderless** mode, get into a menu or match.
2. In a terminal: `C:\ControlsHub\tools\SpikeTests\SpikeTests.exe wing` then switch to the game within 5 s (alt tab or click).
3. Expected: Game Bar opens over the game. Note yes / no.
4. Repeat with a game in **fullscreen exclusive** (any game that offers true fullscreen). `SpikeTests wing 8` gives 8 s if you need longer.
5. If nothing happens, try once from an elevated terminal (some games run elevated and block input from normal processes) and note which one worked.

## 7. HidHide timing with the DualSense

Only if HidHide is installed. SpikeTests never leaves the pad hidden: it unhides in a finally block and on Ctrl+C, and refuses to touch a device that is already on HidHide's hidden list.

1. Make sure DualSense Controls is running and its exe is on HidHide's **Applications** list (HidHide Configuration Client). Otherwise it would lose the pad too.
2. Elevated terminal: `C:\ControlsHub\tools\SpikeTests\SpikeTests.exe hidhide-list`. Copy the DualSense `deviceInstancePath` (starts with `HID\VID_054C&PID_0CE6` or similar).
3. With a game running that reads the DualSense natively: `SpikeTests.exe hidhide-hide "HID\VID_054C..."`.
   * Expected: the game loses the pad (prompts change or input stops), DualSense Controls still shows the controller and reacts.
   * Press Enter: the pad comes back. Note how long the game takes to pick it up again.
4. `SpikeTests.exe hidhide-time "HID\VID_054C..." 5` prints hide / unhide ms per round and the average.
5. If anything goes wrong: `SpikeTests.exe hidhide-unhide "HID\VID_054C..."`.

## 8. RAM

Task Manager, Details tab (add the "Memory (private working set)" column if you like):

1. `ControlsHub.Broker.exe` with Game Bar closed (idle): note the working set.
2. `ControlsHub.Widget.exe` while Game Bar shows the widget: note it.
3. Close Game Bar (widget not pinned): `ControlsHub.Widget.exe` should disappear or drop to suspended after a few seconds.

## 9. Sign in start

1. Sign out and back in (or reboot).
2. Task Manager: is `ControlsHub.Broker.exe` running before you open Game Bar? (StartupTask; Windows may also list it under Startup apps as "Controls Hub broker".)

## Where the logs are

The broker writes `broker.log`. When it runs packaged, Windows redirects the file into the package folder:

* Packaged: `%LOCALAPPDATA%\Packages\aminenrr.ControlsHub_<id>\LocalCache\Local\ControlsHub\broker.log`
* Console mode from the tools zip: `%LOCALAPPDATA%\ControlsHub\broker.log`

Send both if something fails.

## Uninstall

`powershell -ExecutionPolicy Bypass -File .\uninstall.ps1` (add `-RemoveCerts` in an admin PowerShell to also remove the test certificates).

## Results

| # | Check | Result (yes / no / value) | Notes |
|---|---|---|---|
| 1 | install.ps1 succeeded | | |
| 2a | "Controls" listed in the Game Bar widget menu | | |
| 2b | Widget opens, dark Legend look, right size | | |
| 2c | Pinning works | | |
| 2d | BROKER green dot after opening the widget | | |
| 3a | DummyApp connects (LINK card) | | seconds: |
| 3b | Preset change shows at once on keypress | | any visible delay? |
| 3c | Chip click applies in DummyApp (round trip) | | |
| 3d | Reconnects after DummyApp restart | | |
| 4a | Nav events listed while widget not focused | | |
| 4b | Highlight moves, x applies, q / e switch tab | | |
| 5 | Broker console prints state and nav | | |
| 6a | SpikeTests wing opens Game Bar over MW4 borderless | | elevated needed? |
| 6b | SpikeTests wing over a fullscreen exclusive game | | game: |
| 7a | HidHide hide: game loses pad, DualSense Controls keeps it | | |
| 7b | hidhide-time hide avg / unhide avg (ms) | | |
| 7c | Game picks the pad up again after unhide | | seconds: |
| 8a | Broker working set idle (MB) | | target under 15 MB |
| 8b | Widget working set open (MB) | | |
| 8c | Widget process gone after closing Game Bar | | |
| 9 | Broker running after sign in (StartupTask) | | |
