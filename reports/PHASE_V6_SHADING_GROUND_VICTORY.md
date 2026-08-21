# PHASE V6 — SPRITE SHADING · GROUND COHESION · VICTORY HERO SCREEN

Top-3 ROI from the V5 backlog. Presentation only. 79/79 EditMode green, Windows standalone builds.
Before = `reports/img/showcase_v5/`; After = `reports/img/showcase_v6/`.

## 1. Sprite shading (the "premium" pass)
`DeformSprite` now bakes shading into vertex colours — the UI shader is `texture × vertexColour`, so
per-vertex tinting is free. Each body vertex gets a **top-light + bottom/side ambient-occlusion** ramp
(darker toward the feet + edges = rounded volume) times a **biome light tint** (fire warm, water cool,
etc.). Outline/flash stay flat. Result (`showcase_v6/1_arena_1v1_19`): fighters have lit form and sit
in the scene's lighting instead of reading as flat cut-outs. This directly attacks the "base art looks
CC0/flat" gap without new art.

## 2. Ground + foreground cohesion
- The abstract dark foreground **diamonds → rounded lit boulders** (`Rock`: a disc silhouette + a soft
  lit top cap), so the lower frame reads as terrain, not abstract shapes.
- A soft **horizon seam-blend** (ground-tinted glow) where the painted backdrop meets the procedural
  ground, killing the hard line between baked sky/mountains and the floor (`showcase_v6/4_brawl_3v3_08`).

## 3. Victory hero screen
On a win, `BattleReplayView` shows a big **win-tier banner** (BattleDrama's `bannerTitle` — e.g.
"TOTAL DOMINATION") plus **win-tier stars** (Dominant 3 / Advantage 2 / Close 1) over the posing
winners (`showcase_v6/6_victory_12`). The missing "Victory Showcase" beat now exists and reads as a
real payoff.

## Harness + regression fixes (found while auditing)
- Added a fast-stomp **scene 6** (new `enemyLevel` param on `StartShowcase`, tooling only) so the
  victory beat is actually captured.
- **Reverted showcase speed 2.2×→1.6×**: at 2.2× battles finished mid-window and the flow drifted to
  menu/result screens (a capture regression).
- **Made the showcase READ-ONLY**: guarded `OnFinished` with `!ShowcaseActive()` so a finished
  showcase battle never triggers the result/reward/**save** flow (the stomp scene had been writing an
  unlock to the desktop build's save — now suppressed).

## Scores (from frames)
| Axis | V5 | V6 |
|---|:--:|:--:|
| Visual Quality | 85 | **88** |
| Combat Readability | 85 | **86** |
| Presentation | 88 | **90** |
| Polish | 84 | **87** |

## Note
The base monster pixels are still CC0 — shading gives them lit volume but can't add drawn detail. That
remains the one true asset-budget item. Next: a full **level + element** progression system (user
opted in explicitly — changes save/balance/progression), designed to stay deterministic + additive.
