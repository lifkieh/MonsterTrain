# PHASE V3 — BIOME QUALITY / PARITY

Presentation only, **no new assets**. Goal: bring Fire and Water up to the Nature biome's quality so
the three read as one production tier. Evidence: `reports/img/showcase_v2/`.

## Audit before ([SEEN], `reports/img/showcase/`)
- **Nature** — best: real layered backdrop (sky / clouds / mountains / tree line), bright, deep.
- **Fire** — near-black dark red; flat; read as "dim".
- **Water** ("arena"/default blue) — murky, low-contrast, empty. The weakest.
Ranking was **Nature ≫ Fire ≫ Water**.

## Changes made (no new art — brighter palettes + richer generated depth)

**Fire.** Palette lifted to a warm lava world (bright orange horizon `skyHor 0.90,0.38,0.17`, lit
ground/floor). Far biome enriched: a hazy horizon glow band, a **layered volcanic ridge** (far cones
behind near cones for depth), brighter glowing craters, and lava runs down the cone flanks. Result:
reads as a lit volcanic basin, not a dark smear (`showcase_v2/4_brawl_3v3_12`, `3_arena_3v3_18`).

**Water.** Palette lifted to a bright sea (`skyHor 0.30,0.66,0.88`, lit floor). Far biome enriched: a
**sun** high in the sky, a bright horizon haze band, a deeper multi-band sea, more wave lines, and a
sun-glint reflection on the surface. Result: a vivid daylight sea (`showcase_v2/1_arena_1v1_02`,
`2_arena_2v2_13`). (Iteration note: the sun was first placed at fighter height and read as a grey
blob; it was moved high into the sky, clear of the fighters.)

**Nature.** Kept its forest-photo backdrop; palette nudged slightly brighter for parity of ground
tone. Still the richest because it uses a real painted texture (`5_tag_3v3_06`).

## Result — new ranking (from frames)
**Nature ≈ Fire ≈ Water**, with Nature still marginally ahead because its backdrop is a real painted
photo (fore/mid/back depth) while Fire/Water are generated shapes. The *gap closed from large to
small*: all three are now bright, have horizon depth, atmosphere (embers / waves+sun / leaves), and a
lit ground the fighters stand on. The one honest ceiling that remains is that Fire/Water depth is
procedural silhouettes, not a painted backdrop — closing that fully is an asset task, out of scope
for a no-new-assets pass.
