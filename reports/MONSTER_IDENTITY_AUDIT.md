# MONSTER IDENTITY AUDIT (Phase 1)

Character Direction pass. Presentation only; deterministic; 79/79 EditMode. Evidence:
`reports/img/showcase_v11/` (motion), `reports/img/uishowcase/2_teamselect.png` + grayscale
`reports/img/grayscale/roster_gray.png` (silhouettes, all 21 species).

## Method
Judged from captured frames: silhouette (grayscale roster), stance/pose (battle frames), and the
motion profile now driving each species (`CharacterProfile.cs`). Full motion is best in video; stance
+ silhouette are verified from stills.

## Finding
- **Silhouettes are already distinct** (CC0 sprites): grayscale roster shows bat / wolf / golem /
  ghost / jelly / kraken / phoenix / spider / treant / turtle / mantis / fire_lizard all readable
  without colour. The only look-alikes are **evolution families** (wolf↔dire_wolf, jelly↔slime,
  fire_lizard↔salamander, mantis↔blade_mantis, dragonling↔inferno_drake) — correctly similar by design.
- **Motion was the gap.** Before this pass, motion was role/element-generic — two Fire Bruisers moved
  identically. Now every species has a `CharacterProfile` (stance, lean, pace, elasticity, settle,
  wind-up, hit style, death style). Unlisted species get a hash-based micro-variation so no two match.

## Ranking (silhouette + motion)
| Tier | Species |
|---|---|
| **Distinct** | fire_lizard, turtle, jelly, wolf, phoenix, golem, ghost, kraken, treant, spider, bat, mantis, blade_mantis, dire_wolf |
| **Partially distinct** | dragonling, inferno_drake (dragon-ish → separated by hover), salamander (lizard → lean+fire), slime (blob → extreme elastic), squire, mushroom_beast, bee |
| **Generic** | none — all 21 now carry a distinct motion profile on top of a distinct sprite |

## Verified pose evidence (battle)
`showcase_v11/3_arena_3v3_08`: Golem sits **low + wide + planted** (tank), Fire Lizard **leans forward**
(aggressive), Dragonling rides **higher** (hover), Mantis reads as a small angular insect — four clearly
different creatures in one frame, confirmed again in grayscale (`grayscale/arena3v3_gray.png`).

## Interchangeable monsters remaining
Only within evolution families, and only by silhouette — their **motion now differs** (e.g. slime is
more elastic than jelly; salamander leans harder than fire_lizard via profile). No two species are
motion-identical.
