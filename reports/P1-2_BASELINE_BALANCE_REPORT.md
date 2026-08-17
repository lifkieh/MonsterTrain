# P1-2 — Baseline Balance Report

Read-only measurement of the **current** balance (post initiative-fix commit
`083d2cb`). **No numbers changed. `balance.json` untouched.** This is a snapshot
to inform the future Stage-3 balance pass — it does not tune anything.

## Method

- The real 12-species roster was rebuilt as headless `Core` data, replicating
  `SpeciesAssetGenerator` exactly (base stats from the Balance Sheet, growth
  tendency pyramids, the shared 10-skill pool + per-species assignments). It
  reads the live `balance.json` constants via `BalanceConfig` defaults.
- **Distribution sweeps** (random 3v3 vs random 3v3, both sides random from the
  12): 1,000 and 5,000 battles at level 5, plus 1,000 at level 1 for scaling.
- **Round-robin** for role / dominant-build ranking: homogeneous 3× species *i*
  vs 3× species *j*, all 132 ordered pairs × 30 seeds = 3,960 battles at level 5.
- Metrics parsed from the seed-deterministic event logs. Measurement harness was
  temporary and has been removed; committed code is unchanged.
- Caveats: growth is rolled per unit (variance smoothed by sample size);
  `speciesGainRates` is empty so all species share `defaultGainRates`; the
  round-robin uses homogeneous teams (isolates species strength, not synergy).

## 1. Duration distribution

| Sweep | P10 | P50 | P90 | mean | <15 s | hard-resolve | team-A win |
|---|---|---|---|---|---|---|---|
| L5, 1,000 | 13.9 s | 17.9 s | 38.2 s | 24.6 s | 16.2% | 2.4% | 49.9% |
| **L5, 5,000** | **13.6 s** | **17.9 s** | **39.2 s** | **24.5 s** | **17.0%** | **2.1%** | **49.9%** |
| L1, 1,000 | 15.4 s | 21.4 s | 54.5 s | 30.8 s | 4.5% | 3.9% | 50.8% |

- **Off-target.** Goal is P10 ≥ 30 s / P90 ≤ 90 s. Actual P10 ≈ 13.6 s and P50
  ≈ 18 s — the whole distribution sits **below** the 30–90 s window; ~17% of
  level-5 battles finish under 15 s (burst-anomaly territory).
- **Battles get FASTER as level rises** (L1 P50 21.4 s → L5 P50 17.9 s): offence
  out-scales HP/DEF with level — the scaling failure predicted in the balance
  sheet, confirmed.
- The 1,000 and 5,000 runs agree to 0.1 s / 0.0% — the sample is stable; 1,000 is
  sufficient going forward.
- team-A win ≈ 49.9% across the diverse roster confirms the initiative-bias fix
  (`083d2cb`) holds beyond the synthetic mirror test.

## 2. Win-rate distribution (round-robin, homogeneous 3v3, level 5)

| Species | Role | Win % |
|---|---|---|
| Bat | Assassin | **93.8** |
| Wolf | Bruiser | **92.9** |
| Goblin | Bruiser | **82.6** |
| Ghost | Mage | **75.0** |
| Fire Lizard | Mage | 63.2 |
| Dragonling | Bruiser | 56.2 |
| Bee | Support | 42.0 |
| Spider | Assassin | 39.7 |
| Mushroom Beast | Support | 27.3 |
| Slime | Tank | 18.2 |
| Golem | Tank | 7.1 |
| Turtle | Tank | **2.1** |

