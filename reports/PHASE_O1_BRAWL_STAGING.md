# Phase O-1 — Brawl Staging & Active-Fighter Bug

Date: 2026-08-18 · Presentation layer only · Author: Lifkie Lie

## Step 0 — Investigation (read-only) findings

**Q: Does the simulator run all six units concurrently on one interleaved timeline, or
sequential 1v1 duels?**
**A: Fully CONCURRENT — one shared timeline across all six units.** Evidence:

- `BattleSimulator.Run` (Core) loops one clock: `var actor = ActionTimeline.NextActor(state)`
  then `state.clock = when` (BattleSimulator.cs:38–44). There is one `state.clock`, not a
  per-duel clock.
- `ActionTimeline.NextActor` considers **both** teams and returns the globally-earliest
  `nextActionTime`: `Consider(s.teamA, ref best); Consider(s.teamB, ref best)`
  (ActionTimeline.cs:14–15). Any unit of either team can be the next actor.
- Each step purges and can act on any unit of both teams, and only ends when a whole team
  is wiped: `if (state.TeamWiped(enemyTeam)) return End(...)` (BattleSimulator.cs:67–69).
  A unit reschedules `nextActionTime` and the loop continues (71–72).
- The event log is therefore a single stream of `Action`/`Modifier`/`Died` events
  interleaved across all six units, each tagged with `actorTeam/actorSlot` +
  `targetTeam/targetSlot`. `BattlePlayback` keys every unit by `(team,slot)`
  (BattlePlayback.cs:31,51) and reconstructs HP for all six on one timeline.

⇒ **Branch A.** The old 1v1 framing was purely a *view/camera* choice; simultaneity is
already how the fight is computed. Staging every unit changes **no outcome** — presentation only.

## Statue-bug root cause

All six `UnitView`s were always created and correctly keyed by `(team,slot)` (no
slot/instanceId desync). The bug was the **1v1 reserve staging layered over a concurrent
sim**: only the front unit (rank 0) sat at `ActiveAnchor`; ranks 1–2 were parked
**off-screen** at `ReserveAnchor` (±820 px) and dimmed/shrunk, with a "next runs in on
death" system. But the sim has *all three* acting from t≈0. So whenever slot 1 or slot 2
was the actor, its choreography played from its off-screen park (its dash dragged it
briefly on-screen and back), while the on-screen front unit — acting only on its own
sparse turns — appeared to stand still. Net effect: "monster a is a statue while b and c
fight." It was never an event→actor mapping error; it was reserve staging hiding
concurrent fighters. Fixed by staging everyone in formation and removing reserves/run-in.

## Files touched (presentation only)
- `Assets/Scripts/Battle/BattleReplayView.cs` — brawl staging rewrite (tasks 1–10).
- `Assets/Scripts/Battle/UnitView.cs` — KO now despawns the HP bar (bar `CanvasGroup`
  fade) and spins on launch (task 6).
- `Assets/Scripts/Tests/BrawlStagingTests.cs` — NEW EditMode test (added coverage).
- `PHASE_O1_BRAWL_SPEC.md` (repo root), `reports/PHASE_O1_BRAWL_STAGING.md` (this file).

Gameplay/Core/Meta-sim/balance/save/`.asset` data: untouched. Choreography director,
`ReplayBuilder`, `BattlePlayback`, `BattleDrama` unchanged (so their EditMode tests are
untouched and still green).

## What changed, by task
1. **Stage everyone.** `Formation(team,slot)` places a `UnitView` per living unit, both
   teams: 3-slot echelon per side (front slot 0 nearest centre, two staggered back rows
   with X stagger + Y depth so bodies never stack), one shared ground band. Player left,
   enemy right, player mirrored. Per-unit shadow + floating HP bar from O-0 carry over.
   Reserves and run-in-on-death removed.
2. **Event→actor fidelity.** Every choreography resolves its actor/target through the
   explicit `_views[(team,slot)]` map (`View(team,slot)`), never spawn order. Held as an
   invariant and covered by a new test (below).
