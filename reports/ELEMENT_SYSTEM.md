# Element System (K4)

Date: 2026-08-18. A symmetric elemental triangle layered on top of the K2–K3 stat
parity: matchups matter (~16% isolated swing) without overpowering equal-budget
parity (element aggregate win-rates stay ~50%).

## The triangle
```
Fire  >  Nature
Nature >  Water
Water >  Fire
```
Same element = neutral; no element = neutral.

## Damage multiplier
Applied to every damage hit (deterministic — no RNG, so the growth→crit→dodge→variance
contract is untouched):
- Attacker has advantage → damage × (1 + `elementAdvantage`)  = ×1.04
- Attacker at disadvantage → damage × 1/(1 + `elementAdvantage`) = ×0.962
- Neutral → ×1.0

`elementAdvantage = 0.04` (in `balance.json`). Implemented in
`StatMath.ElementMultiplier` and applied in `SkillResolver`.

## Assignments (4 per element — symmetric)
| Element | Species |
|---------|---------|
| **Fire** | bat, goblin, dragonling, fire_lizard |
| **Water** | ghost, turtle, slime, mushroom_beast |
| **Nature** | spider, wolf, golem, bee |

Each element spans multiple roles (an assassin, a bruiser/mage, a tank, a support),
so element and role are independent axes.

## Validation (20,000 random 3v3, level 5)
- **Element aggregate win-rate**: Fire 49.6% · Water 49.8% · Nature 50.6% — the
  triangle is balanced in aggregate (a symmetric cycle with equal representation).
- **Isolated matchup swing**: a Fire monster vs an otherwise-identical Nature
  monster wins **~66%** (vs a 49.4% same-element mirror) → a **~16%** swing.
  Meaningful (you feel the advantage) but comparable to a modest stat edge, so it
  never dominates the parity framework.

## Design intent
Element is a **tactical layer**, not a power layer: because the triangle is
symmetric and every element is equally represented across roles, no element is
stronger overall — the advantage only decides specific matchups, rewarding team
composition and counter-picking.
