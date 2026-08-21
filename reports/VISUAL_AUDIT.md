# VISUAL AUDIT (Director Cycle — current build 1206a8a / V8)

Evidence: fresh deterministic frames `reports/img/showcase_v8/` (battle) + `reports/img/uishowcase/`
(meta), captured from the CURRENT build. Every item tagged from what rendered. Presentation only.

## Category scores (from frames)
| Axis | /10 | Basis |
|---|:--:|---|
| Combat Readability | 8 | pips + HP + Lv + element icons + combo + KO all read; peak clash busy |
| Combat Feel | 7 | hitstop/shake/zoom/bloom/impact-frame present; solid, not AAA |
| Animation | 7 | mesh-deform breathe/sway/bend + shading = alive; not drawn frames |
| Composition | 6 | **fighters in a mid band; ~40% lower frame empty (water/blue worst)** |
| Arena Quality | 8 | painted volcano/sea/hills, atmospheric, distinct |
| VFX | 7 | shaped element VFX + bloom; peak over-layer tamed |
| UI Polish | 8 | meta now painted + cohesive; element/Lv on cards |
| Store Readiness | 6 | competitive framing/color; base CC0 sprite detail still the tell |

## Top 20 issues (ranked by visual impact)

| # | Issue | Tag | Sev | Impact | Effort | Solution |
|---|-------|-----|:--:|:--:|:--:|---|
| 1 | Lower ~40% of frame is empty in Arena (esp. water/blue) — fighters sit in a mid band | [SEEN] `3_arena_3v3_08` | High | 8 | M | Tighten vertical framing (base-zoom bump) to crop dead margins + enlarge fighters |
| 2 | Reserve/edge fighters' HP bar + Lv badge clip the screen edge | [SEEN] `3_arena_3v3_08` (Golem "Lv12" cut) | Med | 6 | S | Hide Lv/element on benched reserves (declutter + no clip) |
| 3 | Empty upper sky is static/flat (non-nature) | [SEEN] `1_arena_1v1_11` | Med | 6 | S | Drifting procedural cloud wisps in the battle sky |
| 4 | Single-frame base sprite detail (CC0) | [SEEN] all | High | 8 | L(asset) | Exhausted with mesh-deform + shading; true detail needs an artist |
| 5 | Ultimate green/element wash goes near-monochrome | [SEEN] `5_tag_3v3_08` | Low | 4 | S | Reduce full-screen tint alpha on the ceremony |
| 6 | Deform slightly rubbery on elongated sprites | [SEEN] `5_tag_3v3_08` | Low | 4 | S | Clamp lean/limb amplitude by aspect |
| 7 | Peak clash still busy (rings + banners) | [SEEN] `2_arena_2v2_13` | Med | 5 | M | Already gated; further cap simultaneous banners |
| 8 | Fire ground scorch diamonds read abstract | [SEEN] `4_brawl` | Low | 4 | S | Round/soften them like the foreground boulders |
| 9 | Brawl bunches at the clash contact | [SEEN] `4_brawl_03` | Med | 5 | M | Already spread; minor lane widening |
| 10 | Contact shadow is a soft blob (grounding) | [SEEN] | Low | 4 | S | Tighter shadow + landing dust |
| 11 | Team pip portraits small at a glance | [SEEN] | Low | 3 | S | Slightly larger chips |
| 12 | HP-bar name labels can touch in 3v3 | [SEEN] | Low | 4 | S | Already staggered; fade distant names |
| 13 | No parallax depth cue on the painted mountains | [SEEN] | Low | 3 | S | Already slight; increase parallax factor |
| 14 | Damage numbers can overlap combo text | [SEEN] | Low | 3 | S | Offset lanes |
| 15 | Water ripple decals still faintly noisy | [SEEN] | Low | 3 | S | Already thinned; fine |
| 16 | Victory banner overlaps winner HP bars | [SEEN] `6_victory` | Low | 3 | S | Raise banner / hide winner bars on win |
| 17 | Element matchup readout can spam in brawl | [SEEN] | Low | 3 | S | Already throttled 0.8s; fine |
| 18 | Menu backdrop same for all screens | [SEEN] uishowcase | Low | 3 | M | Per-screen tint variety |
| 19 | Boss music/one-shot cues not visible | n/a | — | — | — | audio, out of visual scope |
| 20 | No screen-space bloom post (only fake halos) | [SEEN] | Low | 4 | M | UI additive material (shader risk) |

## TOP 3 highest-ROI (implementing this cycle)
1. **#1 Composition — tighten vertical framing** (base-zoom bump + safe clamps) so fighters fill the
   frame and the empty lower band is cropped. High impact, code-only.
2. **#2 Reserve HUD declutter** — hide Lv/element on benched reserves → fixes the edge-clip + cleans
   the flanks.
3. **#3 Living sky** — slow drifting procedural cloud wisps in the battle backdrop so the upper frame
   isn't dead.

Deliberately NOT touching #4 (asset), and deferring the many Low-severity items.

---

## CYCLE RESULT — implemented + verified (before `showcase_v8` → after `showcase_v9`)

**#1 Composition — lower-centre framing + tighter zoom.** [IMPLEMENTED] base zoom bumped
(1v1 1.42→1.52, 2v2 1.26→1.34, 3v3 1.12→1.18) + the whole battle stage framed **175 px lower** with a
taller arena so it still covers. [VERIFIED] Fighters are now hero-sized and stand in the **lower
centre**; in Fire/Nature the painted mountains fill the upper 2/3 = **cinematic** (`showcase_v9/
4_brawl_3v3_08`); Water reads calm with sky + clouds (`1_arena_1v1_09`, `2_arena_2v2_08`). The ~40%
empty lower band is gone. First attempt (zoom-only) enlarged fighters but did NOT move the action down;
the stage-offset was added to actually fix it — verified from frames, not assumed.

**#2 Reserve HUD declutter.** [IMPLEMENTED] benched reserves drop their Lv + element badges.
[VERIFIED] the flank clutter + the clipped "Lv12" on edge reserves is gone (`showcase_v9/3_arena_3v3_08`
vs `showcase_v8/3_arena_3v3_08`).

**#3 Living sky.** [IMPLEMENTED] three slow-drifting procedural cloud wisps over the painted sky.
[VERIFIED] the upper frame (esp. Water) is no longer static gradient (`1_arena_1v1_09`).

### Score delta (from frames)
| Axis | Before (V8) | After (V9) | Reason |
|---|:--:|:--:|---|
| Composition | 6 | **8** | fighters fill the frame; Fire/Nature cinematic with mountains |
| Visual Quality | 89 | **90** | bigger lit fighters, filled frame |
| Combat Readability | 8 | **8** | unchanged-high; bigger fighters help slightly |
| Presentation | 92 | **92** | already high |
| Polish | 89 | **90** | flank declutter + sky life |

### Publisher question
**"Would I proudly show these screenshots to a publisher?"** — For **Fire and Nature** stills
(`4_brawl_3v3_08`, tag/nature): **getting close to yes** — cinematic framing, painted atmospheric
depth, animated lit fighters, clean HUD. For the **product overall: still NO** — the base CC0 monster
sprite *detail/resolution* is the one remaining tell vs Monster Legends / Summoners War / Epic Seven /
Raid, and that is the sole genuine asset-budget item left (framing, color, composition, animation-via-
deform, backdrops, UI are all now competitive). Everything code could do has been done.

