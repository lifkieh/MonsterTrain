# CHECKPOINT 004 — Android APK Build Successful

Date: 2026-08-17. Follows Checkpoint 003. **First playable now builds to a signed
Android APK.** Android Build Support was installed automatically via the Unity Hub
CLI, and the APK built end-to-end.

## What was achieved

### Android environment (installed automatically)
- Located Unity Hub at `D:\unity\Unity Hub\Unity Hub.exe` (custom drive).
- Installed the Android modules headlessly via
  `Unity Hub.exe -- --headless install-modules --version 6000.5.8f1 -m android
  --childModules` (run detached so it survived session boundaries).
- Result: **"All Tasks Completed Successfully."** All components present under
  `…\6000.5.8f1\Editor\Data\PlaybackEngines\AndroidPlayer\`:
  **AndroidPlayer**, **SDK** (platforms 34/36/37, build-tools, platform-tools/adb,
  cmdline-tools), **NDK** (r27c), **OpenJDK** (17).

### Android build
- `IsBuildTargetSupported(Android)` → **true** (Unity switched to the Android
  target and reimported assets).
- `MTA → Build Android APK` (`AndroidBuilder.BuildApk`, IL2CPP/ARM64, portrait,
  package `com.trainyourmonster.game`, min SDK 24, development build, debug
  keystore) → **Succeeded**.

## APK

| Field | Value |
|---|---|
| Path | `E:\TrainYourMonster\Build\Android\TrainYourMonster.apk` |
| Size | **36,527,654 bytes (~34.8 MB)** |
| Built | 2026-08-17 19:37 |
| Backend / arch | IL2CPP / ARM64 |
| Min SDK | 24 (Android 7.0) |
| Signing | Android debug keystore (development build) |

## Release verification

- **EditMode tests: 15 / 15 pass** (on the Android active target), 0 compile errors.
- `balance.json` (StreamingAssets) **unchanged**.
- Only tracked config change: `ProjectSettings/ProjectSettings.asset` (Android
  player settings). APK/Build are git-ignored (not committed).

## Known limitations

- **Development build**, debug-signed — installs/sideloads fine, **not** Play-Store
  release-signed (needs a release keystore + AAB later).
- **Not yet validated on a physical device** — on-screen layout, touch, battle
  readability, and performance are unverified (needs hardware + a human; see
  `MOBILE_QA_CHECKLIST.md` / `DEVICE_VALIDATION.md`).
- Balance is amber-frozen (role imbalance, ~18 s median battles) — playable, not
  tuned.
- First-playable scope only: menu → pick 3 → auto-battle → win/lose → repeat.
  Placeholder visuals, no audio, no save/training/career/capture.
- ARM64 only (no ARMv7/x86).

## Next

Install on a mid-range device (`adb install -r`), run the 7-step loop, fill in
`DEVICE_VALIDATION.md`. Then: balance pass, release signing + AAB, art/audio.
