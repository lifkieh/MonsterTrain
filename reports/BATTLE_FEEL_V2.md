# Battle Feel V2

Date: 2026-08-17. Second presentation pass — species-specific attacks, projectiles,
hit-stop, VFX, camera, HP-bar animation, death polish, auto-pacing. **Presentation
only** — deterministic simulation preserved, combat outcomes unchanged,
`balance.json` untouched. Same seed → identical hash + identical replay order
(test-proven).

## Files changed

**New**
- `Assets/Scripts/Meta/AttackStyle.cs` — species → attack style (pure C#, testable).
- `Assets/Scripts/Battle/BattleFx.cs` — pooled procedural VFX bursts + pooled projectiles.
- `Assets/Scripts/Tests/BattleFeelTests.cs` — 3 tests.

**Rewritten (presentation)**
- `Assets/Scripts/Battle/UnitView.cs` — smooth + delayed HP bar; death knockback.
- `Assets/Scripts/Battle/BattleReplayView.cs` — styles, projectiles, hit-stop,
  camera, VFX, auto-pacing.

**Edited**
- `Assets/Scripts/App/GameBootstrap.cs` — builds the attack-style map, passes it in.

**Untouched:** all `Core/` (sim, determinism, event log) and `balance.json`.

## New systems (by task)

1. **Species attack styles** — `MeleeLunge / HeavySmash / RangedProjectile /
   AssassinDash / MageCast`, derived from base stats + basic-skill scaling (INT
   basic → caster; fast+light → ranged; fast → dash; heavy → smash; else lunge).
   Different dash distance/feel per style.
2. **Projectile system** — pooled quads; travel actor→target (~0.22 s) then
   trigger the hit + impact burst. Used by ranged/mage styles (mage = purple,
   ranged = gold).
3. **Hit-stop** — 0.04 s on normal hits, **0.08 s on crit/ultimate** (freezes the
   replay clock briefly for punch).
4. **Camera** — attack shake, bigger crit shake, ultimate shake + zoom punch,
   victory zoom. (Stage RectTransform; no Cinemachine.)
5. **VFX** — slash (melee/dash), impact burst (smash), heal burst, crit burst,
   ultimate burst — pooled expanding/fading procedural quads.
6. **HP bar animation** — main fill interpolates smoothly toward the new value; a
   **delayed "ghost" bar** trails behind to show the chunk just lost. Fill recolors
   green→amber→red by health.
7. **Death polish** — knockback away from the attacker + rotation + fade + sink.
8. **Speed controls** — 0.5× / 1× / 2× / 4× (retained).
9. **Auto-pacing** — target replay window scaled from sim duration (15–60 s), then
   **close matches ×1.25 (longer), stomps ×0.7 (faster)** using drama stats
   (survivors + lead changes). Simulator is never slowed — playback only.
10. **Tests** — see below.

## Tests (Task 10)

Full EditMode suite: **22 / 22 pass** (19 prior + 3 new).
- `AttackStyle_ClassifiesEachArchetype` — all five styles classified from stats.
- `AttackStyle_MapIsDeterministic` — same input → same map.
- `Presentation_KeepsHashAndEventOrder` — building styles/replay/drama does not
  mutate the result; **replay event kind+time order identical across builds**, and
  **same seed → same `logHash`** (simulation hash unchanged).

Plus the prior guarantees still pass: `Replay_DoesNotChangeOutcomeOrHash`,
`Replay_EventsAreTimeOrdered`, `Determinism_SameSeedSameHash_100Runs`.

## Performance notes

- All FX (bursts, projectiles, floating text) are **pooled** and reused — no
  steady-state allocation. Animations are transform/color updates in `Update()`.
- No sprites/particles/post-processing; ≤ 6 units + a handful of transient FX on
  screen → 60 fps target is comfortable on the S24 FE.
- Hit-stop uses the replay clock only (does not stall the whole app).

## Known limitations

- Visual correctness/feel not verified by the agent (headless) — needs on-device
  human QA (`MOBILE_QA_CHECKLIST`).
- Projectiles/bursts are colored quads (no sprites yet — Build Phase 4).
- HP-bar drop follows the reconstructed sim HP at event time while a projectile is
  mid-flight → the delayed ghost bar hides the small visual lead; acceptable.
- Camera shake/zoom jitter uses `UnityEngine.Random` — **visual only, never
  touches the simulator/determinism**.

## Constraints honored

Deterministic sim preserved · combat outcomes unchanged · `balance.json` untouched
· same-seed hash + replay order unchanged (test-proven) · presentation layer only.