3. **Approach & return.** Melee attackers dash from their formation slot to a side-offset
   approach point beside the target (offset by attacker slot so two attackers on one
   target don't overlap), exchange, then return to slot. Ranged fire the compact sequence
   from their slot.
4. **Spotlight rule.** At most ONE full cinematic melee suite
   (dash→ground→launcher→air→slam) plays at a time, guarded by `_spotlightBusy`. It is
   granted to the higher-drama event (ultimate/crit/heavy `hits≥3`); the drama tiers come
   from the logHash-seeded director beats, so the choice is deterministic. Every other
   concurrent attack uses the compact hit (`CompactDash`, or `CompactLunge` if the unit is
   already mid-dash) or `RangedHit`. Everything still overlaps in time.
5. **Scheduling policy (documented).** Choreographies are realtime coroutines that
   **overlap freely**; the sim clock is NOT frozen for movement, so events fire at their
   sim timestamps and nobody queues idle. Policy for "a unit's next event lands mid-
   choreography": **no queue, no drop — overlap.** A unit already mid-dash takes the
   in-place `CompactLunge` (prevents `combatOffset` conflicts); the current spotlight unit
   ignores new spotlight grants until its suite ends (single-cinematic invariant);
   additional hits on any unit stack additively (impulse/flash), never blocking.
6. **Death = KO.** No replacement run-in. The KO'd unit is launched + spun (UnitView death
   arc), flashes dark, fades out, and its HP bar despawns (bar `CanvasGroup` → 0). The
   fight shrinks 3v3 → 3v2 → 2v2 → … → 1v1 naturally as deaths land.
7. **Camera.** Wide default (rest zoom 1.0 holds the whole brawl); punch-in/shake only on
   spotlight beats, crits, ultimates, and KOs. The ultimate keeps its cinematic zoom; the
   finisher keeps slow-mo.
8. **Hit-stop discipline.** Global sim-clock freeze is reserved for heavy/crit/ult/KO
   beats — light/compact hits never freeze. All freezes draw from a per-battle budget
   **`FREEZE_CAP = 3.5 s`** (`HitStop` no-ops once the budget is spent), so six units can
   never stutter the world.
9. **HUD.** Round pips unchanged (still per-KO, deplete as units fall). Formation spread
   keeps the six floating HP bars separated; the pip row sits at the top and never
   overlaps the bars.
10. **Pools ×6.** VFX pool raised 12 → 24 and pre-warmed for six concurrent units;
    floating-text pool grows/recycles on demand. Note: afterimage ghosts are still
    spawned on demand (only on the spotlight dodge, one at a time) — full ×6 afterimage
    pooling belongs to Phase O's afterimage system and is scheduled there.

## Tests / build
- **EditMode: 67 / 67 passed** (66 prior + 1 new). Determinism, sim, balance, save,
  progression, and all director/replay tests unchanged and green.
- **New test** `BrawlStagingTests.EveryEventActorAndTargetResolvesToASpawnedUnit`: over 5
  seeds, asserts every replay event's actor and target `(team,slot)` resolves to a spawned
  unit (6 units in a 3v3). This is the data-level guarantee behind "no statues." Coverage
  grew; no existing test was weakened or removed.
- **PlayMode UI smoke: PASS** — booted the game and ran a full brawl battle with all six
  units on stage; 0 runtime errors, 0 misplaced buttons.
- **Android APK: Succeeded** — `Build/Android/TrainYourMonster.apk` (~75 MB;
  `MTA: Android build = Succeeded`).

## Human QA checklist (verify on device)
- [ ] Semua monster yang kamu pilih ikut bertarung dari awal — keroyokan, tidak ada yang
      cuma berdiri jadi pajangan.
- [ ] Pertarungan masih "terbaca": kamu tahu siapa memukul siapa; momen besar (crit/ult/KO)
      tetap terasa besar di tengah keramaian (satu sinematik penuh pada satu waktu).
- [ ] KO terasa enak (terpental + berputar keluar arena, HP bar-nya hilang), lalu
      pertarungan mengecil ke 2v2 / 1v1 dengan mulus.
- [ ] FPS stabil dengan 6 unit + VFX sekaligus; tidak ada HP bar / UI yang tumpang tindih.