- **Extreme spread: 2.1% → 93.8%.** The target ("every sensible archetype 40–60%,
  nothing above ~65%") is badly missed. **Four species breach the 65% dominance
  flag** (Bat, Wolf, Goblin, Ghost); three tanks are near-unplayable.

## 3. Role performance

| Role | Win % |
|---|---|
| Bruiser | **77.2** |
| Mage | **69.1** |
| Assassin | **66.7** |
| Support | 34.6 |
| Tank | **9.1** |

- Offensive roles dominate; **Tanks collapse to 9.1%**, Support is weak (34.6%).
  The intended rock-paper-scissors of the five archetypes does not exist at these
  numbers.

## 4. Dominant builds

- **Fast + high-ATK wins everything.** Bat (SPD 20) and Wolf (ATK 24, SPD 14) top
  the table; Goblin (ATK 21, LUCK 14) third. This is the **SPD-stacking + ATK**
  risk the balance sheet named — realised.
- **The SPD brake never engages.** The diminishing-returns kink is at SPD 25
  (`spdKink=25`), but level-5 SPD tops out ~22 (Bee/Bat), so SPD scales **fully
  linearly** in practice — action economy is unchecked.
- **Tanks have no payoff.** DEF mitigates only ~24–34% at these values (`K=50`),
  and low-SPD tanks (Turtle SPD 5, Golem SPD 4) act rarely and can't kill, so
  survivability never converts to wins. Turtle at 2.1% is the clearest symptom.
- **Assassins split** (Bat 93.8 vs Spider 39.7): raw SPD+ATK (Bat) dominates;
  Spider's `slow_hex`/burst kit underperforms — skill kit matters less than raw
  action economy right now.

## 5. Skill usage (share of all skill activations, L5, 5,000 battles)

| Skill | Slot | per battle | % of actions |
|---|---|---|---|
| strike | Basic (ATK) | 8.55 | 37.8 |
| power_strike | Active (ATK) | 2.73 | 12.1 |
| spark_burst | Active (INT) | 2.39 | 10.6 |
| war_cry | Active (ATK buff) | 2.26 | 10.0 |
| zap | Basic (INT) | 2.07 | 9.2 |
| slow_hex | Active (SPD debuff) | 1.41 | 6.3 |
| savage_rend | Ultimate (ATK) | 1.07 | 4.7 |
| rally | Ultimate (ATK buff) | 0.90 | 4.0 |
| mend | Active (INT heal) | 0.67 | 3.0 |
| mind_blast | Ultimate (INT) | 0.54 | 2.4 |

- **ATK-scaling skills dominate** (strike + power_strike + savage_rend + war_cry
  ≈ 65% of all activations); INT skills trail — consistent with the offence-ATK
  meta and the ATK-heavy roster.
- **`mend` (heal) is nearly absent** (0.67/battle) — healing/support barely
  participates, matching Support's 34.6% role win rate.

## 6. Ultimate usage

| Sweep | battles with ≥1 ultimate | ult activations / battle |
|---|---|---|
| L5, 5,000 | 82.8% | 2.52 |
| L1, 1,000 | 95.5% | 3.48 |

- **~17% of level-5 battles end before any ultimate charges** (ultimates ready at
  15 s / rally 18 s; median battle 17.9 s). A core skill slot frequently never
  fires — a direct consequence of the short duration.
- At level 1 (longer battles) ultimates fire far more (95.5%), reinforcing that
  the duration problem, not the ultimate design, suppresses ultimate usage.

## 7. TTK

| Sweep | mean time-to-first-kill | mean unit death time | deaths / battle |
|---|---|---|---|
| L5 | 5.6 s | 13.7 s | 3.66 |
| L1 | 6.8 s | 16.4 s | 3.66 |

- First kill ~5.6 s (near the spec's 6–10 s squishy TTK), but the **whole 3v3
  collapses by ~18 s** — units die in quick succession once the first falls
  (focus-fire snowball). 3.66 deaths/battle means the loser is usually fully
  wiped (3) plus occasional winner losses.

## Summary of baseline problems (observations, not fixes)

1. **Battles too fast** — distribution centred ~18 s, below the 30–90 s window;
   ~17% under 15 s; worsens with level.
2. **Severe species/role imbalance** — 2.1%–93.8% spread; Tanks 9%, offence
   roles 67–77%; four dominance-flag species.
3. **SPD + ATK is the dominant build**; the SPD-25 brake never triggers at MVP
   levels; DEF (`K=50`) is nearly inert.
4. **Ultimates and healing under-fire** because battles end too early / support is
   too weak.

All four trace to the same root the duration analysis identified: offence
out-scales HP/DEF. **No changes were made** — tuning is the Stage-3 balance pass,
explicitly out of scope here. This report is the pre-tuning baseline to measure
that pass against.

## Success / constraints

- Ran 1,000 + 5,000 (+ a level-1 comparison) as requested; all metrics captured.
- `balance.json` and all gameplay numbers **unchanged**; the 7/7 gate suite still
  passes; the temporary probe was removed (committed code unchanged).
