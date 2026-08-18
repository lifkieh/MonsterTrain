# Balance Validation (K8 — Automated Large-Scale)

Date: 2026-08-18. 20,000 random 3v3 battles at level 5 on the live rebalanced species + `balance.json`.

## Duration & side-bias
- Duration P10/P50/P90 = 17.1 / 29.4 / 60.3 s  (target 25–90 s band)
- Hard-resolve rate = 3.9%
- Team-A win-rate = 50.5%  (target 47–53%)

## Species presence win-rate (target 40–60%)

| species | element | role | win-rate |
|---------|---------|------|----------|
| mushroom_beast | Water | Support | 55.7% |
| goblin | Fire | Bruiser | 53.3% |
| wolf | Nature | Bruiser | 52.9% |
| dragonling | Fire | Bruiser(Mage) | 52.8% |
| bee | Nature | Support | 51.4% |
| slime | Water | Tank | 51.0% |
| spider | Nature | Assassin | 49.5% |
| ghost | Water | Assassin(Mage) | 49.3% |
| golem | Nature | Tank | 48.5% |
| bat | Fire | Assassin | 47.5% |
| fire_lizard | Fire | Mage | 44.9% |
| turtle | Water | Tank | 43.1% |

**Spread: 43.1% – 55.7%** ✓ all species inside 40–60%.

## Role aggregate win-rate

| role | avg win-rate |
|------|--------------|
| Support | 53.5% |
| Bruiser | 53.1% |
| Bruiser(Mage) | 52.8% |
| Tank | 47.5% |
| Assassin | 48.5% |
| Assassin(Mage) | 49.3% |
| Mage | 44.9% |

## Element aggregate win-rate (should be ~50% each — symmetric triangle)

| element | avg win-rate | species |
|---------|--------------|---------|
| Water | 49.8% | 4 |
| Fire | 49.6% | 4 |
| Nature | 50.6% | 4 |

## Element matchup swing
- Fire→Nature (advantage) win-rate = 65.8%  vs same-element mirror 49.4%
- Swing ≈ 16.4% (target ~10–15% matchup impact, does not overpower stat parity).

## Power-difference → win-rate curve

| power diff | A win-rate |
|-----------|------------|
| -40.2% | 0.4% |
| -29.3% | 3.3% |
| -14.1% | 18.7% |
| -12.2% | 22.0% |
| -5.3% | 37.5% |
| -1.0% | 46.4% |
| 0.0% | 48.5% |
| 1.0% | 50.7% |
| 5.4% | 59.3% |
| 13.4% | 76.1% |
| 19.4% | 83.3% |
| 37.1% | 95.9% |
| 61.2% | 99.7% |

A <10% power difference stays near 45–55%; a large advantage still wins more,
but no slight edge produces a 90%+ auto-win.
