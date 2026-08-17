# PROJECT_STATUS.md — Train Your Monster

Snapshot updated during the first-playable build. Legend: **COMPLETE** ·
**IN PROGRESS** · **NOT STARTED** · **BLOCKED**.

Location: `E:\TrainYourMonster` (relocated off OneDrive). Engine: **Unity
6000.5.8f1**. HEAD: `083d2cb`. Balance: **amber-frozen** (see
`reports/PHASE1_BALANCE_LOCK.md`).

## Core / engine

| Item | State | Notes |
|---|---|---|
| Vision / GDD / architecture / spec | COMPLETE | Design locked & consolidated. |
| Unity project + compile | COMPLETE | Compiles clean on Unity 6.5; 4 asmdefs (+ Meta = 5). |
| Headless deterministic sim | COMPLETE | `BattleSimulator.Run`; event-log replayable. |
| Initiative fairness fix | COMPLETE | Seeded initiative key; mirror 49.9% (commit `083d2cb`). |
| Content assets (12 species + 10 skills) | COMPLETE | Generated to `Assets/Resources/` (`MTA → Generate Phase 1 Content`). |
| EditMode test suite | COMPLETE | **10/10 pass** (7 gate + 3 flow). |

## Balance

| Item | State | Notes |
|---|---|---|
| Balance research | COMPLETE (closed) | P1-1…P1-6; direction locked, tuning deferred. |
| Duration in 30–90 s | IN PROGRESS (deferred) | Amber-frozen; provisional dir K=25/apsLow0.016/ult3.0. Not a first-playable blocker. |
| Role/species balance | NOT STARTED (deferred) | Needs post-playable per-species pass (mixed-comp metric). |

## First playable (current focus)

| System | State | Notes |
|---|---|---|
| M1 Content generation | COMPLETE | 12 + 10 assets on disk. |
| M2 GameFlow state machine | COMPLETE | `Meta/GameFlow` + tests. |
| M2 Session + team model | COMPLETE | `Meta/GameSession` + toggle/cap tests. |
| M3 Match runner (teams→sim) | COMPLETE | `Meta/MatchRunner` + full-battle test. |
| M4 Battle replay view | COMPLETE | `BattlePlayback` (tested) + `BattleReplayView`/`UnitView`; Spawn events added to log. |
| M5 Runtime UI screens | COMPLETE | `UIFactory` + `GameBootstrap` (menu/select/battle/result), code-built. |
| M6 Scene bootstrap + wiring | COMPLETE | `GameController` (tested), `GameBootstrap`, scene builder; `Assets/Scenes/FirstPlayable.unity`. |
| M7 Windows build | COMPLETE (build) | `Build/Windows/TrainYourMonster.exe` (~97 MB) built successfully. |
| M7 Human play-through | **BLOCKED (needs human)** | Agent cannot watch/click a GUI headlessly. Human must run the .exe and confirm the 7-step loop. |

## Not started (post-first-playable / out of scope)

Training · leveling UI · save/load · career/leagues/gates · capture · economy ·
progression · real art/audio/VFX · Android polish · optimization. (Forbidden-list
items remain forbidden.)

## Phase 2 — Android (first playable on device)

| Task | State | Notes |
|---|---|---|
| Scene end-to-end | COMPLETE (logic) | 15/15 tests incl. full loop; Windows build runs. Visual = human. |
| Remove desktop-only deps | COMPLETE | `LoadBalance` now Resources-first (Android-safe); `balance.json` copied to `Resources`. |
| Touch-compatible UI | COMPLETE | Legacy Input Manager (`activeInputHandler:0`) + `StandaloneInputModule` handle touch; uGUI buttons. |
| Android Player Settings | COMPLETE (scripted) | `AndroidBuilder.Configure()`: package `com.trainyourmonster.game`, min SDK 24, IL2CPP/ARM64. |
| Portrait orientation | COMPLETE (scripted) | Set in `Configure()`. |
| Android build pipeline + APK script | COMPLETE | `App/Editor/AndroidBuilder.cs` (`MTA → Build Android APK`, headless-invokable). |
| Install instructions | COMPLETE | `reports/ANDROID_BUILD_INSTRUCTIONS.md`. |
| Automated tests | COMPLETE | 15/15 pass after changes. |
| **Build APK** | **BLOCKED (needs human)** | Android Build Support module NOT installed. Build script detects + reports it. Install via Hub, then `MTA → Build Android APK`. |

## Blockers / notes

- **First-playable visual verification requires a human** running the build
  (headless agent can build + logic-test but cannot watch the screen). This is
  the M7 handoff, by design.
- Uncommitted since `083d2cb`: many `reports/*`, the new `Meta/` layer +
  FlowTests, generated `Resources/` assets. Commit gated per standing rule.
