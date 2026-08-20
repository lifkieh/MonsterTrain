# PHASE V5 — ANIMATOR + ENVIRONMENT ARTIST (procedural, no purchased/AI assets)

The two ceilings called out as "asset-bound" in every prior review — **single-frame monster sprites**
and **procedural/flat backdrops** — attacked with CODE, not bought or AI-generated art. Presentation
only: no sim / determinism / save / balance / progression change. 79/79 EditMode green, Windows
standalone builds. Before = `reports/img/showcase_v4/`; After = `reports/img/showcase_v5/`.

## The animator — `DeformSprite.cs` (free-form mesh deform)
The monster sprites are single non-readable PNGs, so pixel-warping is fragile. Instead each fighter is
drawn on a **5×7 grid mesh whose vertices are pushed every frame** by procedural fields — chest-rise
breathing, idle upper-body sway, an attack bend (the body leans into the swing), an impact ripple, and
a travelling limb-ripple at rest. Feet stay planted; the upper body moves. Body + separation-outline +
hit-flash are three deform copies sharing ONE shape so they wobble together.

**Result (from frames):** the silhouette genuinely changes frame-to-frame — `showcase_v5/1_arena_1v1_02`
vs `_05` show Fire Lizard in two different stances and Jelly bending into its hit. The "paused = one
dead frame" tell is **mitigated**: every still is a different organic pose, and it animates live.
Honest limit: this is *free-form deformation* (whole-body warp), not redrawn/articulated frames — it
reads alive but a purist still sees it's warp-based, not hand-animated.

## The environment artist — `PaintedBackdrop.cs` (baked per-biome texture)
One texture baked per biome: a graded sky, a hazy sun, soft Perlin clouds, and two **atmospheric
mountain ridges** (the far ridge fades toward the sky = aerial perspective) over a ground band. Water
skips ridges for a flat sea horizon. Replaces the flat sky-band gradient, the silhouette layers, and
the reused forest photo.

**Result (from frames):**
- **Fire** (`showcase_v5/4_brawl_3v3_12`) — layered volcanic ridges + haze over a warm sky. Reads as a
  painted volcanic basin.
- **Water** (`1_arena_1v1_02`) — graded sky, sun glow, clean flat sea horizon.
- **Nature** (`5_tag_3v3_06`) — layered green hills + haze, consistent with the others.

**Parity achieved:** previously only Nature (a real photo) looked finished; now **all three biomes are
painted, layered, and atmospheric at one quality.** Honest limit: procedural-painted (noise + ridges),
not hand-painted-artist detail — and Nature traded the photo's tree/cloud detail for set consistency.

## Scores (from frames only)
| Axis | V4B | V5 | Basis |
|---|:--:|:--:|---|
| **Visual Quality** | 76 | **85** | painted backdrops + animated bodies = a real tier up |
| **Combat Readability** | 84 | **85** | unchanged-high; backdrops cleaner, deform doesn't hurt reads |
| **Presentation** | 82 | **88** | consistent painted locations, live fighters, framed HUD |
| **Polish** | 78 | **84** | clean; residual = warp-not-frames, proc-not-hand-painted |
| **Animation Quality** | ~30 | **~68** | free-form deform alive; not true articulated frames |

## Honest verdict
This is the biggest single jump of the whole effort because it finally moves the two things I kept
saying code couldn't touch — and it moved them **with code**. The game now presents as animated
fighters in painted, atmospheric locations, consistent across biomes.

It is **not** equal to Monster Legends / Summoners War / Epic Seven / Raid: those use hand-drawn
articulated frame animation and hand-painted layered art. Ours is **procedural** animation + backdrop —
a very strong *prototype-plus-plus* that closes most of the perceived-quality gap, but the base monster
pixels are still CC0 and the motion is warp-based. The remaining gap is now **quality of the base
sprite art itself** (detail/resolution), which procedural deform can't add — that is the one true
asset-budget item left.
