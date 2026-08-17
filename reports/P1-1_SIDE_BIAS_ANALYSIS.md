# P1-1 — Mirror Side-Bias Analysis

Investigation only. No code, `balance.json`, or design changed.

Failing test: `MirrorComps_NoSideBias` (`Assets/Scripts/Tests/Phase1GateTests.cs:127`).
Observed: **team A wins 73.5%** of 400 mirror battles (expected 42–58%).
Sweep line: `battles=400 P10=12.5s P50=15.6s P90=19.5s hardResolve=0.0% sub15s=87 teamAWin=73.5%`.

## Root cause

**A deterministic initiative tie-break that always favours team A, applied to
units whose action schedules are permanently phase-locked.** Two code facts
combine:

1. **The tie-break prefers team A.** `ActionTimeline.Consider`
   (`Assets/Scripts/Core/ActionTimeline.cs:28`) resolves equal action times by:
   earliest time → higher **base** SPD → **team A before team B** (`u.team <
   best.team`) → lower slot. When two units are otherwise equal, **team A always
   acts first.**

2. **Mirrored, same-SPD units stay phase-locked forever, so that tie fires every
   cycle.** Every unit starts at `nextActionTime = ActionInterval(SPD)` with **no
   stagger** (`BattleSimulator.BuildTeam`, `BattleSimulator.cs:106`) and
   reschedules by the same fixed `+ActionInterval(SPD)` each turn
   (`BattleSimulator.cs:66`). Two units with equal SPD therefore share an
   identical, never-diverging schedule — they tie at **every** action, and team
   A wins **every** one of those ties.

Because the battle is a burst (median 15.6 s, 87/400 under 15 s — see the
duration report), winning initiative means team A lands the first hit on a
shared focus target and often secures the **first kill** before its team-B
counterpart acts. That converts into a numbers advantage that snowballs. Result:
a systematic ~73.5% edge, not the ~50% a fair mirror requires.

## Why same-SPD ties are common (not rare)

The tie-break was intended as a rare determinism guarantee, but at these stats it
is the **dominant** effect, because SPD takes very few discrete values:

- `LevelGain(SPD) = round(gainRate[SPD] × tierMultiplier[grade])` with
  `gainRate[SPD]=0.6` (`BalanceConfig.cs:25`) and tier multipliers
  `{D .6, C .8, B 1, A 1.25, S 1.5}` (`BalanceConfig.cs:24`) evaluates to **0**
  for grades D/C and **1** for B/A/S. Over `level-1 = 4` levels that is **+0 or
  +4 SPD** — nothing else.
- So each species has only two possible SPD values at level 5 (e.g. the test's
  `"a"` → 12 or 16; `"b"` → 18 or 22). Across a 3v3 with duplicated species,
  many units share identical SPD → identical `ActionInterval` → phase-locked
  ties every cycle.

## Ruling the candidate causes in / out (with evidence)

- **Turn order — YES, primary.** The `team A < team B` tie-break
  (`ActionTimeline.cs:28`) is the systematic asymmetry.
- **Initiative — YES, same mechanism.** "Acts first" is exactly the first-strike
  advantage; amplified by the short TTK.
- **SPD scaling — CONTRIBUTOR, not the source.** The coarse `+0/+4` SPD ladder
  (from `gainRate[SPD]=0.6` rounding) makes exact ties frequent, which is what
  lets the biased tie-break fire constantly. Fair scaling would reduce tie
  frequency but the tie-break would still favour A on the ties that remain.
- **Target selection — RULED OUT.** `TargetSelector` (`TargetSelector.cs`) is
  symmetric: both teams use front-most / lowest-HP / lowest-HP% with tie → lower
  slot **within the enemy team**. No team term. Contributes no side bias.
- **RNG sequencing — RULED OUT as a source.** Growth grades are rolled i.i.d.
  per unit (team A's 18 draws, then team B's 18) via `GrowthWeights.Roll`
  (`ContentData.cs:50`, one sample each) — draw order does not bias an
  independent weighted pick, so mirror growth asymmetry is **fair noise centred
  on 50%**, not a systematic edge. Crit rolls are consumed in resolution order
  (`SkillResolver.cs:42`); team A consuming crit RNG "first" is a *consequence*
  of the turn-order bias, not an independent cause.
