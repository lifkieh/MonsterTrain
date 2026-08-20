# VISUAL DIRECTOR MASTERPLAN

**Mode:** Visual Director. Judged **only** from frames the current build actually rendered — not
source, tests, or old reports. This is the first session I can say **[SEEN, current build]** about
every observation below.

**Scope guard:** every change proposed here is **presentation-layer only**. Nothing touches
gameplay, balance, progression, save, sim, or determinism. **Not implemented yet** — this is the
roadmap you asked for.

---

## 1. How these frames were produced (the review harness)

Built a self-driving showcase so the game can be reviewed **repeatedly, with no phone and no manual
input**:

- **Deterministic showcase battles** — `GameController.StartShowcase(players, enemies, tag, seed, levels)`:
  fixed teams + fixed seed + fixed mode, no random enemy, no match-count advance. Same input →
  identical battle every run.
- **Auto navigation** — `-showcase` boot flag skips menus/onboarding and drives the sequence directly.
- **Auto capture** — in-build `ScreenCapture.CaptureScreenshot` fires on a timer per scene; frames
  land self-labeled (`1_arena_1v1_07.png`, …) in `persistentDataPath/showcase`.
- **Auto sequence** — 5 scenes × 20 frames = 100 frames, unattended, ~150 s total.

**Key finding that unblocked everything:** in a windowed standalone, in-build `ScreenCapture` writes
**real pixels** (30–76 KB PNGs, not black). Earlier this session desktop **input** injection failed
(no interactive input desktop), but **render + in-build capture works** — so a self-driving harness
is the correct path, and it now runs.

Frames live in `reports/img/showcase/`. Required output moments — **all captured**:

