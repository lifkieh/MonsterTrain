# VISUAL DIRECTOR — PHASE V4 AUDIT

Judged **only** from frames the CURRENT build rendered: `reports/img/showcase_v2/` (build 639530b,
after V1–V3). Deterministic capture — a re-run produces identical frames, so these ARE the fresh
current-build evidence. No source assumptions except to validate a visible finding. Presentation-only
scope. Does **not** repeat V1–V3 work.

Baseline scores carried in: Visual 62 · Readability 73 · Presentation 66 · Polish 58.

---

## Category scores (from frames)

| Cat | Score /10 | Note (evidence) |
|---|:--:|---|
| **A. Composition** | 7 | Eye lands on the fighters; they're dominant + outlined. Costs: grey "sun" blob competes top-left (`2_arena_2v2_05`); dark foreground strip + flat empty sky still eat frame. |
| **B. Combat Readability** | 7 | Winning (pips), dying (HP red+pulse), ultimate (`1_arena_1v1_18`), damage numbers, team lead — all read in ~1 s. **Breaks at the brawl opening clash** (`4_brawl_3v3_03`) where 6 units + every bar/label/banner pile on one point. |
| **C. Impact** | 7 | Full tier ladder (hit/crit/launch/slam/KO) with shake/hitstop/zoom/eruption (`3_arena_3v3_18`). Held back by flat VFX — no bloom/additive glow, so effects read a tier below authored. |
| **D-Fire** | 7 | Warm volcanic basin, layered ridge, lava (`4_brawl_3v3_12`). Reads as a place. |
| **D-Water** | 6 | Bright sea, but the grey "sun" blob + faint ripple-ring decals cheapen it (`1_arena_1v1_02`, `2_arena_2v2_05`). Weakest biome. |
| **D-Nature** | 8 | Real layered photo depth; strongest (`5_tag_3v3_06`). |
| **E. Brawl** | 5 | Mid-fight fans out fine (`4_brawl_3v3_12`), but the **opening charge piles all 6 + label soup** (`4_brawl_3v3_03`). Biggest single readability hole. |
| **F. Tag** | 7 | Duel is dramatic + rotation obvious (Golem promoted, dominant, `5_tag_3v3_19`); reserves benched/dim. Stronger composition than Brawl. Cost: reserves clip the screen edge. |
| **G. Mobile / Store** | 5 | Brutally: single-frame sprites + procedural backdrops read below Monster Legends / Summoners War / Epic Seven / Raid. Framing + color are now competitive; **animation + painted art are not**. |

---

## Top 20 Remaining Problems

