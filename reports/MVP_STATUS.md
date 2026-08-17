# MVP Status — Train Your Monster

Date: 2026-08-17 · Version 0.1.0 · Package `com.trainyourmonster.game`

## Snapshot
A deterministic monster-raising auto-battler for Android, built from a single
code-constructed scene. First-playable → MVP soft-launch candidate. All feature
work is additive and preserves the deterministic battle simulator, replay
determinism, and save compatibility (verified by the test suite every phase).

## Feature completeness (MVP scope)

| System | Status | Phase |
|--------|--------|-------|
| Deterministic battle sim + replay | Done | (pre) |
| Battle feel: attacks, VFX, camera, pacing | Done | A |
| Monster identity: color/icon/nameplate | Done | B |
| Meta progression: profile, JSON save, XP, levels, unlocks | Done | C |
| Audio: synth SFX + mute | Done | D |
| Collection & encyclopedia (filter/sort/seen) | Done | E |
| Training & leveling (detail screen, coin sink) | Done | F |
| Career mode (12 stages, 4 leagues, scaling, rewards) | Done | G |
| Daily rewards & retention (7-day streak, anti-cheat) | Done | H |
| Release prep (icon, splash, loading, settings, about, version) | Done | I |

## Project stats
- Scripts: 59 `.cs` (45 game + 14 test files).
- Assemblies: 8 asmdef (Core, Data, Meta, Battle, App, App.Editor, EditorTools, Tests).
- Tests: **53 EditMode tests, all passing.**
- Scenes: 1 (`FirstPlayable.unity`; all UI built in code).
- ScriptableObjects: 22 (12 species + 10 skills) + `balance.json`.
- Build: signed development APK, IL2CPP / ARM64, minSdk 24, portrait.

## Player loop
Launch → daily reward → menu → (Casual battle | Career ladder) → animated replay
→ result + rewards → collection/detail → train monsters → back to battle. Progress,
collection, leveling, career frontier, and daily streak all persist across sessions.

## Remaining before store submission (post-MVP)
- **On-device visual QA** — headless builds can't verify pixels; needs a human pass.
- **Release signing** — currently debug-signed; needs a real upload keystore + AAB.
- **Store art** — hand-drawn icon/feature graphic to replace the procedural icon.
- **Balance tuning** — role win-rate spread and ~18s battle duration remain amber
  (documented; `balance.json` intentionally frozen this run).

## Verdict
MVP feature set is complete and green. Ready for the on-device QA + store-packaging
pass; no code blockers outstanding.
