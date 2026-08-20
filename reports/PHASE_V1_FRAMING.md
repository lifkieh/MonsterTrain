# PHASE V1 — VISUAL FRAMING

Presentation only. No sim / determinism / save / balance / progression change (79/79 EditMode green
after the pass). Source of truth: `reports/VISUAL_DIRECTOR_MASTERPLAN.md`. Evidence: re-captured
deterministic showcase frames in `reports/img/showcase_v2/` (compare vs `reports/img/showcase/`).

## Problems targeted (from the masterplan, all [SEEN])
- Dead black bands top + bottom of the frame.
- Fighters ~15 % of frame height (miniature).
- Brawl scrum piling to one side; half the arena empty.
- HP name labels overlapping when units bunch.
- No intentional per-mode framing (one camera for 1v1…3v3).

## Changes made

**Stage fills the whole screen (kills the black bands).**
`BattleArena` root grew `1200×1700 → 1360×2560`, the sky gradient was widened + extended
(`14 bands, 1400 wide, top y 1120`), and a `GroundBase` fill was added under the terrain so the deep
bottom is ground, never a black void. Result: top black band gone; bottom is arena terrain
(`showcase_v2/1_arena_1v1_02`, `4_brawl_3v3_12`).

**Fighters dominate.** `UnitView.ART 256 → 336` (feet offset scaled to match). Combined with the new
base zoom, fighters read ~1.5× larger and clearly own the frame (`1_arena_1v1_02`, `5_tag_3v3_06`).

**Dynamic framing by roster size.** New `_baseZoom` in `BattleReplayView`, chosen from the living
roster: Arena 1v1 `1.42`, 2v2 `1.26`, 3v3 `1.12`; Brawl 1v1 `1.34`, 2v2 `1.18`, 3v3 `1.05`. The
camera tightens further toward the last-two-standing and after the win. So 1v1 is a hero shot and 3v3
stays readable without cramming — each mode is framed on purpose, not one-size-fits-all.
**Critical fix:** every camera punch/zoom target was rebased to `_baseZoom + Δ` (they were absolute
`1.05–1.24` constants that, under the higher base, would have zoomed the camera *out* on a hit). KO
zoom now correctly punches IN (`3_arena_3v3_18`).

**Brawl spreads across the arena.** `EngageAnchor` gained a per-slot vertical lane (`(slot−1)·66`)
plus a mild pull toward a per-slot horizontal lane on the unit's own half (`Lerp(a.x, laneX, 0.24)`).
The unit still tracks its own opponent (dominant term); this only biases where the fights sit, so the
scrum now fans left-vs-right and fights in the arena centre instead of collapsing into one corner
(`4_brawl_3v3_12` vs the old `showcase/4_brawl_3v3_08`). Separation min-distance raised `135 → 162`
for the bigger bodies.

**Reserve HUD recedes.** Benched reserves' HP bars/names now follow the reserve dim, so the flanks
read as background, not pasted UI cells (`3_arena_3v3_10`). Fighter name labels shrunk (`20 → 17`,
narrower) to reduce overlap.

## Result (from frames)
- Black bands: **gone** (top) / **filled with terrain** (bottom).
- Fighter size: **~15 % → ~35–45 %** of frame in 1v1; readable in 3v3.
- Brawl: **centred, fanned** — no longer one-corner pile.
- Per-mode framing: **distinct** (1v1 hero vs 3v3 wide).

Remaining (carried to the review): a faint dark foreground band still sits at the very bottom; names
can still touch in the tightest brawl bunches.
