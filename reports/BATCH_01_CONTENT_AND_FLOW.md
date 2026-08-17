# Batch 01 — Content Generation + Game-Flow Skeleton

**Objective:** unblock the playable — generate the 12-species content and build
the headless flow skeleton (states, session, team→battle bridge), verified by
edit-mode tests.

## Implemented

- Ran `MTA → Generate Phase 1 Content` (headless `-executeMethod`): **12 species
  + 10 skill assets** created under `Assets/Resources/{Monsters,Skills}`.
- `Assets/Scripts/Meta/` (new asmdef `MTA.Meta`, references `MTA.Core`, pure C#):
  - `GameFlow.cs` — `GamePhase {MainMenu,TeamSelect,Battle,Result}` + transitions.
  - `GameSession.cs` — player/enemy teams, seed, last result, toggle+cap logic.
  - `MatchRunner.cs` — teams → `TeamConfig` → `BattleSimulator.Run`; player = team A.
- `Tests/FlowTests.cs` (3 tests) + `MTA.Tests.asmdef` now references `MTA.Meta`.

## Tests

Full EditMode suite: **10/10 pass** (7 gate + 3 new flow):
`GameFlow_WalksTheLoop`, `Session_TeamSelectToggle`,
`Match_RunsFromSelectedTeam_ProducesWinner` — all green. 0 compile errors.

## Result

M1 + M2 + M3 complete and verified headlessly. The flow logic runs a real battle
from a selected team with no scene/UI. Next: Batch 2 — the battle replay view
(`BattleReplayView`/`UnitView`) with a play-mode smoke test.

No commit (standing gate). `balance.json` untouched.
