# Android Release Report — Train Your Monster (First Playable)

Date: 2026-08-17. Status: **Android APK built successfully.** Development
(sideload) build of the first-playable. This documents the environment, build,
verification, and the path to a public release.

## Build artifact

| Field | Value |
|---|---|
| **APK path** | `E:\TrainYourMonster\Build\Android\TrainYourMonster.apk` |
| **APK size** | **36,527,654 bytes (~34.8 MB)** |
| **Build timestamp** | 2026-08-17 19:37 |
| Application id | `com.trainyourmonster.game` |
| Product name | Train Your Monster |
| Scripting backend | IL2CPP |
| Target architecture | ARM64 |
| Min SDK | 24 (Android 7.0) · Target SDK: Auto |
| Orientation | Portrait (locked) |
| Signing | Android **debug** keystore (development build) |
| Unity | 6000.5.8f1 · Gradle 9.1.0 / AGP 9.0.0 |

## Environment (installed automatically this session)

Android Build Support was not installed at start. Installed headlessly via the
Unity Hub CLI (`install-modules … -m android --childModules`, run detached).
Verified present:

| Component | Path (under `…\6000.5.8f1\Editor\Data\PlaybackEngines\AndroidPlayer\`) |
|---|---|
| AndroidPlayer engine | `\` (present) |
| SDK | `\SDK` (platforms 34/36/37, build-tools 35/36, platform-tools/adb, cmdline-tools) |
| NDK | `\NDK` (r27c) |
| OpenJDK | `\OpenJDK` (17) |

`IsBuildTargetSupported(Android)` = **true**.

## Build pipeline

- `Assets/Scripts/App/Editor/AndroidBuilder.cs` — `MTA → Build Android APK`
  (headless-invokable). Configures portrait + package + SDK + IL2CPP/ARM64 +
  debug signing, switches target, then `BuildPipeline.BuildPlayer` → APK.
- Android-safe runtime: `SpeciesDatabase.LoadBalance()` reads `balance.json` from
  **Resources** (StreamingAssets is not File-readable on Android).
- Touch: legacy Input Manager + `StandaloneInputModule` (uGUI buttons process taps).

## Verification

- EditMode tests: **15 / 15 pass**, 0 compile errors (Android active target).
- `balance.json` unchanged; no gameplay/design changes.
- APK present on disk, 34.8 MB.

## Install & smoke test (human, on device)

```
adb install -r "E:\TrainYourMonster\Build\Android\TrainYourMonster.apk"
```
Then the 7-step loop: menu → PLAY → pick 3 → START BATTLE → watch → VICTORY/DEFEAT
→ PLAY AGAIN / MENU. Full checklist in `MOBILE_QA_CHECKLIST.md`; record results in
`DEVICE_VALIDATION.md`.

## Remaining work before a PUBLIC (Play Store) release

1. **Device validation** — run on a mid-range Android; confirm portrait layout,
   touch targets, battle readability, text scaling, ≥30 fps, 5-battle stability.
2. **Release signing** — create a real upload keystore; build a signed **AAB**
   (Play Store requires AAB, not a debug APK).
3. **Play Console setup** — app listing, privacy policy, content rating, target-SDK
   compliance, store assets (icon, screenshots, feature graphic).
4. **Balance pass** — the amber-frozen role imbalance (2–93%) and short duration
   (~18 s median) should be tuned before public exposure (post-first-playable).
5. **Art/audio pass** — replace placeholder quads with the chibi asset pack;
   add SFX (Build Phase 4).
6. **Content** — training, leveling, save/load, career/leagues, capture (Build
   Phases 2–3); none are in this first-playable build.
7. **App icon + splash**, and an app name/branding pass.

## Known limitations (this build)

Development/debug-signed · ARM64 only · placeholder visuals, no audio ·
first-playable scope (no progression/save/career) · not device-validated · balance
untuned.
