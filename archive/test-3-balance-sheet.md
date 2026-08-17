# Test run — eval 3: balance-sheet (iteration 1, skill v4)

## Active Role

Both — Creative Director sets the direction (all five archetypes must stay
viable; battles won by preparation), Lead Architect provides numbers and
verification.

## Objective

A first-pass level-1 balance sheet for the 12 MVP monsters that lands typical
3v3 battles in the 30–90 second window, with the risks named before they're
discovered the hard way.

## Assumptions (state these before trusting any number)

1. **Damage per hit** = ATK × (1 − DEF/(DEF+K)), mitigation constant K = 50
   (in `balance.json`).
2. **Attack rate** = SPD × 0.02 attacks/sec (SPD 10 → one attack per 5 s).
3. **Crit** = LUCK × 0.5% chance, 1.5× damage. Skills scale on INT with
   2.5–4× basic-hit budgets on 6–12 s cooldowns.
4. Growth grades below are **species tendencies** — the *typical* roll from each
   species' weighted GrowthProfile. Actual monsters roll per instance, so a
   player's Wolf can beat these expectations. Never print these as fixed grades.

## Design Decision — roster sheet (tendencies, not fixed grades)

| # | Monster | Role | HP | ATK | DEF | SPD | INT | LUCK | Growth tendency (HP/ATK/DEF/SPD/INT/LUCK) |
|---|---------|------|----|-----|-----|-----|-----|------|---------------------------------------------|
| 1 | Slime | Beginner tank | 120 | 16 | 18 | 8 | 10 | 6 | A/C/B/D/C/B |
| 2 | Wolf | Bruiser | 100 | 24 | 12 | 14 | 6 | 8 | B/A/C/B/D/C |
| 3 | Bat | Speedster | 70 | 18 | 8 | 20 | 10 | 12 | D/B/D/S/C/B |
| 4 | Mushroom Beast | Support (INT heal/debuff) | 110 | 12 | 14 | 7 | 20 | 6 | B/D/B/D/A/C |
| 5 | Spider | Assassin | 75 | 20 | 9 | 17 | 14 | 10 | D/B/D/A/B/B |
| 6 | Goblin | Lucky bruiser | 95 | 21 | 12 | 13 | 9 | 14 | C/B/C/B/D/A |
| 7 | Turtle | Hard tank | 150 | 12 | 26 | 5 | 8 | 4 | A/D/S/D/C/D |
| 8 | Ghost | Mage | 80 | 8 | 10 | 12 | 24 | 10 | C/D/C/B/S/B |
| 9 | Bee | Fast support | 65 | 16 | 7 | 22 | 12 | 9 | D/C/D/S/B/B |
| 10 | Golem | HP tank/bruiser | 140 | 20 | 22 | 4 | 6 | 3 | S/B/A/D/D/D |
| 11 | Fire Lizard | Hybrid caster | 90 | 18 | 11 | 11 | 18 | 7 | C/B/C/C/A/C |
| 12 | Dragonling | Late bloomer | 85 | 17 | 12 | 10 | 16 | 8 | B/A/B/B/A/B |

Per-monster intent, briefly: Slime is the forgiving starter body. Wolf teaches
focus-fire. Bat/Bee bracket the SPD extreme (Bat kills, Bee enables). Mushroom
and Ghost split INT into sustain vs burst. Spider is the glass knife; Goblin the
crit gamble; Turtle vs Golem are DEF-tank vs HP-tank so anti-tank answers differ.
Fire Lizard blends ATK/INT for flexible builds. Dragonling has modest bases but
the best growth ceiling — the "raise your future ace" fantasy, straight from the
core pillar.

## Expected TTK and battle duration (the analysis behind the table)

Worked example: three mid attackers (~ATK 20, SPD 13) focus a Wolf (HP 100,
DEF 12). Per hit: 20 × (1 − 12/62) ≈ 16.1. Team rate ≈ 3 × 0.26 = 0.78 hits/s →
~12.6 dps → **TTK ≈ 8 s** before skills/crits (~6–7 s with). Same squad into
Turtle (HP 150, DEF 26): per hit ≈ 13.2, → **TTK ≈ 15 s**, stretching to ~20 s+
behind Mushroom sustain.

Sequential focus-fire projection: squishy-heavy teams wipe in ~25–40 s;
tank-cored teams ~55–80 s; double-tank + support stalls threaten the 90 s
ceiling (see risks). **Expected typical duration: ~35–75 s** — inside target,
verified properly only by the headless simulator: 1,000 seeded 3v3s across
random level-1 comps, assert P10 ≥ 30 s and P90 ≤ 90 s, publish the histogram.

## Expected win-rate distribution

Mirror comps: 50% by construction (seeded sim confirms no side bias). Target for
league-appropriate AI: every sensible archetype comp within **40–60%**; any comp
above ~65% is a dominance flag. First-pass concern list: Bat+Spider+Bee rush
(likely >60% vs slow comps) and Turtle+Golem+Mushroom stall (wins by boredom).

## Dominant build risks

1. **SPD stacking** — action economy is multiplicative with ATK, so SPD is
   secretly the best stat. Mitigation lives in `balance.json`: diminishing
   attack-rate curve above ~SPD 25, and stat-point costs can rise per point later.
2. **Stall comps** — two tanks + healer can exceed 90 s. Mitigation: cap
   effective mitigation at ~55%, or +5%/10 s ramping damage after 60 s ("sudden
   death"), data-only.
3. **LUCK crit variance** — undermines "I won because I prepared correctly."
   Keep crit ceiling ~30% and multiplier 1.5× so preparation, not dice, decides.
4. **Ultimate snowball** — if ultimates one-shot, INT burst invalidates HP
   builds; keep single-skill damage ≤ ~45% of an average HP pool.

## Scaling risks

1. Flat +50 K-constant means DEF falls off as ATK grows with level — revisit K
   per league tier (data-only).
2. 3 allocation points/level vs growth gains (0.6–1.5/stat/level): at high
   levels growth dwarfs allocation, weakening the player-agency pillar —
   consider allocation scaling by level bracket.
3. INT double-dips if skill power also scales with level — pick one scaling
   channel now.
4. League opponent curves in `balance.json` must outpace training gains slightly,
   or Master league trivializes.

## Scope Classification

MVP Safe — numbers and curves only; every mitigation named above is a
`balance.json` edit, no code.

## Recommendation

Adopt this sheet as v0 of `balance.json`, build the headless simulator
*immediately after* (Build Phase 1 already requires the damage system), and
re-tune from the duration histogram rather than from feel. Re-run the sweep
whenever any multiplier changes.
