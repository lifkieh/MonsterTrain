# Meta Progression MVP

Date: 2026-08-17. Adds a persistent player profile, JSON save, monster collection,
XP/levels, battle rewards, unlocks, a progress screen, and continue. **Does not
touch the deterministic simulator or `balance.json`.** (Player monsters now enter
battle at their collection level — an intended progression input, not a balance
change; per-seed determinism still holds.)

## Files changed

**New**
- `Assets/Scripts/Meta/SaveData.cs` — save model (`SaveData`, `MonsterSave`) +
  `BattleRewards` + `Progression` rules (pure C#, testable).
- `Assets/Scripts/Meta/SaveSystem.cs` — JSON load/save to
  `Application.persistentDataPath` (Android-safe), atomic write, backward compatible.
- `Assets/Scripts/Tests/ProgressionTests.cs` — 4 tests.

**Edited**
- `Assets/Scripts/Meta/GameFlow.cs` — `Progress` phase + `GoProgress`.
- `Assets/Scripts/Meta/GameController.cs` — `ToProgress` / `BackToMenu`.
- `Assets/Scripts/Meta/GameSession.cs` + `MatchRunner.cs` — optional per-species
  `playerLevels` for the player team (default = fixed level, so existing tests are
  unaffected).
- `Assets/Scripts/App/GameBootstrap.cs` — profile load/create, Continue + Progress
  menu buttons, progress screen, unlock-gated team select, apply rewards + save
  after each battle, reward display.

**Untouched:** `Core/` (sim/determinism) and `balance.json`.

## Systems (by task)

1. **Player profile** — name, level, XP, coins, battles played/won.
2. **Local save** — `SaveSystem` writes JSON via `JsonUtility` to
   `persistentDataPath/save.json` (works on Android), temp-then-replace atomic
   write; loads with list defaults; `saveVersion` migration hook (backward compat).
3. **Monster collection** — per-species `MonsterSave { level, xp }`.
4. **Unlock tracking** — start with the first 6 roster species unlocked; team
   select is gated to unlocked monsters (locked ones greyed/disabled).
5. **XP** — player XP and per-monster XP awarded per battle.
6. **Player level** — XP curve `100 + 60·(level-1)`; each player level-up unlocks
   the next locked species.
7. **Monster level** — XP curve `50 + 40·(level-1)`, cap 30; team monsters that
   fought gain XP and can level up.
8. **Battle rewards** — win: +80 player XP / +60 monster XP / +50 coins; loss:
   +30 / +25 / +15. Applied once per battle, then saved.
9. **Progress screen** — player level/XP/coins/record + the full collection
   (unlocked levels, locked entries). Reachable from the menu.
10. **Continue** — the menu shows CONTINUE when a save exists; the profile is
    loaded at boot and persists across sessions.

Player monsters carry their collection level into battle (`GameSession.playerLevels`
→ `MatchRunner`), so leveling is meaningful; the enemy uses the base level.

## Tests

Full EditMode suite: **28 / 28 pass** (24 prior + 4 new).
- `NewGame_UnlocksStarters` — 6 unlocked/collected on a fresh profile.
- `ApplyBattle_Win_RewardsAndDeterministic` — deterministic XP/coins; monster
  levels up; battle counters update.
- `PlayerLevelUp_UnlocksNextSpecies` — reaching player level 2 unlocks a species.
- `Save_RoundTripsAndIsBackwardCompatible` — JSON round-trip preserves data; a
  partial/old save loads with non-null lists + defaults.
- All prior determinism/hash/replay/identity tests still pass.

## Persistence / compatibility notes

- Save path: `Application.persistentDataPath/save.json` (per-app, survives
  reinstalls-in-place; standard on Android).
- Atomic write (temp file then move) so a crash mid-save can't corrupt the file.
- Backward compatible: missing fields fall back to defaults; `saveVersion` is
  bumped on load; new fields added later ride the same mechanism.

## Known limitations

- On-device visual QA still needed (headless); progress/reward screens verified by
  logic + build only.
- Save is a single slot; no cloud save (out of MVP scope).
- Enemy scaling is flat (base level); balance of leveled play is untuned
  (amber-frozen — a later balance pass).
- Unlock order follows the sorted roster; no capture flow yet (Build Phase 3).

## Constraints honored

Android JSON persistence · backward compatible · deterministic simulator +
`balance.json` untouched · existing species only.
