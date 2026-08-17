# Android Build Support — Full Diagnostic

Date: 2026-08-17. Project: `E:\TrainYourMonster`. Method: filesystem + Unity Hub
config + Windows registry (EditorPrefs) + Unity command-line verification. No
manual folder inspection required.

## VERDICT: **B — Missing module**

Android Build Support is **not installed** for the only Unity editor on this
machine. It is **not** a wrong-version, corruption, or path/config problem. An
external Android SDK exists (`E:\sdk`) but that alone does not enable Unity
Android builds — Unity's AndroidPlayer engine + NDK + OpenJDK are required and are
absent.

---

## 1. Project editor version

`ProjectSettings/ProjectVersion.txt` → **`6000.5.8f1`** (rev `5cb7df797b7d`).

## 2. Unity installations on this machine

| Location | Found |
|---|---|
| `C:\Program Files\Unity\Hub\Editor\` | **`6000.5.8f1` only** |
| `C:\Program Files (x86)\Unity\Hub\Editor\` | none |
| Standalone `C:\Program Files\Unity\Editor\` | none |
| Hub secondary install path | not set (`secondaryInstallPath.json` empty) |

→ **Exactly one editor installed**, and it is the project's version. There is no
other version the Android module could have gone to.

## 3–4. Android components for `6000.5.8f1` (exact paths)

Base: `C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Data\PlaybackEngines\`

| Component | Expected path | Status |
|---|---|---|
| AndroidPlayer engine | `…\PlaybackEngines\AndroidPlayer\` | **MISSING** |
| Android SDK (bundled) | `…\AndroidPlayer\SDK\` | **MISSING** |
| Android NDK (bundled) | `…\AndroidPlayer\NDK\` | **MISSING** |
| OpenJDK (bundled) | `…\AndroidPlayer\OpenJDK\` | **MISSING** |
| `adb` | `…\AndroidPlayer\SDK\platform-tools\adb.exe` | **MISSING** |

PlaybackEngines actually present: **`WebGLSupport`**, **`windowsstandalonesupport`**
only.

Machine-wide search for **any** `AndroidPlayer` directory on `C:` and `E:` →
**none found.**

## 5. Unity command-line verification

Ran `-executeMethod` with a read-only diagnostic:

```
ANDROID_DIAG| IsBuildTargetSupported(Android)=False
ANDROID_DIAG| activeBuildTarget=StandaloneWindows64
ANDROID_DIAG| supported=StandaloneWindows
ANDROID_DIAG| supported=StandaloneWindows64
ANDROID_DIAG| supported=WebGL
```

→ Unity's own verdict: **Android is NOT a supported build target.** (Confirms the
earlier `AndroidBuilder` blocker.)

## 6. WHY it is missing

- **Hub module registry** (`%APPDATA%\UnityHub\…` + editor `modules.json`):
  `android isInstalled=None, selected=False`; likewise its children
  `android-sdk-ndk-tools`, `android-ndk-r27c`, `android-open-jdk-17.0.18+8`,
  `android-sdk-platform-tools-36.0.0`, `android-sdk-platforms-34/36` — all
  `isInstalled=None`.
- **No paused/interrupted download** (`paused-downloads.json` → `{"downloads":[]}`)
  — so it is not a stuck mid-install.
- **No Android EditorPrefs** in the registry (`HKCU\Software\Unity Technologies\
  Unity Editor 5.x`) — Unity was never configured for Android (SDK/NDK/JDK paths
  unset), consistent with the module never being installed.
- **Wrong-version ruled out:** only one editor exists.
- **Corruption ruled out:** nothing is partially present — the AndroidPlayer
  folder is cleanly absent, no orphan files, no incomplete SDK/NDK under the
  engine.

**Most likely cause:** the module install was **not actually completed** for
`6000.5.8f1` (checked but not confirmed/continued, or cancelled). A probable
source of confusion: a **standalone Android SDK already exists at `E:\sdk`**
(platforms `android-34`/`android-36`, build-tools `35.0.0`/`36.1.0`,
platform-tools/adb) — likely from Android Studio. Having an SDK is **not** the
same as having Unity's Android Build Support; Unity still needs its AndroidPlayer
engine, an **NDK** (none on the machine — `E:\sdk\ndk` is empty), and an
**OpenJDK** (none; only a DBeaver-bundled JRE exists).

## 7. Auto-fix / build

Not applicable — the module is missing, so there is nothing to reconfigure and
nothing to build. No path fix can substitute for the absent AndroidPlayer engine.

## Remediation (the only unblock)

Install the module for `6000.5.8f1` via Unity Hub:

1. **Unity Hub → Installs →** the `6000.5.8f1` **gear → Add modules**.
2. Check **Android Build Support**, **Android SDK & NDK Tools**, **OpenJDK**
   (install **all three** — the machine has no NDK and no JDK; the SDK at
   `E:\sdk` lacks an NDK, so Unity's bundled NDK is needed regardless).
3. Let all downloads finish; confirm the Hub shows them **Installed**.
4. Verify `…\6000.5.8f1\Editor\Data\PlaybackEngines\AndroidPlayer\` now exists.
5. Re-run `MTA → Build Android APK` (or the headless `-executeMethod`).

Optional (not required): after install, Unity can be pointed at `E:\sdk` for the
SDK via Preferences → External Tools, but the bundled SDK/NDK/JDK work out of the
box — simplest to use them.

## Definitive diagnosis

**B — Missing module.** Android Build Support (AndroidPlayer + SDK + NDK + OpenJDK)
is not installed for editor `6000.5.8f1`, the project's only editor. Confirmed by
filesystem, Hub config, registry, and Unity's `IsBuildTargetSupported(Android)=
False`. Install the three sub-modules via Hub to unblock; everything else in the
project is already Android-ready.
