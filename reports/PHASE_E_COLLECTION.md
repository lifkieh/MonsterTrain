# Phase E — Collection & Encyclopedia

Date: 2026-08-17. Adds a collection/encyclopedia screen with seen/owned states,
rarity, role filtering, sorting, collection %, and a new-monster popup. Save
extended (backward compatible); determinism untouched.

## Files changed
- **New** `Assets/Scripts/Meta/MonsterMeta.cs` — role classifier, rarity (1–5),
  stars, collection % (pure C#).
- **New** `Assets/Scripts/Tests/CollectionTests.cs` (4 tests).
- Edited `Meta/SaveData.cs` (`seen` list + `IsSeen`/`MarkSeen`), `Meta/SaveSystem.cs`
  (seen null-guard), `Meta/GameFlow.cs` + `GameController.cs` (Collection phase),
  `App/GameBootstrap.cs` (collection screen, tiles, filters, sort, new-monster popup,
  mark-enemies-seen).

## Tasks
1. **Collection screen** — grid of all species reachable from the menu.
2. **Monster encyclopedia** — each tile shows name, role-derived data, rarity.
3. **Seen / Owned states** — LOCKED / SEEN (fought) / OWNED (unlocked, with level);
   enemies fought are marked `seen` after each battle.
4. **Capture rewards** — unlocking a species (via player level-up) adds it to the
   collection (the MVP "capture").
5. **Unlock animation / 6. New monster popup** — a "NEW MONSTER!" overlay appears
   on the result screen when a species unlocks.
7. **Collection progress %** — header shows owned/total and percent.
8. **Filter by role** — All / Tank / Bruiser / Assassin / Mage / Support.
9. **Sort by rarity** — toggle rarity-desc vs name.
10. **Collection save support** — `seen` + collection persist in the JSON save.

Role and rarity are derived deterministically from base stats (`MonsterMeta`).

## Tests
Full EditMode suite: **34 / 34 pass** (30 prior + 4 new): role classification,
rarity range+ordering, owned-percent, seen mark+persist. Determinism/replay/save
tests still green.

## Known limitations
- Tiles use color badges + initials (no art). Deep per-monster encyclopedia detail
  (full stat pages) lands with the monster-detail screen in Phase F.
- Rarity is a stat-budget heuristic; a hand-authored rarity table can replace it.
- On-device visual QA still needed.

## Constraints
Android primary · determinism preserved · save backward-compatible (`seen`
defaults empty on old saves) · existing species only · no functionality removed.
