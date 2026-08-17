# ROADMAP.md — Train Your Monster

**Target of this roadmap: a playable Phase 1 Battle Prototype** — the 30-day
first-playable that answers the four Phase 1 questions with evidence (does the
damage model land 30–90 s battles? does preparation convert to winning? does the
data pipeline hold? is combat deterministic?).

Execution spine:
**Compile → Test → Simulate → Balance → Debug Viewer.**

> Phase 1 deliberately excludes coins/timers, career/leagues, capture flow,
> save/load, and production UI. Those are Build Phases 2–5, sketched at the
> bottom for context only — **do not start them.**

---

## Where we are

Design is complete and consistent. The Phase 1 Core scripts are drafted
(~1,322 lines, no stubs) but have **never been imported, compiled, or run**.
The whole roadmap below is about turning that drop into a verified, playable,
on-device prototype.

## Stage 0 — COMPILE  *(P0 tasks)*

Bring the code to life inside Unity.

1. Create a Unity 2021.3 LTS+ 2D Android project; import the drop's `Assets/`;
   set up Core/Tests asmdefs.
2. Resolve to **zero compile errors**; confirm `balance.json` loads.
3. Run **MTA → Generate Phase 1 Content** → 10 skills + 12 species assets.

**Exit gate:** project compiles clean; registry returns 12 valid species.

## Stage 1 — TEST  *(P1-1)*

Prove the mechanics before trusting the simulator.

1. Run edit-mode `Phase1GateTests` (8 tests). Fix Core/tests until green.
2. This verifies success criteria **1, 3, 5, 6** and mechanics math, plus a
   small-sample preparation/mirror check.

**Exit gate:** all 8 tests pass; determinism hash identical across 100 runs;
green run archived in `/reports`.

## Stage 2 — SIMULATE  *(P1-2)*

Make the balance engine usable.

1. Build `Editor/BalanceSweepWindow.cs` (`MTA/Balance Sweep`) wrapping
   `BalanceSweep.Run` — inputs (count/level/comp-gen/persona/team-size),
   outputs (per-battle CSV + P10/P50/P90, hard-resolve %, win rate, sub-15 s
   anomalies, pass/fail vs thresholds).
2. Confirm 1,000 battles run in seconds with no per-battle allocations.

**Exit gate:** the window sweeps the real 12-species roster and emits a CSV +
summary with a pass/fail line for criteria 2–4.

## Stage 3 — BALANCE  *(P1-3 — the Phase 1 gate)*

Tune values (never formula shapes) from the histogram, not from feel.

1. Sweep the roster; populate `speciesGainRates` if needed; iterate on
   `balance.json` until:
   - Duration **P10 ≥ 30 s, P90 ≤ 90 s**, ≤ 5% hard-resolve, 0 non-terminating.
   - Mirror fairness **50% ± 3%** (2,000 battles).
   - Preparation signal **≥ 75%** (trained beats untrained mirror, 1,000 battles).
2. Export the per-species 1v1 round-robin CSV (informational, for Phase 3).
3. Re-run the sweep after every multiplier change.

**Exit gate:** success criteria **2, 3, 4** verified on real content;
before/after tuning recorded in `/reports`.

## Stage 4 — DEBUG VIEWER  *(P2 + P3)*

See a battle, then see it on a phone.

1. `Battle/BattleReplayView.cs` + `UnitView.cs` — replays the event log only
   (placeholder art; white-flash + knockback for "hit"); portrait; no per-frame
   allocations.
2. Debug harness to pick two teams + seed, run the sim, feed the replay.
3. Verify replay final-HP matches sim state (log-replay consistency).
4. Android build of the debug scene; install on a device; replay a battle.

**Exit gate (Phase 1 complete):** all **7** success criteria checked, including
device proof (criterion 7). Checklist archived in `/reports`.

---

## Phase 1 milestone map

| Stage | Tasks | Verifies criteria | Output |
|---|---|---|---|
| 0 Compile | P0-1, P0-2 | — | Live project + generated assets |
| 1 Test | P1-1 | 1, 3, 5, 6 | Green edit-mode suite |
| 2 Simulate | P1-2 | (tooling for 2–4) | Balance Sweep window + CSV |
| 3 Balance | P1-3 | 2, 3, 4 | Tuned `balance.json`, sweep report |
| 4 Debug Viewer | P2-1, P2-2, P3-1 | 7 (+ replay consistency) | On-device debug replay; Phase 1 done |

**Phase 1 estimate:** the spec budgets ≈ 30 solo-dev evenings (~4 weeks part-
time) for the full build; since Core is already drafted, remaining effort is
weighted toward Stages 0–4 above (import/verify/tune/view), with the riskiest
system — the simulator — already written and proven the moment Stages 1–3 go
green.

---

## Beyond Phase 1 (context only — DO NOT START)

Per the MVP build roadmap:

- **Build Phase 2 — Progression systems:** XP-from-battle wiring, save/load,
  training timers/fees, stat-allocation UI.
- **Build Phase 3 — Content pass:** per-species signature skills, career mode
  (45-battle table + gates), leagues, capture flow, nicknames.
- **Build Phase 4 — Asset pass:** confirm huberthart license → purchase, chibi
  monsters, Kenney UI, VFX, audio, adapt roster to the pack.
- **Build Phase 5 — Play Store release:** Android build hardening, store assets,
  QA pass, low-end performance optimization.

Retention layers (overnight camp appointment, promotion-gate labels, mastery
grades) attach during Phases 2–3. Mastery grades may slip to a first patch.
