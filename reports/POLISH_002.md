# Polish 002 — Combo Timing & Camera Feel

Date: 2026-08-18. Presentation-only tuning of the cinematic replay's combo rhythm
and camera. No new systems. Deterministic director untouched → winner + battle
hash unchanged (all determinism tests still green).

## Changes (view layer only — `BattleReplayView.cs`)
### Combo timing
- **Accelerating combos.** Per-hit spacing now ramps from 1.15× → 0.68× across the
  chain (`baseStep` 0.045–0.055 s), so hits speed up into the finish instead of a
  flat cadence.
- **Impact on the connecting hit.** Mid-combo hits get a light 0.02 s flow-freeze
  and a small shake; the *final* hit carries a heavier freeze (crit 0.085 s, ult
  0.10 s, else 0.055 s) plus the damage number, knockback, big shake and zoom-punch
  — the hit-stop now reads as impact, not uniform stutter.

### Camera
- **Wind-up → payoff.** Attacks set a gentle wind-up zoom (1.05, ult 1.16) at the
  start; the strong shake + zoom-punch now fire on the connecting hit (synced to
  impact) rather than at combo start.
- **Magnitude-scaled shake.** Shake duration scales with strength (0.18 s + mag·0.006),
  so heavy hits shake longer/harder and light hits stay snappy.
- **Zoom feel.** Punch clamped to ≤1.35×; zoom-target relaxes slightly slower
  (1.2/s) and eases in faster (5.5/s), so the punch lingers before settling.
- **Deeper finisher slow-mo.** 0.28×→**0.22×** time scale, held 1.3 s→**1.4 s**,
  finisher zoom 1.22→**1.24** for more drama on the killing blow.

Playback-speed scaling (0.5×–4×) preserved: all combo timing still divides by the
speed multiplier, so fast-forward stays consistent.

## Verification
- Full EditMode suite: **58 / 58 pass** (director + all pure-C# logic untouched).
- Android APK: built.
- `BattleCinematicDirector` and every gameplay/data file untouched.

## Constraints honored
Presentation only · no combat formula / `balance.json` / simulator changes ·
same seed ⇒ identical winner + battle hash · no functionality removed.
