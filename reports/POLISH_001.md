# Polish 001 — Cinematic Replay Feel

Date: 2026-08-18. Presentation-only polish of the Phase J cinematic replay. No
new systems. Deterministic director untouched → winner + battle hash unchanged.

## Changes (view layer only)
- **Combos now respect the playback speed buttons.** The combo coroutine used
  real-time waits (`WaitForSecondsRealtime`), so 2×/4× fast-forward sped the
  sim clock but left combos playing at 1× — the replay felt inconsistent and long
  ultimate chains dragged. Combo step, hit-stop, and the dodge beat now scale by
  `1 / speedMultiplier` (clamped 0.5×–4×), read live each hit, so fast-forward
  actually speeds the choreography.
- **Fighters approach on entry.** Added an intro: the two active fighters rush in
  from their screen edges (`IntroApproach` → `UnitView.EnterFrom`) instead of
  popping in place — the fighting-game "square up" beat.

## Files changed
- `Assets/Scripts/Battle/BattleReplayView.cs` — speed-scaled combo pacing + `IntroApproach`/`ActiveView`.

## Verification
- Full EditMode suite: **58 / 58 pass** (unchanged; director + all pure-C# logic
  untouched, so determinism/hash/outcome tests stay green).
- Android APK: built.
- `BattleCinematicDirector` and every gameplay/data file untouched — this is
  strictly rendering pacing + an entry animation.

## Constraints honored
Presentation only · no combat formula / `balance.json` / simulator changes ·
same seed ⇒ identical winner + battle hash · no functionality removed.

## Still open
On-device visual QA of the fight feel (view animation is not unit-testable).