| # | Problem | Screenshot | Why it hurts | Impact | Effort | Risk |
|---|---------|-----------|--------------|:--:|:--:|:--:|
| 1 | Brawl opening charge piles all 6 units on one point | `4_brawl_3v3_03` | The clash frame is an unreadable stack of bodies | 9 | M | Med |
| 2 | HP bars + name labels collide in the pile | `4_brawl_3v3_03` | "Kraken/Turtle/Mantis/Treant" + bars overlap into soup | 8 | M | Med |
| 3 | Single-frame sprites (static when paused/zoomed) | `3_arena_3v3_18` | The #1 "unfinished" tell vs competitors | 8 | L | Med |
| 4 | Water "sun" reads as a flat grey placeholder blob (square core) | `2_arena_2v2_05`, `1_arena_1v1_19` | Looks like an unfinished proc shape near the HUD | 6 | S | Low |
| 5 | Faint ground decals (rings/ripples/dashes) read as noise | `1_arena_1v1_02` | Low-level floor clutter, not grounding | 6 | S | Low |
| 6 | Whole frame is flat — no vignette / photographic depth | all | Reads as "UI on a gradient", not a lit scene | 7 | S | Low |
| 7 | Reserve units clip the screen edges | `2_arena_2v2_05`, `5_tag_3v3_19` | Golem/Dragonling cut in half — looks broken | 6 | S | Low |
| 8 | Team pips are flat boxy squares | all top HUD | Cheap HUD; no portraits/frames | 6 | M | Low |
| 9 | VFX are flat — no bloom/additive glow | `2_arena_2v2_13` | Effects read a tier below authored spritesheets | 6 | M | Med |
| 10 | Peak-clash over-layers (rings + 2–3 banners) | `2_arena_2v2_13` | Spectacular but noisy at the loudest moment | 6 | M | Med |
| 11 | Bottom foreground band is a dark dead strip | `1_arena_1v1_19` | ~1/6 of frame is empty dark chevrons | 5 | S | Low |
| 12 | Bright cyan ground-lip lines look harsh/gamey | `1_arena_1v1_02` | Two saturated cyan horizontals break realism | 4 | S | Low |
| 13 | Flat empty upper sky in non-nature biomes | `2_arena_2v2_05` | Big dead gradient above the fighters | 5 | M | Low |
| 14 | Damage numbers small/plain vs the impact | `4_brawl_3v3_03` | "+46/+50" don't feel expensive | 5 | S | Low |
| 15 | Transient VFX read as odd boxes (blue speed-line barcode) | `5_tag_3v3_19` | A blue striped rectangle looks like an artifact | 4 | M | Med |
| 16 | Grounding shadow weak under the bigger sprites | `1_arena_1v1_02` | Fighters read slightly floaty | 5 | S | Low |
| 17 | Element cast rings can obscure the caster | `5_tag_3v3_06` | Big green ring covers the wolf's read | 4 | S | Low |
| 18 | Banner hierarchy weak (combo/skill/crit compete) | `4_brawl_3v3_03` | Everything same weight → nothing leads | 5 | M | Med |
| 19 | Launched units become unreadable smears | `5_tag_3v3_19` | Spin+blur hides the creature (acceptable juice, low pri) | 3 | S | Low |
| 20 | No consistent HUD safe-area (sun/pips overlap) | `2_arena_2v2_05` | Scene elements drift into the HUD band | 4 | S | Low |

---

## Top 10 Highest-ROI Fixes (ranked — only score-movers)

1. **Screen-space vignette + subtle grade overlay.** One additive/multiply overlay pair on the canvas
   → instant photographic depth on every frame. (S / Low / Impact 7) — moves Visual + Presentation.
2. **Fix the brawl opening + label collision.** Stagger the charge collision points and, when a
   fighter is crowded, stagger its HP-bar height by slot + fade its name → kill the pile-soup.
   (M / Med / Impact 8) — moves Readability (the worst hole).
3. **Cleanup pass: remove the grey sun, thin/soften the ground decals, soften the cyan lips, pull
   reserves fully on-screen.** Removes the most-visible "unfinished" tells in one sweep.
   (S / Low / Impact 6) — moves Polish + Presentation.
4. **Framed portrait chips for team pips** (M / Low / Impact 6).
5. **Additive bloom/glow on element + impact VFX** (M / Med / Impact 6).
6. **Bigger weight-graded damage numbers + crit/ult hierarchy** (S / Low / Impact 5).
7. **Enrich/lighten the bottom foreground band** (S / Low / Impact 5).
8. **Stronger contact shadow + landing dust (grounding)** (S / Low / Impact 5).
9. **Cap simultaneous banners/rings at the climax** (M / Med / Impact 5).
10. **Break up the empty upper sky** (light cloud/haze bands) (M / Low / Impact 5).

---

## Expected Gain (implementing the top 3 only)

| Axis | Current | After (est.) |
|---|:--:|:--:|
| Visual Quality | 62 | **70** |
| Combat Readability | 73 | **80** |
| Presentation | 66 | **74** |
| Polish | 58 | **68** |

The single-frame-sprite ceiling (#3 in the problem list) caps how far *any* presentation-only work can
push — that one is an animator's job, out of this scope.

---

## Implementation decision

Implement **only the top 3 ROI fixes**, isolated + reviewable:
1. Vignette + grade overlay.
2. Brawl opening / label de-collision.
3. Cleanup (sun, decals, cyan lips, reserve clamp).

Everything else stays in this backlog for a later phase.
