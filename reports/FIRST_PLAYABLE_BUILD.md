# First Playable — Build Report (Batches 2–4)

Consolidated report for the interactive shell + build. Follows
`BATCH_01_CONTENT_AND_FLOW.md`. **First playable is code-complete and builds to a
Windows `.exe`.** The only remaining step is a human running it to confirm it
renders and plays — the agent cannot verify a GUI headlessly.

## Objective

Reach a playable build: open → select team → start battle → watch → win/lose →
return to menu → play again. Built entirely in code (no manual scene/prefab
wiring) so it is reproducible headlessly.

## Implemented

**Batch 2 — battle view**
- `Core/BattleSimulator.cs`: added per-unit **Spawn events** (maxHp + species) so
  the event log is self-contained for replay. Safe for all tests (determinism is
  self-referential; balance parsers ignore the new kind).
- `Meta/BattlePlayback.cs`: pure-C# replay reconstruction (Spawn/Action/Died/End →
  per-unit HP + alive + winner). Edit-mode tested.
- `Battle/BattleReplayView.cs` + `UnitView.cs`: placeholder visuals (colored
  panels, HP bars, floating damage numbers), driven by the event log over time,
  fires `OnFinished(winner)`. New asmdef `MTA.Battle`.

**Batch 3 — shell**
- `Meta/GameController.cs`: the playable's brain (flow + session + enemy roll +
  match run + phase advance). Pure C#, fully edit-mode tested.
- `App/UIFactory.cs` + `App/GameBootstrap.cs`: code-built canvas + four panels
  (Main menu, Team select of the 12 species, Battle, Result), wired to
  `GameController`; loads registry + `balance.json` at boot. New asmdefs
  `MTA.App` + `MTA.App.Editor`.
- `App/Editor/FirstPlayableSceneBuilder.cs`: builds `Assets/Scenes/FirstPlayable.unity`
  (one `GameBootstrap` object) and the Windows player from script.

**Batch 4 — build**
- Scene built + registered; **Windows player built: `Build/Windows/TrainYourMonster.exe`
  (~97 MB), result = Succeeded.**

## Verification (headless)

| Check | Result |
|---|---|
| Compile (all 6 asmdefs) | 0 errors, 0 warnings |
| EditMode tests | **15 / 15 pass** |
| — full playable loop (`GameControllerTests.FullPlayableLoop`) | pass |
| — replay reconstruction (`PlaybackTests`) | pass |
| — flow / session / match (`FlowTests`) | pass |
| — all 7 original gate tests | pass |
| Content assets | 12 species + 10 skills present |
| Scene builds | `FirstPlayable.unity` created + in Build Settings |
| Windows build | Succeeded, `.exe` on disk |
| Headless player smoke-launch | No exceptions logged (but batchmode/no-display can't drive the game loop — inconclusive by nature) |

## What the agent CANNOT verify (the human step)

Rendering, layout (nothing off-screen / overlapping / unclickable), and the actual
7-step click-through require a real display. This is the **M7 play-through** and
must be done by a human:

1. Run `E:\TrainYourMonster\Build\Windows\TrainYourMonster.exe` (or open the
   project and press Play on `Assets/Scenes/FirstPlayable.unity`).
2. Main menu → **PLAY**.
3. Team select → tap **3** species (turn green; "3 / 3") → **START BATTLE**.
4. Watch the battle (HP bars drain, damage numbers, deaths).
5. **VICTORY/DEFEAT** banner appears.
6. **PLAY AGAIN** → back to team select; **MENU** → back to main menu.
7. Repeat a few times.

Report any of: black screen, off-screen buttons, unclickable UI, no battle
motion, exceptions in the Player log. Those are the expected first-run fixes
(KILL_CRITERIA S4: any screen past 3 sessions → collapse to a plain list).

## Status

First playable: **code-complete + built**, all logic tested. Milestone
verification pending a human run. Nothing committed (standing gate) — recommend a
checkpoint commit once the human confirms the loop runs.
