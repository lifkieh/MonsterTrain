# POST-V3 VISUAL DIRECTOR REVIEW

Judged **only** from the deterministic showcase frames actually rendered by the current build — not
source, tests, or commits. A/B is `reports/img/showcase_v2/` (after V1–V3) vs `reports/img/showcase/`
(before). Presentation-only pass; 79/79 EditMode green, sim/determinism/save/balance untouched.

Frames captured with no phone and no manual input, via the self-driving `-showcase` harness +
in-build `ScreenCapture`. All 8 required moments present (1v1/2v2/3v3 Arena, 3v3 Brawl, Tag, Ultimate,
KO; Victory end-beat still outran the per-scene window).

## The six questions — answered from frames

**1. Is the battle easier to read now?** **Yes.** Fighters are ~1.5× larger and carry a dark
separation outline, so they pop off the backdrop instead of sinking into it. The core reads
(who's attacking / HP state / combo / who's winning / KO) all land within ~1 s.
Compare `showcase_v2/3_arena_3v3_10` to the old dim `showcase/3_arena_3v3_10`.

**2. Does the arena feel more alive?** **Yes.** Biomes are brighter and lit; Fire has a glowing
volcanic ridge + lava, Water has a sun + sea + glints, Nature keeps its layered photo depth. Ambient
motion (embers / waves / leaves) plays over a stage that now fills the whole screen — the black bands
are gone (`1_arena_1v1_02`, `4_brawl_3v3_12`).

**3. Are the fighters big enough?** **Yes.** In 1v1 they occupy ~35–45 % of frame height and read as
hero-sized (`1_arena_1v1_02`, `1_arena_1v1_18`); dynamic zoom keeps 3v3 readable without shrinking
them to miniatures (`3_arena_3v3_10`).

**4. Is the brawl scrum still messy?** **Much improved, not perfect.** The per-slot vertical + mild
horizontal lane bias makes the scrum fan across the arena centre instead of hugging one edge
(`4_brawl_3v3_12` vs old `showcase/4_brawl_3v3_08`). At the very peak of a multi-unit clash it still
bunches, and names can still touch when three stack.

**5. Are Fire and Water on par with Nature?** **Close now.** Ranking went from **Nature ≫ Fire ≫
Water** to **Nature ≈ Fire ≈ Water**. Nature stays marginally ahead only because its backdrop is a
real painted photo; Fire/Water depth is generated shapes (bright, layered, atmospheric, but not
painted). The gap is small, not glaring.

**6. Top 5 remaining visual problems (from frames)**
1. **Single-frame sprites** — unchanged; the one gap no presentation code closes. Obvious on the KO
   zoom (`3_arena_3v3_18`). Needs an animator (asset).
2. **Faint ground decals** (water ripples / rings / dashes) still read as low-level noise on the floor
   (`1_arena_1v1_02`).
3. **Lower foreground band** is lighter but still a somewhat empty dark strip at the very bottom
   (`3_arena_3v3_10`).
4. **Peak-clash over-layer** — concentric shockwave rings + 2–3 banners still stack at the loudest
   moment (`2_arena_2v2_13`). Thinned, not eliminated (kept intentionally intense).
5. **Minor HUD collisions** — names can overlap in tight brawl bunches; the water sun disc slightly
   overlaps the top pip row (`2_arena_2v2_13`).

## Scores (0–100, from frames only)

| Axis | Before (showcase) | After (showcase_v2) | Basis |
|---|:--:|:--:|---|
| **Visual Quality** | 40 | **62** | bigger + lit + separated + biome parity; still single-frame + procedural |
| **Combat Readability** | ~55* | **73** | fighters dominate; attack/HP/combo/win/KO all read in ~1 s |
| **Presentation** | 45 | **66** | full-screen lit stage, dynamic framing, dimmed reserves |
| **Polish** | 42 | **58** | clean core; residual decal noise, foreground band, peak-clash layering |

\*prior readability wasn't scored numerically in the masterplan; estimated for delta only.

## Honest bottom line
V1–V3 moved the perceived quality up a real tier **without touching gameplay** — the frames prove it:
lit, full-screen stages, hero-sized outlined fighters, dynamic per-mode framing, and Fire/Water now
near Nature. The ceiling that remains is **single-frame sprites** (an animator) and **painted vs
generated backdrops** — both asset production, not code. Within the no-new-assets, presentation-only
box, this is close to the practical maximum; the leftover items (decal noise, foreground band,
peak-clash layering, HUD collisions) are small, code-tunable follow-ups.
