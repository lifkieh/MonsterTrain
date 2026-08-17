# CHECKPOINT 002 — Mirror Initiative Bias Fixed

Date: 2026-08-17. Follows Checkpoint 001 (first compiling project). Scope: the
P1-1 initiative side-bias fix only. No balance tuning, no `balance.json` change,
no gameplay redesign.

## Problem

The Phase 1 gate test `MirrorComps_NoSideBias` failed: in 400 mirror battles
(identical teams both sides), **team A won 73.5%** — far outside the required
42–58%. A mirror must be ~50/50, so the simulator had a systematic side bias.

## Root cause

Deterministic initiative bias, not a balance issue. In
`ActionTimeline.Consider`, equal action-times were broken **team A before team
B**, and mirrored same-SPD units share an identical, never-diverging action
schedule (all start at `nextActionTime = interval(SPD)`, reschedule by the same
`+interval`). So team A won **every** initiative tie, took the first strike, and
— at the current ~16 s TTK — snowballed that into 73.5%. Full trace in
`reports/P1-1_SIDE_BIAS_ANALYSIS.md`.

## Chosen solution

Candidate 5 — **deterministic seeded per-unit initiative key** (selected in
`reports/P1-1A_SIDE_BIAS_FIX_RECOMMENDATION.md` over four alternatives). Each unit
gets `initiativeKey = FNV1a64(seed, team, slot, speciesId)`, computed once at
team build; the tie-break uses that key instead of the `team` term. Chosen for
lowest risk to the make-or-break simulator, zero RNG-stream impact, and no
replay-view or test-golden changes.

## Implementation summary

Three `Core/` files (+43/−7); no test or `balance.json` change:

- `BattleState.cs` — added `ulong initiativeKey` to `CombatUnit`.
- `BattleSimulator.cs` — `BuildTeam` takes `seed`; sets the key; added
  `InitiativeKey` (FNV-1a 64) + `MixInt` helpers (consume no RNG).
- `ActionTimeline.cs` — tie-break is now
  `time → base SPD → initiativeKey → slot → team (collision-only)`; the
  `team A first` bias term is removed. Total ordering (hence determinism) is
  preserved. Full detail in `reports/P1-1B_IMPLEMENTATION_REPORT.md`.

## Before / after metrics

`MirrorComps_NoSideBias` — 400 mirror battles, level 5, 3v3, baseSeed 555:

| Metric | Before | After |
|---|---|---|
| team-A win rate | **73.5%** (FAIL) | **50.8%** (PASS) |
| P10 / P50 / P90 | 12.5 / 15.6 / 19.5 s | 12.5 / 15.9 / 20.5 s |
| sub-15 s | 87 / 400 | 70 / 400 |
| hard-resolves | 0.0% | 0.0% |

Duration essentially unchanged (the fix touches only initiative, not the
damage/HP model).

## Test results

Full EditMode suite: **7 / 7 pass, 0 fail** (was 6/7).

- StatMath_MatchesSpecFormulas · Training_RoutesThroughGrowthGrade ·
  Determinism_SameSeedSameHash_100Runs · AllTeamSizes_Terminate_Within_HardResolve ·
  ThirteenthSpecies_FromPureData_ZeroCode · PreparationSignal_TrainedBeatsUntrained ·
  **MirrorComps_NoSideBias** — all Passed.

Determinism preserved (100-run identical hash); RNG contract preserved (the key
consumes zero `System.Random` draws); no golden-hash re-baseline required (the
determinism test is self-referential).

## Remaining open issues

1. **Short battle duration (highest priority).** Median ~16 s vs the 30–90 s
   target; 70/400 battles still under 15 s. This is a balance concern (damage
   too high relative to HP; DEF near-inert at `K=50`) — analysis in
   `reports/P1-1_BATTLE_DURATION_ANALYSIS.md`. It requires a Stage-3 balance pass
   and is **not** started (no balance work authorized yet).
2. **Balance criteria 2–4 still unverified on real content.** The full duration/
   fairness/preparation sweep across the 12 real species (not just the test's
   `a`/`b`) has not been run — that is Stage 2 (build the Balance Sweep window)
   then Stage 3 (tune + verify).
3. **Debug replay view + Android device proof** (criterion 7) not started.
4. **Unity 6.5 vs docs' 2021.3 LTS** version drift noted (harmless so far).

Next logical milestone: address the duration balance (Stage 2 → Stage 3), which
also further dampens any residual first-strike effect.
