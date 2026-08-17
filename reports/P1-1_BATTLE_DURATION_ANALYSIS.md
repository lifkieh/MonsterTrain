# P1-1 — Battle Duration Analysis

Investigation only. No code, `balance.json`, or design changed.

Observed (from `MirrorComps_NoSideBias`, level 5, 3v3, 400 battles):
`P10=12.5s P50=15.6s P90=19.5s`, **87/400 battles under 15 s**, 0% hard-resolves.
Target (spec / GDD): **30–90 s** (P10 ≥ 30 s, P90 ≤ 90 s), sub-15 s flagged as a
burst anomaly. Battles are running **~2–3× too fast**.

## Root cause

**Damage output is far too high relative to HP pools at level 5, and DEF barely
mitigates it.** Time-to-kill per unit lands around ~3–6 s, so a whole 3v3
resolves in ~12–20 s. The balance sheet already predicted this scaling failure
("flat K means DEF falls off as ATK grows"; "growth can dwarf allocation"); the
sweep confirms it at level 5.

## Worked TTK model (level 5, the test's `"a"` = base HP100/ATK20/DEF12/SPD12)

Effective stats at level 5 (`EffectiveStats`, `StatMath.cs:39`;
`base + LevelGain×4`), grade-dependent ranges with `defaultGainRates =
{HP 2.5, ATK 1.0, DEF 0.8, SPD 0.6, INT 1.0, LUCK 0.4}` (`BalanceConfig.cs:25`):

| Stat | base | +4 levels (D…S) | effective |
|---|---|---|---|
| HP | 100 | +8 … +16 | **108–116** |
| ATK | 20 | +4 … +8 | **24–28** |
| DEF | 12 | +0 … +4 | **12–16** |
| SPD | 12 | +0 … +4 | **12–16** |

Take a representative attacker ATK 24 vs target DEF 16:

- **Mitigation** = `1 − DEF/(DEF+K)` = `1 − 16/66` = **0.758** — DEF removes only
  ~24% (`StatMath.Mitigation`, `StatMath.cs:12`, `K=50`).
- **Basic hit** = `24 × 1.0 × 0.758` ≈ **18** damage.
- **Attack rate** `aps = 0.02 × SPD` → SPD16 = 0.32/s → **interval 3.1 s**
  (`StatMath.AttacksPerSecond`, `StatMath.cs:15`).
- **Active** `power_strike` 2.8× on 8 s cd → `24 × 2.8 × 0.758` ≈ **51** per cast
  (`SpeciesAssetGenerator.cs:26`).
- **Ultimate** `savage_rend` 3.8× at t ≥ 15 s → `24 × 3.8 × 0.758` ≈ **69**.

Focus-fire, 3 attackers on one target (HP ~112):
per-attacker early DPS ≈ basics (18 / 3.1 ≈ 5.8/s) + one active per 8 s
(51 / 8 ≈ 6.4/s) ≈ **~12 DPS**; three attackers ≈ **~36 DPS** →
**first kill in ≈ 3 s.** Damage skills then retarget the next-lowest-HP enemy
(`SkillResolver.cs:38`), so kills cascade; the full 3v3 empties in ~12–20 s —
matching the observed P50 15.6 s.

## Contributing factors (ranked)

1. **HP-to-damage ratio too low.** ~112 HP versus ~18/basic and 50–69/skill means
   a unit dies in a handful of actions. HP has the *highest* gain rate (2.5) yet
   +8–16 HP is under one extra basic hit of survivability — HP growth does not
   meaningfully extend TTK.
2. **DEF is nearly inert.** With DEF 12–16 and `K=50`, mitigation is only
   ~24–30%. DEF cannot scale into relevance (flat `K`), exactly the balance
   sheet's "DEF falls off as ATK grows" risk.
3. **Skill multipliers large and frequent.** 2.8× actives on **8 s** cooldowns
   plus 3.8× ultimates land inside a 15 s window; each is 3–4 basic hits of
   burst against ~112 HP.
