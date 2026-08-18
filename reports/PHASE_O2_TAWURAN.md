# Phase O-2 — "Tawuran" Engagement System

Date: 2026-08-19 · Presentation layer only · Author: Lifkie Lie

**Precondition:** Phase O-1 concluded in **Branch A** (concurrent simulation — all six
units on one interleaved timeline; see `reports/PHASE_O1_BRAWL_STAGING.md`). O-2 is
therefore unblocked and remains presentation-only.

## What changed (grammar shift)

O-1 staged everyone but kept the auto-battler grammar: units lived in fixed formation
slots, dashed out to hit, and **returned to slot**, idling between events — a turn-queue
read. O-2 replaces that with a continuous engagement grammar. **The sim, its event log,
and its timestamps are untouched; only the between/around-event motion changes.**

Core mechanism: a new **`EngagementPlanner`** (Meta, scene-free, seeded by `logHash`)
walks the complete replay up front and emits, per unit, a plan of *engagement segments*
(who it fights in each window), *filler beats* (non-damaging whiffs/blocks/shoves in the
gaps), and *clash* indices. The view then drives each unit's **`BasePos` toward a live
engagement anchor every frame** instead of a static slot — so the existing dash/return
coroutines automatically read as persistent brawling (they now "return" to the moving
engagement position, i.e. stay tangled). No gameplay code touched.

## Files touched
**New:**
- `Assets/Scripts/Meta/EngagementPlanner.cs` — planner (segments + fillers + clashes),
  `logHash`-seeded, no `UnityEngine`.
- `Assets/Scripts/Tests/EngagementPlannerTests.cs` — determinism + filler-no-damage.

**Edited:**
- `Assets/Scripts/Battle/BattleReplayView.cs` — opening charge, per-frame engagement
  tracking, filler playback, clash flourish, crowd flinch, ranged kite, soft separation.
  Spotlight + capped hit-stop from O-1 unchanged.

Gameplay/Core/sim/balance/save/`.asset`: untouched. Cinematic director, `ReplayBuilder`,
`BattlePlayback`, `BattleDrama` unchanged (their EditMode tests untouched, still green).

## Design → implementation (by rule)
1. **Opening charge.** Units start in formation only for frame 0; for the first
   `CHARGE_DUR = 1.15 s` both teams sprint to a centre cluster (`ChargeAnchor`) and
   collide — multi-spark + shockwave + shake at `CHARGE_CONTACT = 0.62 s` (`OpeningClash`).
   (A "FIGHT!" splash hook is left for Phase P.)
2. **Persistent engagement (no return-to-slot).** After the charge, `EngageAnchor` places
   each unit ~150 px on its own side of its current opponent. Coroutines return to this
   moving `BasePos`, so a unit ends each exchange still on its partner — never a formation
   line. Slots are gone after the charge.
3. **Living idle.** The anchor is never static: a per-unit `logHash`-seeded phase drives a
   circling offset (±26 px X cos, ±20 px Y sin) plus the existing idle bob — no unit holds
   still.
4. **Filler beats (non-damaging).** Between a unit's real events the planner inserts
   whiff/block/shove beats. They call **no** `_texts.Spawn` (no numbers), **never** touch
   `_pb` HP, and **never** `PlayHit` (no white flash). Distinct language: whiff =
   speed-lines, block = **blue** spark + tiny mutual pushback, shove = puff + push — vs a
   real hit's white flash + number. Rates: `FILLER_INTERVAL = 0.7 s`, held
   `FILLER_LEAD = 0.28 s` clear of any real event, only in gaps `> 0.62 s`.
5. **Clash moments.** Planner flags reciprocal real events within `CLASH_WINDOW = 0.15 s`;
   the view renders a lunge-collide (`ClashFlourish`: big spark + mini shockwave + "CLASH"
   + mutual push) and the real damage still lands via the events. Cooldown 0.18 s prevents
   a double flourish on the paired event.
6. **Target switching.** Segments engage the opponent of the unit's **next** real
   interaction, so when the next event names a new target the unit is already charging it —
   no teleport, no slot logic.
7. **Ranged kite.** Ranged units hold a larger gap (300 px) and back off by 150 px when an
   enemy closes within 250 px — constant motion, firing on the move.
8. **Scrum drift + separation.** Every anchor is lerped 16 % toward centre each frame so
   the teams interpenetrate; pairwise soft separation (`minD = 135 px`) keeps bodies from
   stacking. Positions clamped to the arena band.
9. **Crowd flinch.** On an ultimate cast (r ≤ 300) and on a KO (r ≤ 280), nearby
   non-participants get an away-impulse (`CrowdFlinch`).
10. **Spotlight + hit-stop rules carry over** from O-1 unchanged (one full cinematic suite
    at a time; global freeze budget 3.5 s/battle for heavy/crit/ult/KO only). Units that
    are the current spotlight actor/target are exempted from engagement motion so the
    cinematic isn't disturbed.

## Planner policy & rates (chosen)
- **Opponent policy:** per unit, opponent(t) = the opponent of its *next* real interaction
  (attack it makes or takes) in the log; before the first, its first opponent; after the
  last, its last opponent; dead-partner ⇒ view redirects to nearest living enemy.
- **Filler:** interval 0.7 s, lead 0.28 s, min-gap 0.62 s, kind ∈ {whiff, block, shove}
  chosen from the seeded RNG. Rate-limited by construction; skipped while a unit is in a
  cinematic.
- **Clash window:** 0.15 s. **Charge:** 1.15 s, contact 0.62 s.
- Determinism: planner consumes a single `logHash`-seeded xorshift in a fixed order (idle
  phases in spawn order, then filler kinds/jitter), so same log ⇒ byte-identical plan. No
  `UnityEngine.Random` and no per-frame allocation in the engagement/filler hot paths
  (indexed loops, cached dictionaries, value-type math).

## Tests / build
- **EditMode: 69 / 69 passed** (67 prior + 2 new). Determinism, sim, balance, save,
  progression, and all director/replay tests unchanged and green.
- **New tests** (`EngagementPlannerTests`): (a) the plan is byte-identical across fresh
  runs of the same seed (3 seeds); (b) every filler beat targets an enemy and sits
  ≥ 0.09 s clear of every real event for its unit, and `FillerBeat` has no amount/HP field
  by construction — filler can never move HP.
- **PlayMode UI smoke: PASS** — booted the game and ran a full tawuran battle (opening
  charge, engagement, filler, clash, KO shrink) with 0 runtime errors, 0 misplaced buttons.
- **Android APK: Succeeded** — `Build/Android/TrainYourMonster.apk` (~75 MB;
  `MTA: Android build = Succeeded`).

## Human QA checklist (verify on device)
- [ ] Screenshot 3 momen ACAK di tengah battle. Kalau ketiganya masih dua barisan rapi
      berhadapan → gagal, lapor. Kalau kerumunan saling terkam → lulus.
- [ ] Pembukaan "tawuran": dua kubu lari & tabrakan di tengah.
- [ ] Tidak ada monster berdiri diam menunggu giliran; semua terus bergerak/menekan.
- [ ] Pukulan beneran (angka damage, HP turun) jelas beda dari block/whiff (percikan kecil,
      tanpa angka).
- [ ] FPS stabil; pertarungan tidak jadi bubur tak terbaca (satu sinematik penuh sekali
      waktu tetap menonjol).
