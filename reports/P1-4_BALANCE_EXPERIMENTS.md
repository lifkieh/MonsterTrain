# P1-4 — Controlled Balance Experiments

Validation of the P1-3 strategy. **In-memory `BalanceConfig` overrides only — the
canonical `balance.json` on disk was never modified. No commits, no permanent
changes.** Temporary harness removed after this report.

Method: each experiment ran **5,000 random 3v3 battles** (level 5) + a **full
homogeneous round-robin** (132 ordered species pairs × 30 seeds = 3,960 battles).
BASE = canonical defaults (`k=50, spdKink=25`), included for reference.

## Experiment matrix

| Exp | Override | P10 | P50 | P90 | mean | <15 s | hard% | team-A | win spread |
|---|---|---|---|---|---|---|---|---|---|
| BASE | k50 / kink25 | 13.6 | 17.9 | 39.2 | 24.5 | 17.0% | 2.1% | 49.9% | 2.1–93.8 |
| **A** | **k=30** | 15.4 | 20.6 | 50.0 | 29.3 | 8.9% | 3.5% | 49.4% | 2.7–92.9 |
| **B** | **k=25** | 15.6 | 21.9 | 56.9 | 31.5 | 6.6% | 4.6% | 49.8% | 2.9–93.3 |
| **C** | **k=25, kink=15** | 15.4 | 23.1 | 61.1 | 32.9 | 4.3% | 4.6% | 49.7% | 2.9–98.8 |

## Role win rates (homogeneous round-robin)

| Role | BASE | A (k30) | B (k25) | C (k25/kink15) |
|---|---|---|---|---|
| Tank | 9.1 | 9.1 | 9.1 | 10.8 |
| Bruiser | 77.2 | 77.3 | 77.2 | **81.2** |
| Mage | 69.1 | 69.2 | 69.2 | 70.2 |
| Assassin | 66.7 | 66.4 | 67.6 | **61.7** |
| Support | 34.6 | 34.7 | 33.7 | **30.0** |

## Species highlights (BASE → C)

| Species | Role | BASE | C |
|---|---|---|---|
| Wolf | Bruiser | 92.9 | **98.8** ↑ |
| Bat | Assassin | 93.8 | **79.4** ↓ (spdKink bit) |
| Bee | Support | 42.0 | **27.1** ↓ (spdKink bit) |
| Goblin | Bruiser | 82.6 | 87.7 ↑ |
| Turtle | Tank | 2.1 | 2.9 (≈flat) |
| Golem | Tank | 7.1 | 8.0 (≈flat) |
| Slime | Tank | 18.2 | 21.5 |

## What the data says

1. **K works for duration.** Lower K monotonically lengthens battles: P50
   17.9 → 20.6 (A) → 21.9 (B) → 23.1 (C); sub-15 s collapses 17% → 4.3%. As
   predicted in P1-3 (correct direction: **lower** K). Low risk — `hardResolve%`
   only rises to 4.6% (still under the 5% ceiling, but now close — watch it).

2. **K does NOT fix role balance.** Tank stayed **9.1% in both A and B**; the win
   spread barely moved (2.1–93.8 → 2.9–93.3). The P1-3 expectation that lower K
   would lift tanks to ~25–35% is **not supported.** Reason: homogeneous tank
   teams lose on **offence** (Turtle ATK 12 / SPD 5, Golem SPD 4) — they can't
   kill and rarely act — and lowering K buffs *both* sides' survivability
   symmetrically, so the side that already out-damages still wins. DEF was never
   the tanks' problem; their ATK/SPD deficit is.

3. **spdKink=15 is a wash-to-negative for balance.** In C it nerfed the intended
   target (Bat 93.8 → 79.4) but **buffed the relative top** (Wolf 92.9 → 98.8,
   Bruiser role 77 → 81) and **hurt Support** (Bee 42 → 27, Support 34.6 → 30) —
   because Bee is a SPD-support and took the same hit as the rush assassins. Net
   spread got **worse** (max 98.8). Its only clear win is duration/anti-rush.

4. **Metric caveat.** The homogeneous round-robin measures species as *standalone
   teams*, which overstates the weakness of Tanks/Support — those are **mixed-comp
   role pieces**, not solo win-conditions. This is likely the wrong lens for the
   "40–60%" goal; a mixed-comp per-species contribution metric is needed.

5. **None of A/B/C reach the duration target.** Even C sits at P10 15.4 / P50 23.1
   — the median is still **below** the 30–90 s window and P10 is well under 30.
   K is **necessary but not sufficient**; the global Priority-3 lever (HP / aps)
   is still required.

6. **Initiative fix holds** under every config (team-A 49.4–49.9%).

## Recommendation — MODIFY

Not a clean "continue," not a "reject." Split by axis:

- **CONTINUE — K as the duration lever.** It works, direction confirmed, low risk.
  Recommend **K ≈ 25–30** (B or A). But treat it as step 1 of duration only, and
  **still apply Priority 3 (raise HP and/or lower `apsPerSpdLow`)** to move P10 ≥ 30
  / P50 into 30–90 — K alone tops out at P50 ~23 s. Keep `hardResolve%` under the
  5% ceiling as you slow further.

- **REJECT — K (and spdKink) as the role-balance fix.** The evidence kills the
  P1-3 hypothesis that lower K rebalances roles: Tank 9.1% unchanged. Do not spend
  the role-balance budget here.

- **MODIFY — the role-balance approach:**
  1. **Change the metric.** Re-measure species/role balance in **mixed comps**
     (per-species win contribution inside random teams), not homogeneous
     round-robin — the latter structurally condemns role pieces.
  2. **Drop spdKink=15 as a balance tool.** Keep it (if at all) only as a small
     anti-rush/duration aid; it worsens the spread and guts Support.
  3. **Tank/Support weakness is an offence/utility problem**, not a DEF problem:
     the real lever is their **ATK/SPD or kit** (e.g. per-species `speciesGainRates`
     overrides, or making `mend`/support skills matter) — a **per-species pass done
     AFTER** the global duration curve is set, never before.

**Suggested next step (not executed here):** re-run the experiment with **K=25 +
a Priority-3 duration lever** (e.g. `apsPerSpdLow` 0.02→0.015 or a base-HP bump),
measured with a **mixed-comp** balance metric, and leave spdKink at 25 for now.

## Constraints honored

`balance.json` on disk unchanged (overrides were in-memory only); no code kept
(temp harness removed); nothing committed. The 7/7 gate suite is unaffected.
