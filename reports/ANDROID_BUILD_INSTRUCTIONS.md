# Android Build & Install — Train Your Monster (First Playable)

Phase 2 target: a sideloadable APK of the first-playable. All code/config work is
done; **the one blocker is installing the Android module** (agent cannot install
Editor modules).

## One-time prerequisite (the blocker — human action)

The Editor `6000.5.8f1` has **no Android Build Support** installed. Add it:

1. **Unity Hub → Installs →** the `6000.5.8f1` gear **→ Add modules**.
2. Check **Android Build Support**, and its children **Android SDK & NDK Tools**
   and **OpenJDK**. Install.
3. Restart the Editor if it was open.

Verify: `…/Editor/Data/PlaybackEngines/AndroidPlayer/` and its `SDK`, `NDK`,
`OpenJDK` folders exist.

## Build the APK

**From the Editor:** menu **MTA → Build Android APK**. (It runs *Configure Android
Settings* first: portrait, package `com.trainyourmonster.game`, min SDK 24,
IL2CPP/ARM64, adds `FirstPlayable` scene.)

**Headless (same as the agent uses):**
```
"C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" ^
  -batchmode -quit -projectPath "E:\TrainYourMonster" ^
  -executeMethod MTA.App.EditorTools.AndroidBuilder.BuildApk ^
  -logFile build_apk.log
```
First IL2CPP build is slow (several minutes). Output:
`E:\TrainYourMonster\Build\Android\TrainYourMonster.apk`.

> Faster test build (not Play-Store compliant): switch the two lines in
> `AndroidBuilder.Configure()` to `ScriptingImplementation.Mono2x` +
> `AndroidArchitecture.ARMv7`. Builds quicker, no NDK compile, but won't run on
> arm64-only devices. IL2CPP/ARM64 is the default for exactly that reason.

## Install on a device

**Option A — adb (recommended):**
1. On the phone: Settings → About → tap Build number 7× to unlock Developer
   options → enable **USB debugging**.
2. Connect USB, accept the debugging prompt.
3. `adb install -r "E:\TrainYourMonster\Build\Android\TrainYourMonster.apk"`
4. Launch "Train Your Monster" from the app drawer.

**Option B — sideload:** copy the `.apk` to the phone, tap it, allow "Install
unknown apps" for your file manager, install.

## What to verify on device (the 7-step playable loop)

1. App opens in **portrait**, main menu shows **PLAY**.
2. **PLAY** → team select; **tap 3 monsters** (turn green, "3 / 3").
3. **START BATTLE** → battle plays (HP bars drain, damage numbers, deaths).
4. **VICTORY/DEFEAT** banner.
5. **PLAY AGAIN** → team select; **MENU** → main menu.
6. Touch works throughout (buttons respond to taps).
7. Repeat a few battles without a restart.

If any button is off-screen or unresponsive: the UI is code-built at 1080×1920
portrait reference (`UIFactory`); report which screen and it gets a layout fix
(KILL_CRITERIA S4 → plain list fallback).

## Notes

- Touch already works: project uses the legacy Input Manager
  (`activeInputHandler: 0`) + `StandaloneInputModule`, which processes touch as
  pointer input. No new-Input-System package involved.
- `balance.json` is loaded from **Resources** at runtime (Android-safe); the
  StreamingAssets copy is the editor source. They are identical now (balance is
  frozen); keep them in sync if you ever retune.
