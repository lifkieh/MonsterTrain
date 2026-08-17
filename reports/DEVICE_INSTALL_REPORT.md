# Device Install Report — Train Your Monster APK

Date: 2026-08-17. Goal: install + launch the APK on a connected Android device.
**Result: BLOCKED — no Android device detected by adb.** Nothing was installed
(no device to install to). No game code changed, no rebuild.

## Environment

| Item | Value |
|---|---|
| adb | `…\AndroidPlayer\SDK\platform-tools\adb.exe` — v1.0.41 (36.0.0-13206524) — OK |
| adb server | started OK |
| **Devices** | **none** (`adb devices` empty across ~3.4 min of polling) |

## APK (present, not installed)

| Field | Value |
|---|---|
| Path | `E:\TrainYourMonster\Build\Android\TrainYourMonster.apk` |
| Size | 36,527,654 bytes (~34.8 MB) |
| Built | 2026-08-17 19:37 |
| Package | `com.trainyourmonster.game` |
| Backend / arch | IL2CPP / ARM64 · min SDK 24 · portrait · debug-signed |

## Outcome (per requested fields)

| Field | Result |
|---|---|
| Device model | **N/A — no device detected** |
| Android version | N/A |
| APK version | `com.trainyourmonster.game` dev build, ~34.8 MB (above) |
| Install success/failure | **not attempted** (no device) |
| Launch success/failure | **not attempted** |
| Runtime errors | N/A (never installed/launched) |

## Blocker

`adb devices` returned an empty list for the entire retry window (~3.4 min, two
polling rounds). The state was never even "unauthorized" — the phone is not
reaching adb at all, so it is **not physically connected in a debuggable USB
mode**, not merely awaiting the RSA popup.

## Exact human action required

On the phone:
1. **Connect it to the PC with a USB data cable** (some cables are charge-only —
   use one that does data).
2. Settings → About phone → tap **Build number** 7× → unlocks **Developer options**.
3. Settings → System → Developer options → enable **USB debugging**.
4. Pull down the USB notification → set USB mode to **File Transfer (MTP)**, not
   "Charging only".
5. When **"Allow USB debugging?"** appears, tap **Allow** (tick "Always allow from
   this computer").

Then verify on the PC: `adb devices` should show one line ending in `device`
(not `unauthorized`, not `offline`). Once it does, re-run this task and the
install + launch + logcat proceed automatically:
```
adb install -r "E:\TrainYourMonster\Build\Android\TrainYourMonster.apk"
adb shell monkey -p com.trainyourmonster.game 1
adb logcat
```

## Notes

- If the device shows **unauthorized**: the RSA popup wasn't accepted — tap Allow.
- If it shows **offline**: unplug/replug USB and re-accept.
- If it still doesn't appear at all: try a different USB cable/port, or install the
  phone vendor's USB driver on Windows.
