# PHASE V2 — COLOR & READABILITY

Presentation only. No sim / balance / save / progression change. Evidence: `reports/img/showcase_v2/`.

## Problems targeted ([SEEN])
- Whole game reads dim/murky; low contrast.
- Fighters sink into dark backgrounds.
- Ultimate/clash peaks over-layer into noise.

## Changes made

**Global grade — brighter, higher-contrast palettes.** Every biome's sky / ground / floor values were
lifted in `BattleArena.Theme` (this is the "color grade" done in-palette, since a UI canvas has no
post-processing stage). The battle now reads as a lit stage rather than a dim one — most obvious on
the previously near-black biomes (`showcase_v2/1_arena_1v1_02` water, `4_brawl_3v3_12` fire).

**Fighter separation.** Each fighter now renders a dark, slightly-enlarged copy of its own sprite
BEHIND the art — a silhouette outline that seats the creature and makes it pop off busy or dark
backdrops. Clearly visible on every fighter now (`3_arena_3v3_10`, `5_tag_3v3_06`). Cheap, exact to
the sprite outline, no rectangle.

**Ultimate/clash noise trimmed.** The expanding shockwave ring was thinned (max size `440 → 360`,
alpha `0.7 → 0.55`) so the climax keeps its punch without burying the fighters
(`1_arena_1v1_18` ultimate reads clean; `2_arena_2v2_13` clash is still intense but less blown-out).

## Readability reads (from frames)
- **Who is attacking / ultimate:** `ULTIMATE / Focus Strike` + radial focus is unmistakable
  (`1_arena_1v1_18`).
- **HP state:** green / yellow / red + danger pulse all legible; e.g. Jelly red vs Fire Lizard green
  (`3_arena_3v3_10`).
- **Combo:** `2/3/5/6 HITS!` legible, biome-tinted.
- **KO:** zoom-in + warm eruption makes the finish obvious in one frame (`3_arena_3v3_18`).
- **Who is winning:** team pips dim on KO (`4_brawl_3v3_12`: red 1 vs blue 2).

## Result
Battle is now readable in ~1 s: fighters are lit, outlined, and dominant; the four core reads
(attack / HP / combo / win) land. The remaining readability cost is at the very peak of a clash, where
concentric rings + multiple banners still stack (kept intentionally intense — see the review).
