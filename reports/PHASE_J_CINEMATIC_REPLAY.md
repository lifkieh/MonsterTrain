# Phase J — Cinematic Fight Replay

Date: 2026-08-18. Reworks the battle **presentation** into a fighting-game style
cinematic replay — dashes, procedural combos, dodges, knockbacks, launches,
impact pauses, camera work, slow-mo finishers, and a parallax arena — **without
touching combat outcomes**.

## Determinism guarantee (the critical rule)
The cinematic layer is a **read-only consumer** of the simulator output. It never
writes back into the sim, so the winner and log hash are the simulator's, byte for
byte. All "randomness" (combo length, dodge chance) comes from a deterministic
xorshift seeded **entirely by `result.logHash`** — no `UnityEngine.Random`, no
wall-clock. Same seed → same log → same hash → identical choreography.

Architecture preserved: **Simulator → BattleLog → Replay → (new) Cinematic Director → View.**
`balance.json`, combat formulas, and `BattleSimulator` are untouched.

## Files
- **New** `Assets/Scripts/Meta/BattleCinematicDirector.cs` — deterministic choreography (`Choreography`, `ChoreoBeat`, `ChoreoMove`, `ChoreoCam`, `FinisherKind`).
- **New** `Assets/Scripts/Battle/BattleArena.cs` — procedural arena (System 8).
- **New** `Assets/Scripts/Tests/BattleCinematicTests.cs` — 5 determinism/invariance tests.
- Rewrote `Assets/Scripts/Battle/BattleReplayView.cs` — fighting-game staging + beat-driven choreography + camera + finishers + parallax.
- Extended `Assets/Scripts/Battle/UnitView.cs` — impulse (knockback/launch/dodge slide-in), reserve scaling/dim, staging moves.

## Systems delivered
1. **Cinematic Director** — converts each replay event to a `ChoreoBeat`: attack → dash + N-hit combo + knockback; crit → fast dash + long combo + heavy finisher + shake; skill → banner + sequence; ultimate → banner + launch + cinematic zoom; death → launch + slam + defeat pose.
2. **Fighting-game staging** — static lanes replaced. Front-most alive fighter is **active** (centered, full size); the rest wait **behind** (small, dimmed). When the active dies, the next **runs in** (slide-in via decaying impulse) → 1v1, winner stays, next challenger enters.
3. **Combo generation** — procedural chains: light 2–3, medium 3–5, crit 5–8, ultimate 8–15 hits, each with **0.03–0.10 s hit-stop** (the connecting hit carries the single, sim-accurate damage number).
4. **Dodge system** — presentation-only. Low-damage, non-crit hits get a **20–40 %** chance to show a sidestep/backstep before the hit lands. Zero gameplay effect (HP still resolves exactly as the sim says).
5. **Knockback** — hits push; crit = strong push; ultimate = **launch into air**; death = full knockout with rotation + sink.
6. **Camera** — normal medium; combo slight zoom; crit strong shake + punch; ultimate cinematic zoom; finisher **slow-motion**; victory zoom on winner. Parallax arena layers drift opposite the camera.
7. **Finishers** — classified from battle shape: **Total Domination** (winner keeps ≥3, no lead changes), **Comeback** (winner survives with ≤1), **Clutch** (slow-mo final hit). The finishing blow is detected as the last death before the Victory event.
8. **Arena** — procedural: gradient sky bands, far "mountain" diamonds, near pillars, ground + floor line. Generated shapes only, no assets.

## Tests (System 9)
Full EditMode suite: **58 / 58 pass** (53 prior + 5 new):
- `Choreography_IsDeterministic_ForSameSeed` — identical beat signature across runs and fresh registries.
- `Choreograph_DoesNotChangeOutcomeOrHash` — hash + winner unchanged after choreographing; re-run same seed identical; `choreography.seed == logHash`.
- `Choreography_PairsOneToOneWithReplay` — beats align 1:1 with replay events, timeline order preserved.
- `Choreography_ComboAndHitStopWithinBounds` — combo lengths in their tier bands; every hit-stop within 0.03–0.10 s.
- `Choreography_HasExactlyOneFinisher` — exactly one finishing blow, finisher classified.
All prior determinism/replay/save/battle tests remain green.

## Known limitations
- The rich combo/knockback/camera animation is in the (non-unit-testable) view; the tested guarantee is the deterministic choreography + unchanged hash. **On-device visual QA still needed** to judge feel.
- Combos are cosmetic multi-hit flourish over the single sim damage event — the shown number always equals the sim.
- Arena/parallax are procedural silhouettes (no art pass yet).

## Constraints honored
No combat formula changes · `balance.json` untouched · deterministic simulation untouched · same seed ⇒ identical winner + identical battle hash (test-proven) · presentation-only.
