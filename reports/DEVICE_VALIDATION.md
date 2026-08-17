# Device Validation — Train Your Monster (First Playable)

Date: 2026-08-17. **APK installed and launched successfully on a real device.**
No crash. Logic/engine startup verified from logcat; on-screen visual + touch QA
still needs a human (logs prove the engine + graphics initialized, not the pixels).

## Device

| Field | Value |
|---|---|
| Model | **Samsung SM-S731B** (Galaxy S24 FE) |
| Manufacturer | samsung |
| Android version | **16 (API/SDK 36)** |
| ABI | arm64-v8a (matches the APK's ARM64 build) |
| adb serial | RRCY900K2TH |
| Screen | 1080 × 2340, portrait, notch + rounded corners |

## APK

| Field | Value |
|---|---|
| File | `E:\TrainYourMonster\Build\Android\TrainYourMonster.apk` |
| Package | `com.trainyourmonster.game` |
| Size | 36,527,654 bytes (~34.8 MB) |
| Build | IL2CPP / ARM64, min SDK 24, portrait, debug-signed dev build |

## Install

**SUCCESS.** `adb install -r` → `Performing Streamed Install / Success`.
`pm list packages` confirms `com.trainyourmonster.game` present.

## Launch

**SUCCESS.** Launched via `adb shell monkey -p com.trainyourmonster.game 1`
(`Events injected: 1`). Activity `com.unity3d.player.UnityPlayerGameActivity`
started; Unity engine came up cleanly (from logcat, pid 14991):

```
UnityApplication::CreateInstance
GameActivity Package Version '4.4.0'
Starting Game Loop
APP_CMD_START → onResume → APP_CMD_RESUME → APP_CMD_INIT_WINDOW
MemoryManager: Using 'Dynamic Heap' Allocator
[Vulkan init] extensions: count=14        (graphics device: Vulkan)
```

Window + surface created in portrait; focus moved to the app. No FATAL exception,
no native crash (no `Fatal signal` / tombstone for the app pid), no missing game
resource.

## Runtime errors / observations (60 s logcat)

1. **Benign — `E/Unity: java.lang.ClassNotFoundException:
   com.google.android.play.core.assetpacks.AssetPackManager`.** Unity probes for
   Google Play Asset Delivery; the Play Core library isn't bundled (a plain
   sideload APK doesn't need it). Non-fatal — the app continues. Not a defect.
2. **Process reaped after ~28 s as a *cached* (backgrounded) app**, not a crash:
   logcat shows `APP_CMD_LOST_FOCUS` ~2.5 s after resume, then
   `ActivityManager: Process com.trainyourmonster.game (pid 14991) has died: cch
   CRE`. `cch` = cached; the OS reclaimed a backgrounded process. Expected in an
   unattended adb session where the app loses foreground focus (USB notification /
   screen). With a user holding the phone on the app, it stays running.
3. No `NullReferenceException`, no `Resources.Load` failure, no
   `LoadFromResources`/`balance.json` error — the data layer loaded (no errors on
   the Resources-based balance load path added for Android).

## What is confirmed vs still needs a human

- **Confirmed by logs:** install, activity launch, Unity engine + Vulkan graphics
  init, portrait window, no crash/exception, clean data-load path.
- **Still needs a human eye** (logs can't show pixels): main menu renders with
  PLAY/QUIT on-screen; team-select shows 12 tiles + START; a battle plays (HP
  bars/damage/deaths); result banner; touch targets respond; text legible;
  ≥30 fps; 5-battle stability. Run `MOBILE_QA_CHECKLIST.md` while holding the
  phone and record any critical issues here.

## Verdict

**Install + launch: PASS.** The APK runs on Android 16 (S24 FE) with no crash and
a clean engine/graphics startup. Visual/interaction validation is the remaining
step (human, on-device). No code changed, no rebuild — existing APK only.
