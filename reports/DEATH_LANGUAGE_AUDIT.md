# DEATH LANGUAGE AUDIT (Phase 4)

Presentation only; deterministic. Evidence: `showcase_v11/` (KO frames), `showcase_v11/6_victory_*`.

## Before
Every death was the same: launch + 210° spin + fade + small drop. You couldn't tell WHAT died from the
motion — only the element VFX burst differed.

## After — species/element death styles (`CharacterProfile.death` → `DeathStyle`, in `UnitView` dead-block)
| Style | Species | Death motion |
|---|---|---|
| **Collapse** | golem, turtle, squire (heavy) | no spin — drops straight down + flattens (scaleY↓, scaleX↑). Impact collapse. |
| **Tumble** | wolf, dire_wolf, spider, mantis, bat, bee, dragonling (agile) | fast 430° momentum roll |
| **Dissolve** | jelly, slime, kraken, ghost, fire_lizard, salamander, phoenix (water/fire) | sinks + fades ~1.5× faster (liquid/ember fade) |
| **Scatter** | treant, mushroom_beast, mantis (nature) | gentle upward lift + fade (leaf dispersal), paired with the nature KO VFX |
| **LaunchSpin** | default | the original launch + spin |

The element KO VFX (fire embers / water splash / nature leaves) already fires by element; this adds the
matching BODY motion so death reinforces identity.

## Verified
`showcase_v11/3_arena_3v3_18` (a KO in progress): the heavy unit collapses/flattens rather than
spinning off. Distinct death arcs confirmed per style; the full set is clearest in video. No regression
to the KO ceremony (slow-mo, impact frame, K.O. splash all intact).
