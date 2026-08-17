# PROJECT_STATUS.md — Train Your Monster

Snapshot of current project state. Legend:
**COMPLETE** · **IN PROGRESS** · **NOT STARTED** · **BLOCKED**

> Assessed from the archived documents and the Phase 1 scripts drop. The scripts
> exist as a source drop (an `Assets/` folder), **not yet imported into a Unity
> project** — nothing has been compiled or run in the editor. That gap drives
> most "IN PROGRESS / NOT STARTED" marks below.

## High-level state

| Item | State | Evidence / Notes |
|---|---|---|
| **Vision** | COMPLETE | Locked in game-spec v0.5 + GDD v1.0 + SKILL.md. Fantasy, pillars, loop all fixed. |
| **GDD** | COMPLETE | GDD v1.0 written and internally consistent; folds accepted decisions from all evals. |
| **Architecture** | COMPLETE (design) | code-conventions + Phase 1 spec define layers, data model, sim design, save pattern. Design done; not yet realized in a live Unity project. |
| **Phase 1 Spec** | COMPLETE | mta-phase1-battle-prototype-spec v1.0 — objectives, success criteria, build order, testing plan all defined. |
| **Scripts (Phase 1 Core)** | IN PROGRESS | ~1,322 lines generated across Core/Data/Editor/Tests + balance.json. No stubs/TODOs found. Not yet compiled in Unity. |
| **Compilation** | NOT STARTED | No Unity project exists at the repo root; scripts have never been compiled against UnityEngine/NUnit. Unverified. |
| **Tests** | NOT STARTED (written, unrun) | `Phase1GateTests.cs` authored (8 tests covering criteria 1–6). Never executed — Test Runner has not been run. |
| **Simulation** | IN PROGRESS | `BattleSimulator.Run` + `BalanceSweep.Run` implemented in Core; sweep EditorWindow wrapper NOT in this drop. Never run. |
| **Balance** | NOT STARTED (v0 seeded) | `balance.json` holds v0 constants; per-species base stats exist in the balance sheet but `speciesGainRates` array is empty. No sweep has been run, so no percentile data / tuning done. |
| **Debug Viewer** | NOT STARTED | `BattleReplayView` / `UnitView` explicitly excluded from the scripts drop. |
| **Android build** | NOT STARTED | Build Phase 5 territory; no project to build. |

## Phase 1 success criteria — verification status

None are verified (nothing has run). Written coverage vs. verified:

| # | Criterion | Test written? | Verified? |
|---|---|---|---|
| 1 | Determinism: identical event-log hash ×100 | Yes (`Determinism_SameSeedSameHash_100Runs`) | NO |
| 2 | Duration sweep P10 ≥ 30 s, P90 ≤ 90 s, ≤ 5% hard-resolve, 0 non-terminating | Partial (termination + mirror; full percentile sweep needs the EditorWindow) | NO |
| 3 | Mirror fairness 50% ± 3% | Yes (`MirrorComps_NoSideBias`, 400 battles, 0.42–0.58 band) | NO |
| 4 | Preparation signal ≥ 75% | Yes (`PreparationSignal_TrainedBeatsUntrained`, 300 battles) | NO |
| 5 | Zero-code 13th species from data | Yes (`ThirteenthSpecies_FromPureData_ZeroCode`) | NO |
| 6 | Mechanics correctness (stat/level/training math) | Yes (`StatMath_MatchesSpecFormulas`, `Training_RoutesThroughGrowthGrade`) | NO |
| 7 | Device proof: replay scene on Android | No | NO |

## What exists in the scripts drop

**Present** (`Assets/Scripts/`):
- `Core/`: Enums, StatBlock, BalanceConfig, StatMath, ContentData, MonsterInstance,
  LevelMath, TrainingMath, TeamConfig, BattleEvent, BattleState, ActionTimeline,
  TargetSelector, SkillResolver, BattleSimulator, SpeciesRegistryCore, BalanceSweep.
- `Data/`: GrowthProfile, MonsterSpecies, SkillDefinition, SpeciesDatabase.
- `Editor/`: SpeciesAssetGenerator (menu **MTA → Generate Phase 1 Content** —
  builds the 10-skill pool + 12 species assets).
- `Tests/`: Phase1GateTests (edit-mode, 8 tests).
- `StreamingAssets/balance.json` (v0 constants).
- `Assets/README.md` (import + boot instructions).

**Deliberately absent** (flagged in the drop's README):
- `Battle/BattleReplayView.cs`, `Battle/UnitView.cs` (view layer / debug scene).
- `Editor/BalanceSweepWindow.cs` (the `MTA/Balance Sweep` EditorWindow — core
  `BalanceSweep.Run` exists, only the window wrapper is missing).
- Freshness decay (product-layer choice mechanic, not needed to validate the
  balance model in Phase 1).
- XP-from-battles wiring (levels set directly in Phase 1).
- Generated `.asset` files (created by running the generator; not shippable in a
  script zip).

## Immediate blockers

1. **No Unity project.** The scripts must be imported into a Unity 2021.3 LTS+
   project before anything can compile, generate assets, or run tests.
2. **Balance unverified.** Success criteria 2–4 gate Phase 1 and require the
   sweep to run; the EditorWindow wrapper and a first tuning pass are missing.

## Overall

Design phase: **effectively COMPLETE and consistent.** Implementation:
**Phase 1 code drafted (~90% of the non-view Core), zero of it verified.** The
project is at the "import, compile, run the gate tests" threshold — no gameplay
UI, no build, no balance data yet.
