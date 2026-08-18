# Phase L — Production Visuals

Date: 2026-08-18. Replaces the flat programmer-placeholder presentation with a
procedural production look. **Presentation only** — gameplay, balance, determinism,
progression, evolution, and the save system are untouched (verified: EditMode
suite green, self-referential hash tests pass).

## Systems added

### Procedural sprite generator (`ProceduralArt.cs`)
Runtime-generated white sprites (tinted per use): **disc, glow, triangle, ring,
rounded-rect, vertical-gradient** — from `Texture2D`, cached once. Lets the whole
game draw real shapes instead of rectangles. No art assets; works on Android.

### Procedural monster portraits (`MonsterArt.cs`)
Assembles a recognizable monster from **species + element + role**, deterministic
per species (no two alike):
- **Body** (role-shaped: tank bulky, assassin slim, mage orb), **belly**, **eyes**
  (1–2, cyclops variant), **horns** (0–2), **wings**, **tail**, **claws**, **aura**.
- **Element palette**: Fire warm + ember aura, Water cool + splash aura, Nature
  green + leaf aura; blended with the species' unique hue so every monster differs
  within its element.
Used in the **battle units** (`UnitView`), **collection tiles**, **monster detail**,
and the **result MVP**.

### Rarity presentation
Collection cards get a **rarity-colored frame** (gray → green → blue → purple →
gold), rarity stars, the **element badge**, and a card background. Locked entries
show a dimmed silhouette.

### Battle effects
- **Crit / ultimate screen flash** (full-screen white overlay, decaying).
- **Shockwave** rings expanding from crit/ultimate impacts.
- Existing pooled slash/impact bursts, projectiles, floating damage numbers,
  hit-stop, camera shake/zoom, slow-mo finishers retained and now layered with the
  flash/shockwave.
- **Death dissolve** (portrait fades via CanvasGroup) + **heal green pulse**
  (flash overlay) on the portrait.

### Element arenas (`BattleArena.cs`)
Procedural arena themed by the enemy front-liner's element (**Fire / Water /
Nature**): gradient sky, parallax mountain + pillar layers, ground/floor, and
**drifting ambient particles** (embers rise / motes drift / leaves fall), animated
each frame.

### UI modernization
- **Modern buttons** — press ColorTint + glossy highlight + **scale-punch on press**
  (`ButtonPunch`).
- **Rounded panels/cards** (RoundedRect sprite), element/rarity color language.
- **Page transitions** — every screen fades + pops in (`AnimatePanel`/`PanelIn`).
- **Popup animation** — scale-in (`PopIn`).
- **Volume sliders** (`UIFactory.Slider`) in Settings.

### Result screen
- **MVP** shown as a procedural portrait, **Combo King** (most damaging hits),
  damage/heal leaders, rewards breakdown, and a **banner scale-punch reveal**.

## Files changed
- New: `Battle/ProceduralArt.cs`, `Battle/MonsterArt.cs`, `App/ButtonPunch.cs`.
- Rewrote: `Battle/BattleArena.cs`, `Battle/UnitView.cs` (portrait + flash overlay).
- Edited: `Battle/BattleReplayView.cs` (arena element, screen flash, shockwave,
  dynamic-audio hooks), `App/UIFactory.cs` (slider, button punch), `App/GameBootstrap.cs`
  (portraits everywhere, transitions, sliders, result reveal, SFX/music hooks).

## Validation
- **PlayMode smoke PASS** — boots + walks every screen + runs a battle with **zero
  runtime errors** (portraits, arenas, flash, transitions all render clean).
- EditMode gameplay/determinism suite green (see PHASE_M_AUDIO / audit for numbers).
- All visuals procedural — no illustrated sprite assets (documented gap).