4. **Level scaling favours offence.** From level 1 → 5, ATK grows ~+20–40% and
   skill damage scales with it, while effective survivability (HP × mitigation)
   grows less — so battles get **faster** at higher level, not slower. (The
   balance-sheet TTK examples were level-1 base stats.)
5. **Focus-fire targeting concentrates damage.** Deterministic front-most / lowest-HP
   targeting (by design) means no damage is "wasted" spreading across enemies,
   minimising TTK.
6. **Anti-stall is irrelevant here.** It only starts at 75 s (`antiStallStart=75`,
   `BalanceConfig.cs:33`); battles end at ~15 s, so it never engages (0%
   hard-resolves confirms nothing approaches the ceiling).

## Code locations

| What | File:line |
|---|---|
| Mitigation `1 − DEF/(DEF+K)`, `K=50` | `Assets/Scripts/Core/StatMath.cs:12`, `BalanceConfig.cs:11` |
| `aps = 0.02 × SPD` | `Assets/Scripts/Core/StatMath.cs:15`, `BalanceConfig.cs:18` |
| Damage resolution (raw × crit × mitigation × stall) | `Assets/Scripts/Core/SkillResolver.cs:41-49` |
| Gain rates (HP 2.5 / ATK 1.0 / DEF 0.8 …) | `Assets/Scripts/Core/BalanceConfig.cs:25` |
| Skill multipliers (2.8× active, 3.8× ult, 8 s cd) | `Assets/Scripts/Editor/SpeciesAssetGenerator.cs:24-36` |
| Effective-stat assembly | `Assets/Scripts/Core/StatMath.cs:39` |

## Proposed fix directions (NOT implemented — `balance.json` / data, Stage 3)

These are *directions*, not values — tuning is an explicit later step (Stage 3
balance pass) and out of scope for this analysis. To move P10 ≥ 30 s / P90 ≤ 90 s,
effective TTK must roughly **2.5–4×**. Levers, in `balance.json` unless noted:

- **Raise HP** (base and/or `gainRate[HP]`) so pools scale ahead of per-hit
  damage — the most direct lever.
- **Reduce offence**: lower `gainRate[ATK]`/`gainRate[INT]` and/or the active/
  ultimate `powerMultiplier`s (skill data), and/or lengthen active cooldowns.
- **Make DEF matter**: raise `K`, or scale `K` by league/level, so mitigation
  keeps pace with ATK (addresses the flat-K scaling risk directly).
- **Slow the clock**: lower `apsPerSpdLow` so hits land less often (also reduces
  tie frequency from the side-bias report as a side effect).
- **Re-anchor the sweep to the intended level band.** The failing sweep runs at
  level 5; verify the target 30–90 s is being measured at the right power level
  and re-tune from the histogram, per the balancing rule.

Any of these must be validated by re-running the sweep (Stage 2/3), never tuned
by feel — the balancing rule requires re-simulation after every multiplier change.

## Estimated impact

- Reaching the 30–90 s window is a **substantial rebalance** (~2.5–4× TTK), not a
  one-number nudge; expect several sweep-and-tune iterations (Stage 3).
- HP-scaling and DEF/K changes are the highest-leverage, lowest-risk first moves;
  skill-multiplier cuts are effective but interact with the "prepared beats
  untrained" and ultimate-budget (≤45% HP) constraints, so re-check those tests
  after any change.
- **Coupling:** lengthening battles also **reduces the mirror side-bias**
  (first-strike matters less over 30–90 s) — but does not eliminate it; the
  tie-break fix in `P1-1_SIDE_BIAS_ANALYSIS.md` is still required. Sequence the
  work so both are addressed together and the full sweep (criteria 2–4) is
  re-verified once.

## Scope note

Analysis only. No balancing performed, no numbers changed, no code edited, per
the task constraints.