- **Other systemic cause — the no-stagger phase-lock** (`BattleSimulator.cs:106`)
  is the enabler that turns a rare tie-break into a per-cycle one. Counts as part
  of the root cause.

Net: one primary defect (biased tie-break) + one enabler (phase-locked
schedules), amplified by short TTK and coarse SPD scaling.

## Code locations

| What | File:line |
|---|---|
| Biased tie-break (`team A < team B`) | `Assets/Scripts/Core/ActionTimeline.cs:28` |
| No initial stagger (all start at `interval`) | `Assets/Scripts/Core/BattleSimulator.cs:106` |
| Fixed reschedule (`+ActionInterval`) | `Assets/Scripts/Core/BattleSimulator.cs:66` |
| Coarse SPD gain (`gainRate[SPD]=0.6`) | `Assets/Scripts/Core/BalanceConfig.cs:25` |
| Mirror clones team A exactly | `Assets/Scripts/Core/BalanceSweep.cs:52,87` |
| Growth rolled per unit, i.i.d. | `Assets/Scripts/Core/MonsterInstance.cs:32`, `ContentData.cs:50` |
| Failing assertion | `Assets/Scripts/Tests/Phase1GateTests.cs:133` |

## Proposed fixes (NOT implemented — for a later approved change)

Ordered by recommendation. All keep `Determinism_SameSeedSameHash` green
(same-seed reproducibility holds as long as the new rule is itself seed-derived),
but **any** ordering change alters the event-log hash, so the determinism test's
golden value would need re-baselining.

- **A. Seeded fair tie-break (recommended).** When time **and** SPD tie, pick the
  winner with a draw from the seeded `Random` instead of `team A`. Fully
  deterministic per seed; makes each individual mirror battle fair. Cost: inserts
  an RNG draw into scheduling → the documented RNG contract
  (`BattleSimulator.cs:9`) and determinism golden hash must be updated. Touches
  `ActionTimeline` (needs access to the rng) + the contract comment.

- **B. Seed-parity team preference (smallest change).** Break final ties for
  team A or team B based on a seed-derived bit, so across a sweep ~50% of battles
  favour each side. No change to the RNG stream (determinism hash still stable
  without re-baselining the crit sequence). Cost: a single mirror battle is still
  one-sided (balanced only in aggregate) — acceptable for the sweep test, weaker
  as a fairness guarantee.

- **C. Per-unit seeded schedule stagger.** Add a small seeded offset to each
  unit's initial `nextActionTime` so equal-SPD units stop phase-locking. Reduces
  tie frequency at the source; must be seed-derived and team-neutral. Similar
  determinism cost to A.

- **D. Simultaneous resolution for exact-time ties.** Let both tied units act at
  the same instant, applying damage before removing the dead, so first-strike no
  longer steals a kill. Most faithful to "fair mirror" but the most invasive
  (changes resolution flow + event ordering/log).

- **E. Fix duration first (partial).** Longer battles (duration report) dilute
  first-strike, likely dropping the bias toward ~55–60%, but **not** to 50% —
  the systematic tie-break persists. Necessary but **insufficient** on its own to
  pass this test.

## Estimated impact

- **A or C** → mirror win rate expected **~47–53%** (well inside the 42–58% band);
  passes `MirrorComps_NoSideBias`. Requires re-baselining the determinism golden
  hash (one-line test update) — no gameplay-number change.
- **B** → aggregate mirror rate **~50%** across the sweep; passes the population
  test; cheapest; leaves single-battle mirrors one-sided.
- **D** → ~50% and the most "correct", highest implementation risk.
- **E alone** → ~55–60%; **does not pass**. Pair with A/B/C.
- Scope: all are `Core/` logic changes (initiative fairness), **not** balance
  tuning. Effort ≈ 0.5 day for A/B + test re-baseline. Deferred to the
  implementation phase — not part of this analysis task.

## Note

The side-bias and the short-duration findings are **coupled**: the biased
initiative only produces a *73.5%* swing because TTK is ~15 s. A correct fix
addresses both — the tie-break for fairness, and the balance for duration (see
`P1-1_BATTLE_DURATION_ANALYSIS.md`).