| Required moment | Captured frame | Result |
|---|---|---|
| 1v1 Arena | `1_arena_1v1_02` | ✔ |
| 2v2 Arena | `2_arena_2v2_13` | ✔ (clash) |
| 3v3 Arena | `3_arena_3v3_10` | ✔ (`3 HITS!` + fire) |
| 3v3 Brawl | `4_brawl_3v3_08` | ✔ (fire biome scrum) |
| Tag Battle | `5_tag_3v3_06/18` | ✔ (tag-promote visible) |
| Ultimate Showcase | `1_arena_1v1_18` | ✔ (`ULTIMATE / Focus Strike`) |
| KO Showcase | `3_arena_3v3_18` | ✔ (zoom + eruption) |
| Victory Showcase | — | ✖ replays outran the 28 s window; end-beat not captured (see #17) |

---

## 2. What I actually saw

### Strengths (real, on screen)
- **Element identity reads.** Fire = orange flame that engulfs the caster (`3_arena_3v3_10`); Nature =
  green ring + leaf sparks (`5_tag_3v3_06`); Water = blue slash + droplets; Lightning = yellow bolt.
  These are shape-distinct, not color-only. Genuinely good.
- **Ultimate ceremony reads as special** — dark radial vignette + `ULTIMATE` + skill name focuses the
  caster (`1_arena_1v1_18`).
- **Combo counter** (`3 HITS!` … `6 HITS!`) is legible and biome-tinted.
- **HP state read** works — green/yellow/red bars; low HP is obvious (`3_arena_3v3_10` Jelly red).
- **Team pips dim on KO** — you can see who's winning (`4_brawl_3v3_19`: blue 2, red 1).
- **Tag promotion is visible** — front fighters swap (Wolf/Dire Wolf → Golem/Turtle across
  `5_tag_3v3_06` → `_18`).
- **Nature backdrop has real depth** — mountains, sky, clouds, layered pines (`5_tag_3v3_06`). This is
  the quality bar the game already proves it can hit.

### Problems (ranked by how much they hurt the look)
1. **Wasted composition.** In every non-nature scene the fighters sit in a narrow mid band and the
   **bottom ~third is dead black** (`1_arena_1v1_02`, `4_brawl_3v3_19`). Sprites are ~15 % of frame
   height. The stage feels small and empty.
2. **Dark, murky, low-contrast biomes.** The blue "arena" biome is the worst — near-black, faint grey
   noise, reads unfinished. **Nature ≫ procedural.** The whole palette skews dim and desaturated.
3. **Noisy ground decals.** Faint grey circles / rings / dashes are scattered across the floor
   (`1_arena_1v1_09`) — they read as artifacts, not grounding, and add clutter.
4. **Single-frame sprites.** Fine in motion (deform hides it), obvious when the KO camera zooms in
   (`3_arena_3v3_18`). No idle/attack/hurt frames.
5. **Brawl scrum drifts to one side.** Units pile on the right, half the arena empty, and **name
   labels overlap** when they bunch (`4_brawl_3v3_08/19`: Salamander/Treant/Phoenix/Turtle stacked).
6. **Climax over-layering.** At peak, concentric full-screen shockwave rings + 3 text banners + 2
   element rings fire at once (`2_arena_2v2_13`) — spectacular but noisy; readability drops.
7. **`BATTLE` title clips** into the top and is near-invisible on dark scenes (dark-on-dark).
8. **Team HUD is cheap** — flat boxy squares, no portraits/frames.
9. **Reserve/bench units** float in little boxed HP cells at the flanks (`3_arena_3v3_10`,
   `5_tag_3v3_18`) — they look like UI cells pasted onto the arena, breaking the diorama.

---

## 3. The question — top 20 highest-impact visual changes (10×)

Constraint honored: **no gameplay / balance / progression / save / sim change.** Ordered by ROI
(Impact high → low, then favoring low Effort / low Risk). **Impact** 1–10, **Effort** S/M/L,
**Risk** Low/Med/High. Nothing here is built yet.

### Tier A — do first (high impact, code-only, low risk)

| # | Change | Impact | Effort | Risk | Why (from the frames) |
|---|--------|:--:|:--:|:--:|---|
| 1 | **Reframe the stage: scale fighters ~1.6× and raise the ground line to fill the lower frame; delete the dead black bottom third.** | 10 | M | Low | Biggest single win. Fixes #1 across every scene; camera/layout only. |
| 2 | **Global color grade: lift brightness, contrast, and saturation.** | 8 | S | Low | Game reads dim (#2). One post-grade pass makes everything pop; no asset work. |
| 3 | **Replace scattered ground circles/dashes with one clean ground plane + soft per-unit contact shadow.** | 7 | S | Low | Kills the "artifact" clutter (#3), improves grounding immediately. |
| 4 | **Brawl positioning: spread the scrum across full arena width, stop one-side drift; de-overlap HP name labels (stagger / hide-when-bunched).** | 7 | M | Med | Fixes #5. Positioning is presentation; risk is only visual tuning, not sim. |
| 5 | **Tame climax layering: one hero banner at a time, thin the concentric rings, cap simultaneous full-screen VFX.** | 6 | M | Low | Fixes #6 — keep the punch, drop the noise. |
| 6 | **Separation lighting: rim light + stronger drop shadow on fighters so they pop off the backdrop.** | 6 | M | Low | Sprites currently sink into dark biomes; cheap depth win. |
| 7 | **Fix `BATTLE` title: respect top-safe-area, readable on dark (or fade it out during combat).** | 4 | S | Low | Fixes #7; trivial. |
| 8 | **Damage numbers: bigger, tier-weighted (light/heavy/crit), arc + crit styling.** | 5 | S | Low | `57` floats plainly today; more juice for free. |

### Tier B — high impact, needs backdrop/HUD art (medium effort)

| # | Change | Impact | Effort | Risk | Why |
|---|--------|:--:|:--:|:--:|---|
| 9 | **Bring every biome up to the Nature backdrop's quality** (layered painted-style fore/mid/back + parallax). | 9 | L | Low | #2 — Nature already proves the bar; blue/fire/water are the murky outliers. |
| 10 | **Framed team HUD with monster portraits** replacing flat squares. | 6 | M | Low | Fixes #8; makes the top bar look shipped. |
| 11 | **Dock reserves into a clean portrait tray** instead of floating arena HP cells. | 5 | M | Low | Fixes #9; restores the diorama. |
| 12 | **Grounding FX: landing dust, takeoff puff, slam decal / crack.** | 6 | M | Low | Adds weight; fighters still read slightly floaty. |
| 13 | **Ultimate ceremony polish:** brief slow-mo + full-art caster portrait flash + name typography (build on the good radial). | 6 | M | Low | The ceremony already reads special — push it to a signature moment. |
| 14 | **Victory / defeat end beat:** hero pose, banner, win-tier stars, reward flourish. | 6 | M | Low | The one moment I couldn't even capture (#Victory) — the loop needs a payoff frame. |
| 15 | **Idle background motion:** drifting clouds, lava-glow pulse, water shimmer. | 4 | M | Low | Backdrops are static; subtle life kills the "screenshot" feel. |
| 16 | **Screen-space post:** vignette + bloom on VFX + chromatic punch on crit/KO. | 6 | M | Med | Big perceived-quality lift; watch mobile perf budget. |

### Tier C — highest ceiling, asset production (large effort / pipeline risk)

| # | Change | Impact | Effort | Risk | Why |
|---|--------|:--:|:--:|:--:|---|
| 17 | **Frame-animate monsters** (idle breath, attack, hurt, death). | 10 | L | Med | The #1 "unfinished" tell (#4). Decisive vs competitors, but it's an animation pipeline. |
| 18 | **Authored element hit spritesheets** replacing generic CC0 bursts. | 7 | L | Med | Element casts read; the *impact* sheets under them are still generic. |
| 19 | **Per-element cohesive color language** across HUD accent, VFX, and backdrop tint. | 4 | S | Low | Fire scene = warm HUD, etc. Cheap cohesion once biomes are redone (#9). |
| 20 | **Signature KO cinematic:** dedicated finisher pose + slow-mo + full-screen impact frame per element. | 6 | L | Med | KO zoom + eruption already exists; elevate it into the shareable moment. |

---

## 4. If you only fund three things

**#1 (reframe the stage) + #2 (color grade) + #9 (biomes to Nature quality).** Those three alone move
the perceived quality more than any single asset item, and #1 and #2 are code-only, low-risk, and
shippable this week. #17 (frame animation) is the true ceiling but is a project, not a patch —
sequence it after the cheap wins land.

**Honest bottom line:** the combat *systems* read (element identity, ultimate, combo, HP, tag, KO all
land on screen). What holds the look back is **framing, lighting, and backdrop consistency** — mostly
code-fixable — plus **single-frame sprites**, which are the one gap no presentation code can close.
