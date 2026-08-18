# Device Validation — Train Your Monster (Cinematic Replay)

Date: 2026-08-18 · APK `Build/Android/TrainYourMonster.apk` (48.3 MB, built 07:25 — Phase J + Polish 001)
Package `com.trainyourmonster.game` · Version **0.1.0** (versionCode 1)

## Result: PASS — cinematic battle installs, launches, renders, and runs crash-free

Device: **Samsung SM-S731B** (RRCY900K2TH) · Android **16** (SDK 36) · **arm64-v8a** · 1080×2340.

## Steps performed
1. **Install** — `adb install -r -g` → **Success** (streamed). Runtime version 0.1.0 (code 1).
2. **Launch** — `am start -W` on `UnityPlayerGameActivity`: Status ok, **COLD, TotalTime 508 ms**.
3. **UI render check (screenshots)**
   - **Main menu** — all 8 buttons (PLAY / CAREER / DAILY / CONTINUE / PROGRESS / COLLECTION / SETTINGS / QUIT) + `v0.1.0`, well spaced, no overlap.
   - **About page** — title, version, package id, credits (Design & Code: Lifkie Lie), engine version — all crisp and correctly laid out.
4. **Cinematic battle (the Phase J deliverable) — verified on-screen:**
   - **Procedural arena** rendered: gradient sky, parallax mountain silhouettes, ground + floor line.
   - **Fighting-game 1v1 staging** working: active fighters centered and full-size (**golem** vs **slime**), the reserve monsters (**host**, **golem**) parked behind, smaller and dimmed.
   - **Knockback / launch** animation captured mid-flight (a defeated fighter's card rotating away off the arena).
   - HP bars, nameplates, team-colored frames (blue player / red enemy) all rendering.
5. **Logcat scan (battle window ~07:29–07:30)** — the cinematic code path (combos, coroutines, arena, knockback) ran with:
   - **No FATAL / AndroidRuntime**, no ANR, no tombstone, no SIGSEGV.
   - **No C# exceptions** (no NullReferenceException etc.) from the cinematic system.
6. **Liveness** — app process stayed alive throughout (`pidof` unchanged); no crash.

## Startup / version
- Package version 0.1.0 (code 1). Cold start 508 ms.

## Non-fatal log noise (app pid, benign — unchanged from prior runs)
- `E/Unity ClassNotFoundException … AssetPackManager` (no asset packs used).
- `E/SwappyDisplayManager … couldn't find "libgame.so"` (frame-pacer probe; lib already loaded).
- `E/ashmem Pinning is deprecated`.
- `E/System Uncaught exception thrown by finalizer` (ART GC dex-close race).

None fatal; none from game code.

## Notes / caveats
- During the session the app was once sent to the home screen (backgrounded) between captures; the process stayed alive (not a crash). At teardown the device dropped off ADB (USB), so end-of-session focus/screenshot were not re-captured — after the crash-free battle window was already recorded.
- Debug-signed development APK (sideload validation, not Play Store).
- This confirms the cinematic replay renders and runs stably on hardware; full subjective "fight feel" tuning (combo timing, camera intensity) is still a human judgment pass.

## Verdict
The Phase J + Polish cinematic replay APK installs, launches cold in ~0.5 s, and
renders the fighting-game battle (arena, 1v1 staging, reserves, knockback) with
**no crash and no exceptions** on Android 16 / arm64. **On-device validation: PASS.**
