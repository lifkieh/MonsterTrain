# Phase W — Monster Dex (Encyclopedia)

Date: 2026-08-19 · Presentation only · Author: Lifkie Lie

## What was built
A full Monster Dex screen (menu button "MONSTER DEX") covering all 21 species. Uses the
existing discovered state (`IsSeen` = unlocked ∪ seen; enemies are marked seen after every
battle), so no gameplay/save change was needed beyond reading it.

- **Grid** (3 cols): each tile shows the dex number, and — if discovered — the real
  portrait, Title-Case name, element badge, and rarity stars. **Undiscovered monsters
  appear as dim silhouettes labelled "???"** (per requirement).
- **Header:** "Discovered X / 21 (NN%)".
- **Detail** (tap a discovered tile): large portrait, element + role, base stats
  (HP/ATK/DEF/SPD/INT/LUCK), **evolution chain** ("Wolf → Dire Wolf (Lv 10)"), and the
  OWNED/SEEN state. CLOSE returns to the grid; hardware-back closes the detail first.

## Notes
- Completion counts the full 21-species roster; reaching 100% requires evolving the three
  evolution-only forms (Dire Wolf / Blade Mantis / Inferno Drake), which are discoverable
  by evolving their base. This also drives the "Master Collector" achievement.

## Tests / build
- EditMode 75/75, PlayMode smoke PASS (dex builds clean).

## Human QA (device)
- [ ] Undiscovered monsters are silhouettes with "???"; discovered show art + name + element + stars.
- [ ] Tapping a discovered monster shows stats + evolution chain; CLOSE/back works.
- [ ] The Dex % matches how many you've seen/owned.
