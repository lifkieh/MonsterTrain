# Phase N — Combat Feel Overhaul

Date: 2026-08-18. Adds real CC0 impact VFX + arena background and combat-feel
polish (afterimage, MISS, dodge sound). **Presentation only** — balance,
determinism, battle outcome, and the save system are untouched (EditMode 66/66
green, self-referential hash tests pass).

## Sourced assets (all CC0)
| Asset | Source | Author | License | Used for |
|-------|--------|--------|---------|----------|
| **Free VFX Asset Pack** (22 effects) | https://opengameart.org/content/free-vfx-asset-pack | CodeManu | **CC0** | hit sparks, big hit, explosion, speed-lines, fire, electric, puff |
| **Parallax Forest Background** (PNG layers) | https://opengameart.org/content/parallax-background-forest-pixel-art | MatiasVME | **CC0** | real arena backdrop (element-tinted) |

8 effect spritesheets (6×5 grids, 30 frames; puff 7×6/42) were downscaled to
110 px/frame and imported to `Assets/Resources/Vfx`. The 5120×1440 forest panorama
→ `Assets/Resources/Arena/forest.png`. Raw packs + license in `Assets/ExternalArt/`.
(Full 94 MB VFX pack downloaded, only the 8 used sheets kept in-repo.)

## What was added / wired

### 1. Real combat flow (1v1)
Only the active monster is visible; reserves wait **fully off-screen** and the next
**runs in** on a death (from Phase N-prior; retained). Sprites: player back / enemy front.

### 2. Impact VFX (real)
- Light/mid hit → `hit_small` / `hit_impact` spritesheet at the target.
- **Critical** → `hit_big` + shockwave + screen flash.
- **Ultimate** → `explosion` + cinematic zoom + slow-mo + flash.
- **Death** → `explosion` (larger on the finishing blow).
- **Elemental cast** → Fire skills play `fire`, Water play `electric`.
- Speed-lines sheet available for dash emphasis.
All played via a pooled grid-animated `RawImage` (`VfxPlayer`/`VfxPool`/`VfxCatalog`).

### 3. Dodge visualization (was invisible)
- **Sidestep** (impulse) + **afterimage** (fading ghost) + **MISS** text +
  `puff` VFX + a **dodge whoosh** sound.

### 4. Camera
- Attack wind-up zoom, crit shake + zoom-punch, ultimate cinematic zoom, finisher
  slow-mo, victory zoom (retained from Phase J), now layered with the real VFX + flash.

### 5. Combo visualization
- Accelerating N-hit combos with hit-stop, knockback, launch, connecting-hit impact
  (retained), now each hit spawns a real VFX sheet.

### 6. Real arena background
- CC0 forest panorama backdrop, **element-tinted** (Fire warm / Water cool / Nature
  neutral), replaces the procedural sky; procedural ground + ambient particles kept.

### 7. UI asset pass
- **Not done this phase** — UI remains procedural (rounded panels, gloss buttons,
  element/rarity color language). Sourcing + 9-slice-wiring a CC0 UI kit is the next
  pass. (Honest gap.)

### 8. Battle readability
- Damage numbers (crit gold / normal white), **MISS** on dodge, `+heal` green,
  skill/ultimate banners, element-tinted fighters + arena, round-pip HUD, and the
  1v1 framing make attacker/target/damage/crit/miss/heal all clear.

## Validation
- **EditMode: 66/66 pass** — gameplay/determinism/save untouched.
- **PlayMode UI smoke: PASS** — boots + all screens + a battle with **zero runtime
  errors** (VFX pool, arena backdrop, afterimage all render clean).
- **APK**: 75.05 MB → 75.06 MB (delta +0.01 MB).
- Before/after screenshots in `reports/screenshots/`.

## Honest gaps
- VFX are retro pixel effects (CC0), not hand-authored anime FX.
- UI kit not yet sourced (procedural UI retained).
- Arena backdrop is one forest panorama tinted per element (not three distinct
  hand-made arenas) — a per-element background pack is the next sourcing step.
