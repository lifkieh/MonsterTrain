# Attribute Value Analysis (K1 — Combat Economy Audit, PRE-rebalance)

Date: 2026-08-18. Headless measurement of the **original** combat formulas (before
the K2–K5 parity rework), from the live species + `balance.json` at the time.
This is the diagnosis that motivates the rebalance; the post-rebalance numbers are
in `BALANCE_VALIDATION.md`.

Original model: damage = ATK × mult × crit × Mitigation(DEF) × stall, with
`Mitigation = 1 − DEF/(DEF+50)`, `APS = 0.02·min(SPD,25) + 0.01·max(0,SPD−25)`
(cap 1.0), crit from LUCK. No damage / dodge / timing variance.

## Reference monster `HP=100 ATK=30 DEF=20 SPD=20 INT=20 LUCK=20`
- EHP = 140.0 (HP × (DEF+k)/k, k=50)
- DPS = 12.60 (ATK × crit-avg × APS(SPD))
- Power = EHP × DPS = 1764

## Analytic marginal value of +1 of each stat
| Stat | ΔEHP | ΔDPS | ΔPower % |
|------|------|------|----------|
| HP | 1.40 | 0.000 | 1.0% |
| ATK | 0.00 | 0.420 | 3.3% |
| DEF | 2.00 | 0.000 | 1.4% |
| SPD | 0.00 | 0.630 | **5.0%** |
| LUCK | 0.00 | 0.030 | 0.2% |

SPD is worth 5% per point — the single most valuable stat — while LUCK is near-worthless.

## Empirical marginal value (duel win-rate of ref+Δ vs ref, 1500 seeds)
| Stat | +5 | +10 | +20 |
|------|----|-----|-----|
| HP | 50.8% | 79.9% | 97.1% |
| ATK | 97.9% | 98.7% | 100.0% |
| DEF | 50.8% | 79.9% | 97.1% |
| SPD | **100.0%** | 100.0% | 100.0% |
| LUCK | 52.1% | 53.2% | 56.9% |

A mere **+5 SPD wins 100%** of duels and +5 ATK wins 98% — combat is a
near-deterministic cliff: the marginally-better monster wins ~always.

## Power-difference → win-rate curve (the cliff)
| power diff | win-rate |
|-----------|----------|
| −5.3% | 50.8% |
| 0.0% | 50.8% |
| +12.5% | **95.7%** |
| +19.6% | 96.9% |
| +37.6% | 99.7% |
| +62.1% | 100.0% |

A +12.5% power edge already wins 95.7% — exactly the "slight advantage → 90%+"
problem the rebalance must eliminate.

## Original species round-robin (1v1, level 1)
| species | win-rate | Σ stats |
|---------|----------|---------|
| wolf | 98.8% | 164 |
| ghost | 81.5% | 144 |
| goblin | 76.2% | 164 |
| bat | 74.5% | 138 |
| fire_lizard | 71.5% | 155 |
| dragonling | 61.2% | 148 |
| spider | 45.2% | 145 |
| slime | 27.6% | 178 |
| mushroom_beast | 27.3% | 169 |
| golem | 18.5% | 195 |
| bee | 18.2% | 131 |
| turtle | **0.0%** | 205 |

Turtle has the **largest** stat budget (205) yet a **0%** win-rate: raw stat totals
are meaningless because value concentrates in SPD/ATK, and tanks/supports carry
utility kits with no burst.

## Root causes
1. **DPS = ATK × APS(SPD) is a product** — ATK and SPD each multiply the other's
   value, and SPD additionally decides initiative and kill order (a snowball EHP
   bonus). SPD dominates.
2. **DEF diminishes (k/(DEF+k)) while HP is linear**, so a fixed budget buys
   different survivability by split.
3. **No damage/dodge/timing variance** — a deterministic cliff: the slightly-better
   monster wins ~100%, producing the 90%+ outliers and non-viable tanks/supports.
4. **Skill kits are unequal** — power_strike/savage_rend (ATK burst) and INT nukes
   massively out-damage buff/heal/debuff utility kits, independent of stats.

## Fix direction (implemented in K2–K5, validated in BALANCE_VALIDATION.md)
- Linear-through-origin APS (ATK-heavy ≈ SPD-heavy equal budgets).
- Controlled variance: ±30% damage variance, LUCK-based dodge, ±45% initiative
  jitter — smooths the cliff, rehabilitates LUCK, breaks the SPD snowball.
- DEF/HP EHP-parity via k=60; global `damageScale` for pacing.
- Value-balanced species statlines + kit reassignments (tanks/supports gain real
  damage actives) so every species reaches ~equal effective power.
- Symmetric elemental triangle (Fire→Nature→Water→Fire).
