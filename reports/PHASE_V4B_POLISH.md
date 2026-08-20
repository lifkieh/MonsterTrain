# PHASE V4B — POLISH (backlog + animation + store-prep) + PERFECTION RECHECK

Follows `PHASE_V4_IMPLEMENTATION.md` (vignette / brawl / cleanup). This pass executes three tracks in
one go, then rechecks from frames and revises the remaining defects. Presentation only — no sim /
determinism / save / balance / progression change. 79/79 EditMode green, Windows standalone builds.
Before = `reports/img/showcase_v3/`; After = `reports/img/showcase_v4/`.

## Track A — backlog visual polish
- **Portrait pips.** Flat square pips → framed portrait chips (team-coloured rounded frame + dark
  inset + the monster's actual portrait); dead → dims. The top HUD now shows WHO is on each side.
  `showcase_v4/3_arena_3v3_10`, `5_tag_3v3_19`. **Biggest single pro-look jump this pass.**
- **VFX bloom.** Bright element-tinted glow halo under every Impact / Ultimate / KO burst — approximates
  additive bloom with no runtime shader (player-safe). Visible behind launches/casts
  (`5_tag_3v3_19`, `2_arena_2v2_13`).
- **Damage-number weight.** Dark outline on all combat text + bigger crit/ult pop (1.85× vs 1.35×), so
  numbers read against bright VFX and crits dominate (`5_tag_3v3_19`: the "26" now reads heavy).
- **Foreground band.** Lightened (×0.58→×0.66), thinner bar, faint lit top edge — the bottom strip is
  no longer a dead black slab (`4_brawl_3v3_12`).
- **Fewer climax banners.** Dropped the redundant "SLAM!" (LAUNCH + crit word already read).

## Track B — animation liveliness (procedural, no new art)
Pushed idle motion so a single-frame sprite still reads ALIVE: a slow side-to-side **weight-shift
step**, deeper **breathing** (×0.03→×0.045), and a gentle **at-rest sway** rotation blended by the
idle factor. Motion only — the sprite is unchanged. (Honest limit: this masks the single-frame sprite
in *motion*; a paused still still shows one frame. True frame animation is an asset task.)

## Track C — store prep
- Curated 5 real hero frames → `reports/store/screenshots/` (from the showcase harness, reproducible).
- `reports/store/STORE_LISTING.md`: name, short/full description, feature bullets, screenshot map, and
  an **asset checklist** (icon ✓, screenshots ✓; feature-graphic / privacy-policy / data-safety still
  TODO) with an honest ship-readiness note.

## Perfection recheck (audit of the above, from frames) + revisions
Rechecked every scene. Two defects found and **revised in this pass**:
1. **"Speedlines" CC0 sheet read as a blue barcode box** (`showcase_v3/5_tag_3v3_19`). Dashes already
   spawn afterimage ghosts, so the sheet was redundant — dropped its opacity/size to a faint motion
   blur. Gone in `showcase_v4/5_tag_3v3_19`.
2. **Peak clash stacked concentric shockwave rings into a screen-filling mess** (`showcase_v3/
   2_arena_2v2_13`). Added a 0.16 s min-interval gate on the shockwave spawn → now a **single clean
   expanding ring** at the clash (`showcase_v4/2_arena_2v2_13`), impact preserved, noise gone.

No regressions introduced (pips render clean, no idle jitter, banners/damage readable).

## New scores (from frames only)
| Axis | V4 (before this pass) | V4B (after) | Δ |
|---|:--:|:--:|:--:|
| **Visual Quality** | 70 | **76** | +6 |
| **Combat Readability** | 79 | **84** | +5 |
| **Presentation** | 74 | **82** | +8 |
| **Polish** | 67 | **78** | +11 |

## Is it "perfect" now?
**No — and it cannot be, in a presentation-only scope.** Every presentation-fixable defect I could
find is now resolved: framing, depth, color, portraits, readability, bloom, damage weight, the barcode
artifact, and the clash ring-stack. What's left is **not presentation** and cannot be closed by code:
- **Single-frame monster sprites** — the one "unfinished" tell; needs an animator.
- **Procedural (not painted) backdrops** — needs an environment artist.

So this is the practical ceiling for the presentation layer: **~76–84 across axes**, a clean, readable,
atmospheric *prototype-plus*. Perfect/store-competitive requires the two asset investments above —
stated plainly, no optimism bias.
