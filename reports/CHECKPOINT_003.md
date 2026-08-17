# CHECKPOINT 003 — First Playable Built + Phase 2 Android Prep

Date: 2026-08-17. Follows Checkpoint 002 (initiative fix). Covers the first-
playable build and all Phase 2 Android preparation. **The Android APK is NOT
built — blocked by the Android module not being detected** (details below).

## What was achieved

### First playable (code-complete + Windows build)
- `Meta/` layer: `GameFlow`, `GameSession`, `MatchRunner`, `GameController`,
  `BattlePlayback` (pure C#, fully edit-mode tested).
- `Battle/` layer: `BattleReplayView` + `UnitView` (code-built placeholder
  visuals; replays the event log). Core sim now emits per-unit **Spawn events**
  so the log is self-contained for replay.
- `App/` layer: `UIFactory` + `GameBootstrap` (code-built Menu / Team-select /
  Battle / Result); `FirstPlayableSceneBuilder` (scene + Windows build).
- Content generated: 12 species + 10 skills in `Resources/`.
- **Windows player built** (`Build/Windows/`) — sanity path for verification.

### Phase 2 Android preparation
- **Desktop-only dependency removed:** `SpeciesDatabase.LoadBalance()` now loads
  `balance.json` from **Resources** (Android-safe); a Resources copy was added.
  The old `File.ReadAllText(StreamingAssets)` would crash on Android.
- **Touch UI confirmed:** legacy Input Manager (`activeInputHandler:0`, no
  new-Input-System package) + `StandaloneInputModule` process taps on device.
- **`AndroidBuilder.cs`:** configures portrait, package `com.trainyourmonster.game`,
  min SDK 24, IL2CPP/ARM64, debug-keystore signing, and builds a **development
  APK**; self-detects the missing module and reports a precise blocker.
- **Docs:** `ANDROID_BUILD_INSTRUCTIONS.md`, `MOBILE_QA_CHECKLIST.md`.

## Tests

**15 / 15 EditMode pass** after all changes (7 gate + 3 flow + 2 playback + 3
controller). 0 compile errors, 0 warnings. Balance unchanged (`balance.json`).

## Metrics

- Assemblies: 6 (Core, Data, Meta, Battle, App, + editor).
- Windows build: succeeded (~97 MB).
- Android APK: **not produced** (blocker).

## Blocker (why no APK)

Task requested a signed development APK "now that Android Build Support is
installed." It is **not** installed/detected for Editor `6000.5.8f1`:

- `BuildPipeline.IsBuildTargetSupported(Android)` → **false** (Unity's own verdict;
  build script exited at `AndroidBuilder.cs:53`).
- Hub `modules.json`: `android isInstalled=None, selected=False`.
- No `…/PlaybackEngines/AndroidPlayer/` folder; no bundled SDK/NDK/JDK.

Likely causes: the module install did not complete, was queued but not applied,
or targeted a different Editor version. **Resolution:** in Unity Hub → Installs →
`6000.5.8f1` gear → **Add modules** → confirm **Android Build Support** + **Android
SDK & NDK Tools** + **OpenJDK** show as *installed* (not just checked); let the
download finish; verify `…/PlaybackEngines/AndroidPlayer/` exists. Then re-run
`MTA → Build Android APK`. Everything else is ready — it should build in one step.

## Remaining open (unchanged, out of scope here)

- Balance role/duration tuning (amber-frozen, post-playable pass).
- Human visual verification of the playable (Windows + Android).
- Phase 3 device validation (needs a device + the APK).
