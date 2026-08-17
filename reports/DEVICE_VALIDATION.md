# Device Validation — Train Your Monster

Date: 2026-08-18 · APK `Build/Android/TrainYourMonster.apk` (35.0 MB, built 2026-08-18 06:26)
Package `com.trainyourmonster.game` · Version 0.1.0

## Result: BLOCKED — no Android device connected

Install/launch could not proceed: `adb` sees no attached device.

## Steps performed
1. **adb available** — YES. `C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`, Android Debug Bridge 1.0.41 (36.0.0-13206524). (`adb` is not on PATH; the Unity-bundled SDK copy was used.)
2. **adb server** — `kill-server` + `start-server` OK.
3. **Device poll** — `adb devices` polled every 5 s for 90 s. Result every time:
   ```
   List of devices attached

   ```
   No device in any state (no `device`, no `unauthorized`, no `offline`).
4. **APK** — present and current (35.0 MB, dated 2026-08-18 06:26 — the Phase I release-candidate build).

## Diagnosis
The empty device list (not `unauthorized`, not `offline`) means the host is not
seeing any device at the USB/ADB layer. Most likely one of:
- No phone physically connected (or a charge-only USB cable / port).
- USB debugging disabled in Developer Options.
- Device not yet authorized for this host (would normally show as `unauthorized`
  once the cable is seen — so this points to no connection rather than auth).
- Missing/!working USB driver on the Windows host.

This is an environment blocker, not an APK or build problem: the APK exists and is
the latest green build, and adb itself is healthy.

## Remediation (to complete validation)
1. Connect the device by USB (data-capable cable).
2. On the device: Settings → Developer Options → enable **USB debugging**.
3. Accept the "Allow USB debugging?" RSA prompt on the device.
4. Confirm with:
   ```
   adb kill-server && adb start-server && adb devices
   ```
   The device should list as `device`. Then re-run install/launch:
   ```
   adb install -r -g E:\TrainYourMonster\Build\Android\TrainYourMonster.apk
   adb shell monkey -p com.trainyourmonster.game -c android.intent.category.LAUNCHER 1
   adb logcat -v time    # capture ~60 s; watch for FATAL/AndroidRuntime/ANR
   ```
   (Tip: type `! adb devices` in this session to run it yourself and drop the
   output into the conversation.)

## Not captured (blocked)
- Install result, launch, startup time, package version at runtime.
- 60 s logcat; crash / exception / ANR / missing-resource / rendering scan.

## Prior on-device evidence
An earlier session installed and launched a prior build on Samsung SM-S731B
(Android 16 / arm64): app launched, no crash, only a benign AssetPackManager
`ClassNotFoundException`. That validated the app runs on real hardware, but does
**not** cover this newer release-candidate APK — a fresh device pass is still needed.
