# Device Validation — Train Your Monster (First Playable APK)

Status: **PENDING — requires a physical Android device + a human.**

The APK is built (`Build/Android/TrainYourMonster.apk`, ~34.8 MB, IL2CPP/ARM64,
min SDK 24, portrait, debug-signed). On-device validation cannot be performed by
the automated agent: it needs real hardware, a display, and human touch input.

## How to validate

1. `adb install -r "E:\TrainYourMonster\Build\Android\TrainYourMonster.apk"`
2. Work through **`MOBILE_QA_CHECKLIST.md`** (launch/orientation, portrait layout,
   touch targets, battle readability, text scaling, mid-range performance,
   full-loop stability).
3. Capture: device model / Android version / RAM · `adb logcat -s Unity` during
   one loop · rough fps + battle length · a screenshot of each of the 4 screens.

## Results (fill in on device)

| Area | Result | Notes |
|---|---|---|
| Install + launch | ☐ | |
| Portrait, no rotation | ☐ | |
| All 4 screens on-screen | ☐ | |
| Touch targets respond | ☐ | |
| Battle readable (HP/damage/deaths) | ☐ | |
| Text scaling (Large font) | ☐ | |
| Performance ≥ 30 fps | ☐ | |
| 5 battles stable, no crash | ☐ | |

## Triage
Fix now: crash · soft-lock · off-screen/unclickable button · unreadable battle ·
portrait failure. Defer (log only): cosmetic/alignment/animation polish.
