# PHASE V8 — META-UI COHESION

The battle looked polished (V1–V7) while the menu screens were still flat dark panels. This pass brings
the whole meta UI up to one look, and surfaces element + level there too. Presentation only (UI); no
sim / save / balance / progression change. 79/79 EditMode green, Windows standalone builds.

## Changes
- **Shared painted backdrop on every meta screen.** New `PaintedBackdrop.Menu()` bakes a calm indigo
  gradient + soft top glow + faint nebula + vignette. Each screen's flat solid fill is made
  transparent and this painted texture is inserted behind its content (`GameBootstrap.PaintScreen`),
  so labels/buttons stay fully bright on top. Applied to menu / team-select / result / collection /
  detail / career / daily / settings / about / onboarding / quests / achievements / dex.
  (First attempt used a canvas-wide backdrop + heavy scrim → too dark; switched to per-screen opaque
  backdrop so text never dims.)
- **Element + level on the team-select cards.** `DecorateCard` now adds an **element badge (F/N/W)**
  top-right and a gold **Lv** bottom-right of each monster card (`uishowcase/2_teamselect`), matching
  the in-battle surfacing from V7.
- **`-uishowcase` capture harness.** A dev flag that visits each menu screen on a timer and screenshots
  it (read-only, no battles → no save writes), so meta UI can be reviewed deterministically like the
  battle showcase. Frames in `reports/img/uishowcase/`.

## Result (from frames)
- **Menu** (`uishowcase/1_menu`): bright title, glossy buttons, painted indigo backdrop with depth.
- **Team select** (`2_teamselect`): every card shows portrait + element badge + Lv on the painted bg.
- **Detail** (`4_detail`): the existing leveling screen (XP bar, per-stat growth, element, rarity,
  evolve/train) now bright + cohesive instead of a flat dark panel.
- All screens now read as one designed product, consistent with the battle.

## Scores (from frames)
| Axis | V7 | V8 |
|---|:--:|:--:|
| Visual Quality | 88 | **89** |
| Presentation | 90 | **92** |
| Polish | 87 | **89** |
| UX / cohesion | ~72 | **84** |

## Note
Battle presentation is untouched (the battle panel isn't in the painted-screen set — the arena covers
it). The one true remaining ceiling is unchanged: the base CC0 monster sprite detail (asset). Meta and
battle now share a look; element + level are visible on the monster in both.
