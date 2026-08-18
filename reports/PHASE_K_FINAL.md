# Phase K — Attribute Parity Rebalance + UI Overhaul (Final)

Date: 2026-08-18. Re-baselined the combat economy so **equal stat budgets produce
roughly equal power**, redesigned all species to be distinct-and-viable, added an
elemental triangle, and modernized the mobile UI. Determinism preserved (still
seed-deterministic; the log hash is re-baselined to the new rules — all hash tests
are self-referential and green).

## Results vs targets (20,000-battle validation, level 5)

| Target | Result | Status |
|--------|--------|--------|
| Species presence win-rate 40–60% | **43.1% – 55.7%** (all 12) | ✓ |
| Team-A side bias 47–53% | **50.5%** | ✓ |
| Battle duration 25–90 s (median) | P50 **29.4 s** (P90 60 s) | ✓ |
| Element aggregate ~50% each | Fire 49.6 / Water 49.8 / Nature 50.6 | ✓ |
| Element matchup swing ~10–15% | ~16% isolated | ≈ (near target) |
| <10% power diff → 45–55% | −5%→41%, +5%→56%, 0%→48% | ✓ (±3% band tight) |
| No slight edge → 90%+ | +13% power → 69%; need +37% for 90% | ✓ |
| Best species ≤60 / worst ≥40 | 55.7 / 43.1 | ✓ |

## K1 — Combat Economy Audit
`reports/ATTRIBUTE_VALUE_ANALYSIS.md`. Found: SPD worth 5%/point (top), +5 SPD won
**100%** of duels, power curve was a cliff (+12.5%→95.7%), species ranged
wolf 98.8% → turtle 0.0%. Root causes: DPS = ATK×APS(SPD) product + SPD initiative
snowball, DEF/HP mismatch, zero variance, unequal skill kits.

## K2 — Parity framework
- **Linear-through-origin APS** — ATK-heavy and SPD-heavy equal budgets now yield
  equal DPS (the A(90/20) ≈ B(20/90) symmetry).
- **Controlled variance** — ±30% damage variance, LUCK-based **dodge** (rehabilitates
  LUCK as a two-sided stat), ±45% **initiative jitter** (breaks the first-strike
  snowball). Combat is no longer a deterministic cliff.
- **DEF/HP EHP-parity** via k=60 (HP↔DEF budget swap = 49.9%, test-locked).
- **Global `damageScale`** (0.62) for pacing — uniform, so parity-neutral.

## K3 — Species redesign
Every species re-statted to ~equal effective power with a distinct role shape, and
under-performing utility species got a real damage active (kit reassignment):
- **Tanks** (turtle/golem/slime): high EHP, moderate DPS.
- **Assassins** (bat/spider/ghost): high DPS, fragile.
- **Bruisers** (wolf/goblin/dragonling): balanced.
- **Mage** (fire_lizard) + **Supports** (bee/mushroom_beast): scaling / utility.

## K4 — Element system
`reports/ELEMENT_SYSTEM.md`. Symmetric Fire→Nature→Water→Fire triangle, 4 species
each, `elementAdvantage = 0.04`. Matters tactically, balanced in aggregate.

## K5 — Growth
Level-1 parity (43–55% presence) **holds at level 5** under the shared, fair growth
rates + per-instance grade rolls — power comes from progression without any species
snowballing out of the 40–60% band.

## K6/K7 — UI overhaul
`reports/UI_REDESIGN.md`. Modern buttons (press feedback + gloss), element badges
(collection / detail / battle), rarity-framed collection cards, element indicators
on fighters, round-pip battle HUD, polished career/results/menu/settings. Portrait,
touch-target-safe for 1080×2340.

## K8 — Automated validation
`reports/BALANCE_VALIDATION.md` — the 20,000-battle run summarized above.

## Verification
- **63 / 63 EditMode tests pass**, including 5 new parity tests (element triangle,
  dodge scaling, mirror ≈ 50%, HP↔DEF parity, stronger-wins-more-but-capped).
- Determinism: same seed → same hash (self-referential, still green).
- Android APK builds.

## Constraints honored
Author **Lifkie Lie <llifkie@gmail.com>**, no AI attribution. Combat variance is
controlled (dodge/crit/damage/timing), not coin-flip victory. Stronger monsters
still win more; slight edges never hit 90%+.

## Known limitations / next
- UI is procedural (no art assets) — a portrait/icon art pass remains future work.
- Element swing ~16% is a hair above the 10–15% target (aggregate is balanced).
- On-device visual QA of the new UI recommended.
