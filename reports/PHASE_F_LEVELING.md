# Phase F — Training & Leveling

Date: 2026-08-17. Adds a monster detail screen with XP bar, stat/growth preview,
and a coin-cost training button that levels monsters up (persisted). Save
compatible; determinism untouched.

## Files changed
- **New** `Assets/Scripts/Tests/TrainingTests.cs` (4 tests).
- Edited `Meta/SaveData.cs` (`Progression.Train` + `TrainCost`/`TrainXp`),
  `Meta/GameFlow.cs` + `GameController.cs` (Detail phase),
  `App/GameBootstrap.cs` (detail screen, XP bar, previews, TRAIN, level-up popup;
  owned collection tiles open detail).

## Tasks
1. **Monster detail screen** — opened by tapping an owned monster in the collection.
2. **XP bar** — filled bar showing `xp / next` for the monster.
3. **Level-up screen** — a "LEVEL UP!" popup when training pushes a new level.
4. **Training button** — spend `TrainCost` (30) coins → `+TrainXp` (45) XP.
5. **Stat preview** — effective stats at the current level.
6. **Growth preview** — per-stat gain per level and the next-level value.
7. **Multiple level support** — repeated training levels a monster up multiple
   times; the level-up loop handles XP overflow.
8. **Save integration** — training saves immediately; levels persist.
9. **UI polish** — role/rarity header, XP bar, coin display, guarded TRAIN.
10. **Tests** — below.

Effective/growth previews use `StatMath` with the shared `BalanceConfig` (grade-B
reference); no gameplay numbers changed.

## Tests
Full EditMode suite: **38 / 38 pass** (34 prior + 4 new): train spends coins +
grants XP, insufficient-coins no-op, locked-monster no-op, many-trains level up
and persist. Determinism/replay/save tests still green.

## Known limitations
- Stat preview uses a grade-B reference (per-instance growth grades aren't stored
  in the MVP collection); it's indicative, not the exact rolled value.
- Training is a coin sink only (no timers/session tiers yet — those are the fuller
  Build-Phase-2 training UX).
- On-device visual QA still needed.

## Constraints
Android primary · determinism preserved · save backward-compatible · existing
species only · no functionality removed.
