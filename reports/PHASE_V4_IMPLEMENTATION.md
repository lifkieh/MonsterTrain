# PHASE V4 — IMPLEMENTATION

Implemented **only the top-3 ROI fixes** from `VISUAL_DIRECTOR_PHASE_V4_AUDIT.md`. Presentation only —
no sim / determinism / save / balance / progression change (79/79 EditMode green, Windows standalone
builds). Before = `reports/img/showcase_v2/` (V1–V3). After = `reports/img/showcase_v3/` (this pass).
Deterministic capture, same seeds/scenes → true A/B.

## Fix 1 — Screen vignette (photographic depth)
New smooth `ProceduralArt.Vignette()` (bilinear, no 3-step quantization) drawn full-screen over the
battlefield, below the flash/impact overlays + HUD. Black, α 0.5 at the edges, clear centre.
- **Before** `showcase_v2/1_arena_1v1_02`: flat "UI on a gradient", eye wanders.
- **After** `showcase_v3/1_arena_1v1_02`: corners fall off, the eye is pulled to the lit fighters; the
  frame reads as a scene, not a screen. Applies to every biome.

## Fix 2 — Brawl opening + label de-collision
Two changes: (a) `ChargeAnchor` spreads the opening collision by slot (`side·(150+slot·55)`, Y lanes
`±150`) so the charge no longer funnels all six units onto one point; (b) new
`UnitView.SetBarRaise(dy)` staggers each fighter's HP-bar + name height by slot
(`(slot%3)·32`), so stacked teammates' HUD steps instead of overlapping.
- **Before** `showcase_v2/4_brawl_3v3_03`: six bodies + every HP bar / name / banner collapsed into an
  unreadable soup at the clash.
- **After** `showcase_v3/4_brawl_3v3_03`: HP bars form a readable staircase (Kraken low → Turtle high),
  names separate, units more distributed. Still a busy contact moment (6-unit clash), but parseable.

## Fix 3 — Cleanup sweep
- **Removed** the Water "sun" — it rendered as a flat grey placeholder blob near the HUD
  (`showcase_v2/2_arena_2v2_05` → gone in `showcase_v3/2_arena_2v2_05`).
- **Softened** the harsh cyan ground-lip (Floor ×1.6→×1.25, Lip ×2.1→×1.5) — the neon horizontal
  lines are now subtle standing edges.
- **Thinned** the water floor decals (ripples 5→3 @ α0.5→0.32, caustics α0.22→0.13) — less floor noise.
- **Pulled reserves in** (bench anchor ±430→±392) so benched units no longer clip the screen edge under
  zoom (`showcase_v3/2_arena_2v2_05`: Golem now sits on-screen).

## Measured differences (from frames)
| Item | Before (v2) | After (v3) |
|---|---|---|
| Frame depth | flat gradient | vignette-framed, eye centred |
| Brawl opening HUD | overlapping soup | staggered staircase, readable |
| Water sun | grey placeholder blob | removed |
| Ground lip | harsh cyan neon lines | soft standing edges |
| Water floor decals | 5 rings + dense caustics | 3 faint rings + light caustics |
| Reserves | clipped at edge | on-screen |

## New scores (from frames only)
| Axis | V3 (before) | V4 (after) | Δ |
|---|:--:|:--:|:--:|
| **Visual Quality** | 62 | **70** | +8 |
| **Combat Readability** | 73 | **79** | +6 |
| **Presentation** | 66 | **74** | +8 |
| **Polish** | 58 | **67** | +9 |

Matches the audit estimate. The remaining ceiling is unchanged and asset-bound: **single-frame monster
sprites** and **procedural (not painted) backdrops** — no presentation code closes those. Backlog items
4–10 from the audit (portrait pips, VFX bloom, weightier damage numbers, foreground band, banner
hierarchy) remain for a later phase.

## Final question
**"If this game appeared on the Play Store today, would I personally be proud to show these screenshots
to a publisher?"**

**NO.**

Why, judged like a publisher screening 100 mobile games a week — no optimism bias:
- The **presentation layer is now genuinely competent**: readable in ~1 s, framed with depth, lit
  biomes, dominant outlined fighters, clean HUD. In isolation the staging would not embarrass.
- But the **monster art is single-frame**. A publisher spots static/deforming-only sprites within
  seconds of any motion, and even in stills the pixel monsters + procedural backdrops read a clear tier
  below Monster Legends / Summoners War / Epic Seven / Raid. Those titles ship full frame-animated
  creatures and painted, layered environments. That gap is the first thing a seasoned reviewer filters
  on, and no amount of the camera/color/vignette work I can do changes it.
- Verdict: I'd proudly show this as a **strong prototype with a real game-feel layer** — not as a
  store-competitive product against those four. To flip this to **YES** needs two asset investments the
  presentation code cannot substitute: **frame-animated monsters** (animator) and **painted per-biome
  backdrops** (environment artist). Until then: NO.
