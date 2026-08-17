# Device Validation — Train Your Monster

Date: 2026-08-18 · APK `Build/Android/TrainYourMonster.apk` (35.0 MB)
Package `com.trainyourmonster.game` · Version **0.1.0** (versionCode 1)

## Result: PASS — installed, launched, ran 60 s with no crash

Device: **Samsung SM-S731B** (serial RRCY900K2TH) · Android **16** (SDK 36) · **arm64-v8a**.

## Steps performed
1. **adb available** — YES. Unity-bundled `platform-tools\adb.exe`, ADB 1.0.41 (36.0.0-13206524). (Not on PATH; full path used.)
2. **Device detected** — `adb devices`: `RRCY900K2TH  device` (authorized). (First poll found none; device connected on retry.)
3. **Install** — `adb install -r -g` → **Success** (streamed install). Runtime package: versionName `0.1.0`, versionCode `1`, targetSdk 36.
4. **Launch** — resolved `com.trainyourmonster.game/com.unity3d.player.UnityPlayerGameActivity`, `am start -W`:
   - Status: `ok` · LaunchState: `COLD`
   - **TotalTime: 323 ms** · WaitTime: 327 ms (OS time-to-first-frame).
   - Unity native game loop began ~200 ms into process start; surface up ~50 ms later.
5. **Logcat** — 60 s captured (~20,400 lines). App stayed alive the whole window.
6. **Post-run state** — pid unchanged (29523), `mCurrentFocus` = the game's activity → **foreground, running, not crashed**.

## Startup / version (launch success)
- **Package version:** 0.1.0 (code 1).
- **Startup time:** 323 ms cold (OS-reported); interactive within ~1 s.
- **Rendering:** GameActivity surface + `BLASTBufferQueue` created, SwappyDisplayManager frame pacing active. No GL/Vulkan/shader errors. `libgame.so` / `libmain.so` loaded OK by nativeloader.

## Crash / exception / ANR scan
- **FATAL / AndroidRuntime:** none.
- **ANR / "not responding":** none.
- **SIGSEGV / tombstone / abort:** none.
- App process (pid 29523) survived the full 60 s and remained foreground.

### Non-fatal log noise (app pid, benign — no action required)
| Line | Meaning | Impact |
|------|---------|--------|
| `E/Unity ClassNotFoundException … AssetPackManager` | Play Asset Delivery class absent (app bundles no asset packs) | none — app doesn't use it; same as prior sessions |
| `E/SwappyDisplayManager … couldn't find "libgame.so"` | Swappy frame-pacer probing via a class loader without the native path (the lib is already loaded elsewhere) | none — cosmetic probe log |
| `E/ashmem Pinning is deprecated since Android Q` | Unity internal ashmem usage | none |
| `E/System Uncaught exception thrown by finalizer: Failed to close dex file` | ART GC finalizer race closing an in-memory dex | none — non-fatal, common on ART |

The `E/ActivityManager` broadcast exceptions in the buffer come from Samsung
system-server (pid 1404, `CloBigDataManager`), not from this app.

## Notes
- APK is debug-signed (development build); fine for sideload validation, not Play Store.
- Only functional/runtime validation here; on-screen **visual** QA (layout, colors,
  touch targets across the new Career/Daily/Settings screens) still wants a human eye.

## Verdict
The latest release-candidate APK installs cleanly, launches cold in 323 ms, and
runs stable for 60 s with no crash, ANR, or fatal error on Android 16 / arm64.
Only known-benign log noise. **On-device runtime validation: PASS.**
