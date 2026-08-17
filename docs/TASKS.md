# TASKS.md — Phase 1 Battle Prototype

Prioritized, actionable task list. **Scope: Phase 1 Battle Prototype ONLY.**
No Phase 2+ tasks (no coins/fees/timers, career UI, capture flow, save/load,
production UI — those are out of scope here). Starting point: the Phase 1
scripts drop exists but has never been imported, compiled, or run.

Priority: **P0** = blocks everything · **P1** = core gate · **P2** = required
to finish Phase 1 · **P3** = closes the last success criterion.

---

## P0-1 — Stand up the Unity project & import scripts

- **Priority:** P0
- **Description:** Create a Unity 2021.3 LTS+ 2D (Android-target) project at the
  repo. Copy the drop's `Assets/` in. Ensure `Core/` and `Tests/` sit in
  assembly definitions that let edit-mode tests reference Core (NUnit +
  `UNITY_INCLUDE_TESTS`). Confirm `balance.json` is in `StreamingAssets/`.
- **Dependencies:** none.
- **Acceptance:** Unity opens the project with **zero compile errors** in the
  Console; Core and Tests asmdefs resolve; `balance.json` loads via
  `SpeciesDatabase.LoadBalance()` without exception.

## P0-2 — Generate Phase 1 content assets

- **Priority:** P0
- **Description:** Run menu **MTA → Generate Phase 1 Content** to create the
  10-skill shared pool and 12 species `.asset` files under `Resources/{Skills,
  Monsters}` from the approved tables.
- **Dependencies:** P0-1.
- **Acceptance:** 10 skill assets + 12 species assets exist under `Resources/`;
  `SpeciesDatabase.LoadFromResources()` returns a registry of 12 species with no
  null skills; data-validation (unique ids, 3 skills each, ultimate budget cap)
  passes.

## P1-1 — Run edit-mode gate tests green

- **Priority:** P1
- **Description:** Open Test Runner → EditMode → Run All. Fix any failures in
  `Phase1GateTests` (or the Core code they exercise) until all pass. Covers
  success criteria 1, 3, 4, 5, 6 and mechanics math.
- **Dependencies:** P0-1 (tests are pure Core; do not require generated assets).
- **Acceptance:** All 8 tests pass: StatMath formulas, training-through-grade,
  determinism ×100 (identical `logHash`), all team sizes terminate ≤ 120 s,
  13th-species-from-data, preparation signal ≥ 0.75, mirror win rate in
  0.42–0.58. Green run captured (screenshot/log) into `/reports`.

## P1-2 — Build the Balance Sweep EditorWindow

- **Priority:** P1
- **Description:** Add `Editor/BalanceSweepWindow.cs` (`MTA/Balance Sweep`
  menu) wrapping the existing `BalanceSweep.Run`. Inputs: battle count, level,
  comp generator (random-role-valid / mirror / explicit), persona
  (untrained / trained-N), team size. Outputs: per-battle CSV + summary
  (duration P10/P50/P90, hard-resolve %, win rate, sub-15 s anomalies) and
  pass/fail against success-criteria thresholds.
- **Dependencies:** P0-2 (needs the 12 species to sweep realistic comps).
- **Acceptance:** Window runs 1,000 battles in seconds with no per-battle
  allocations beyond the log; writes a CSV; prints the summary; shows a
  pass/fail line for criteria 2–4.

## P1-3 — First balance verification & tuning pass (the Phase 1 gate)

- **Priority:** P1
- **Description:** Run the sweep across the real 12-species roster. Populate
  `speciesGainRates` in `balance.json` if per-species tuning is needed. Tune
  `balance.json` values (never formula shapes) until the duration and fairness
  criteria pass. Record before/after in `/reports`.
- **Dependencies:** P1-2.
- **Acceptance (success criteria 2–4 verified on real content):**
  - Duration: **P10 ≥ 30 s, P90 ≤ 90 s**, ≤ 5% hit the 120 s hard resolve,
    0 battles fail to terminate (1,000-battle sweep).
  - Mirror fairness: **50% ± 3%** over 2,000 mirror battles.
  - Preparation signal: trained (10 lvl + 10 training units) beats untrained
    mirror in **≥ 75%** of 1,000 battles.
  - Per-species 1v1 round-robin CSV exported (informational, for Phase 3).

## P2-1 — Debug battle replay view

- **Priority:** P2
- **Description:** Add `Battle/BattleReplayView.cs` + `Battle/UnitView.cs`
  (placeholder art: CraftPix free golems or colored quads, per prototype asset
  rule). The view **consumes the event log only** — it never re-simulates.
  HP bars, basic attack/skill/death cues (white-flash + micro-knockback for
  "hit"). Portrait orientation.
- **Dependencies:** P1-1 (a trusted sim/event log to replay).
- **Acceptance:** A debug scene plays back a recorded `BattleResult.events[]`;
  final HP recomputed from the replay matches sim state; no per-frame
  allocations in the hot path; renders in portrait.

## P2-2 — Debug harness to launch/record battles

- **Priority:** P2
- **Description:** A minimal debug entry point (scene + inspector-driven
  component or console) to pick two `TeamConfig`s (species/level/allocated/
  trained), a seed, run the sim, and feed the result to the replay view.
- **Dependencies:** P2-1.
- **Acceptance:** From the debug scene, a designer can run an arbitrary matchup
  and watch it replay; the same seed replays identically.

## P3-1 — Android device build of the debug scene (criterion 7)

- **Priority:** P3
- **Description:** Configure Android build settings (portrait, min SDK for the
  low-end target), build the debug replay scene, install on a device, play back
  a recorded battle. Run the full success-criteria checklist end to end.
- **Dependencies:** P2-2.
- **Acceptance:** APK installs and runs on an Android device; a recorded battle
  replays on-device; **all 7 Phase 1 success criteria checked off** and the
  checklist archived in `/reports`.

---

## Out of Phase 1 scope (do NOT start here)

Coins/fees/timers · training UX (freshness decay, session tiers) · XP-from-
battle wiring · save/load · career mode / leagues / gates · capture flow /
nicknames · production UI (5 screens) · final art/VFX/SFX · store listing ·
per-species signature skills. These belong to Build Phases 2–5.
