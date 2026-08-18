# UI Redesign (K6 / K7)

Date: 2026-08-18. Modernizes the code-built mobile UI and surfaces the new element
system across the game. Android-first (portrait, `CanvasScaler` ScaleWithScreenSize
@ 1080×1920 reference, tested against 1080×2340).

## Shared components (`UIFactory`)
- **Modern buttons** — every button now has real press feedback (ColorTint:
  hover/press states, 0.08 s fade) and a subtle glossy top highlight for depth.
  This upgrades every screen at once (menu, career, daily, settings, result, detail).
- **Element palette + badges** — `ElementColor` (Fire = warm orange, Water = blue,
  Nature = green) and `ElementBadge` (colored chip + initial) as reusable pieces.
- Larger default button font (32) for readability / touch legibility.

## Battle
- **Element indicators** on every fighter (element-colored dot, top-right of the
  frame) — matchups are now readable at a glance.
- **Modern health bars** — smoothed current-HP fill + delayed "damage" ghost bar,
  color shifts green→amber→red with HP (in `UnitView`).
- **Damage numbers** — pooled floating combat text (crit/normal/heal colored).
- **Round-pip HUD** — player-vs-enemy remaining-monster pips + VS, screen-fixed.
- **Cleaner camera framing** — fighting-game 1v1 staging, wind-up→impact zoom,
  slow-mo finishers (Phase J cinematic system).

## Collection
- **Card-style tiles** with a **rarity-colored frame** strip (gray→green→blue→
  purple→gold), the species **element badge**, rarity stars, species icon, and
  owned/seen/locked state coloring. Filter-by-role and sort-by-rarity retained.

## Monster detail
- Element surfaced in the stat panel (`Role · Element · Rarity`), XP bar, per-stat
  growth preview, and the training flow.

## Career
- Polished progression ladder: 2-column stage cards colored by state
  (cleared green / current-frontier amber / locked dim), completion %, modern buttons.

## Results
- MVP showcase (top performer + icon) and a rewards breakdown (XP / coins /
  level-ups / unlocks / career stage clear), now with modern button styling.

## Menu / Settings
- Modern mobile menu layout (PLAY / CAREER / DAILY / CONTINUE / PROGRESS /
  COLLECTION / SETTINGS / QUIT) with version display; settings page with
  sound / frame-rate / quality toggles + about/credits.

## K7 — device readiness (Samsung S25 FE class, 1080×2340 portrait)
- Reference resolution 1080×1920, `matchWidthOrHeight = 0.5` → scales cleanly to
  taller 1080×2340 without horizontal overflow.
- Touch targets: primary buttons 90–130 px tall (≥ the ~48 dp minimum).
- Fixed vertical spacing audited per screen — no overlapping controls.

## Honest scope note
All UI is procedural (code-built, no art assets): icons, element badges, rarity
frames, and health bars are generated shapes/colors, not illustrated sprites. This
is a release-quality *layout and interaction* pass; a dedicated art pass (portraits,
illustrated icons, custom fonts) remains a future milestone. On-device visual QA of
these screens is still recommended.
