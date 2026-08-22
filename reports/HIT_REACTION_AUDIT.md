# HIT REACTION AUDIT (Phase 3)

Presentation only; deterministic. Evidence: `showcase_v11/`.

## Before
All monsters took damage identically: a head-snap + white flash + uniform body wobble (`UnitView`
Anim.Hit), scaled only by crit. Damage did **not** reveal character.

## After — species-specific hit language (`CharacterProfile.hit` → `HitStyle`)
| Style | Species | Reaction |
|---|---|---|
| **Stiff** | golem, treant | body barely flinches (mag ×0.4) — reads immovable |
| **Ripple** | jelly, slime, kraken, ghost | big elastic body wobble (×1.35 × elastic) — overreacts |
| **Slide** | turtle | reduced deform + slow settle → slides rather than snaps |
| **AirWobble** | phoenix, bat, bee, inferno_drake, dragonling | vertical air-bob instead of a ground recoil |
| **Recoil** | wolf, mantis, fire_lizard, … | sharp head-snap + push (default) |

Elasticity (`_cElastic`) scales the wobble, and settle rate (`_cSettle`) makes heavy creatures absorb
and slow-settle while agile ones snap back — so the *same* incoming hit looks different per creature.

## Verified
`showcase_v11/3_arena_3v3_13`, `_18` (mid-combat hits): Golem stays planted while lighter units wobble/
slide on the same beats. Full ripple/slide dynamics are clearest in video (on-device). No regression:
the hit flash + damage number still read.
