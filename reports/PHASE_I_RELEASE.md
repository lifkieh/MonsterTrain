# Phase I — Release Prep

Date: 2026-08-17. Release-candidate polish: app icon, splash background, boot
loading screen, settings page (sound / FPS / quality), about + credits, and a
version display (v0.1.0). Deterministic sim + save compatibility preserved.

## Files changed
- **New** `Assets/Scripts/Tests/ReleaseTests.cs` (3 tests).
- Edited `App/Editor/AndroidBuilder.cs` — version name/code, procedural app icon,
  splash background color.
- Edited `Meta/SaveData.cs` (`targetFps`, `quality`),
  `Meta/GameFlow.cs` + `GameController.cs` (Settings + About phases),
  `App/GameBootstrap.cs` (loading screen, settings page, about/credits, version
  labels, display-settings apply on launch; menu SOUND folded into Settings).

## Tasks
1. **App icon** — `AndroidBuilder.MakeIcon` draws a 512px procedural monster mark
   (gradient field + body orb + eyes) assigned to all Android icon sizes. No art asset.
2. **Splash** — Unity splash background set to the app's dark blue (`0.08,0.09,0.12`).
3. **Loading screen** — a branded `LoadingPanel` shows on boot, auto-hidden after
   a short delay once UI is built.
4. **Credits** — About page credits (design/code Lifkie Lie, engine, sim core).
5. **Version display** — `v0.1.0` on the menu, settings, about, and loading screens
   (from `Application.version`, set by the builder's `bundleVersion`).
6. **Settings page** — `SETTINGS` menu entry: sound, frame rate, quality, about.
7. **FPS option** — toggles `Application.targetFrameRate` 30 ↔ 60; persisted.
8. **Quality option** — toggles Low ↔ High (`QualitySettings` lowest ↔ highest); persisted.
9. **About page** — version, package id, one-line pitch, credits, engine version.
10. **Build verification** — tests + APK build below.

Settings persist in `SaveData` and are applied on every launch. `balance.json` untouched.

## Tests
Full EditMode suite: **53 / 53 pass** (50 prior + 3 new): sane display defaults
(60 FPS / High), settings survive a JSON round-trip, and an old save missing the
new fields keeps its defaults (backward compatibility). Determinism/replay/save
tests still green.

## Known limitations
- Icon is procedural (functional placeholder); a hand-drawn store icon is a later art pass.
- Splash keeps the Unity logo (Personal license); background is branded.
- Quality is a 2-step Low/High switch, not per-effect tuning.
- On-device visual QA still needed.

## Constraints
Android primary · determinism preserved · save backward-compatible · no
functionality removed (menu SOUND relocated into Settings, mute still available).
