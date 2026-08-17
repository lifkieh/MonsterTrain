# Phase G — Career Mode

Date: 2026-08-17. Adds a single-player career ladder: 12 stages across 4 leagues,
deterministic opponents, level-scaled difficulty, first-clear coin rewards,
frontier-gated unlocks, completion %, retry, and continue. Deterministic sim and
save compatibility preserved.

## Files changed
- **New** `Assets/Scripts/Meta/Career.cs` — `CareerStage` + pure-C# `Career` rules.
- **New** `Assets/Scripts/Tests/CareerTests.cs` (5 tests).
- Edited `Meta/SaveData.cs` (`int careerStage` frontier field),
  `Meta/GameSession.cs` (`enemyLevel`, `careerStageIndex`),
  `Meta/MatchRunner.cs` (enemy built at `enemyLevel`),
  `Meta/GameFlow.cs` + `GameController.cs` (Career phase + pending-stage flow),
  `App/GameBootstrap.cs` (career map screen, menu button, career-aware result).

## Tasks
1. **League map** — `CareerPanel`: 12 stage buttons in a 2-column grid, 4 leagues
   (Bronze/Silver/Gold/Master), colored by state (cleared / frontier / locked).
2. **Stage progression** — `careerStage` is the frontier (= stages cleared); only
   stages `index <= careerStage` are playable.
3. **Opponent generator** — `Career.Build` seeds a fixed `System.Random(9000+i)`
   per stage → stable 3-monster opponent team drawn from the species pool.
4. **Stage rewards** — first clear pays `40 + index*20` coins.
5. **Unlock requirements** — a stage unlocks only when the previous one is cleared.
6. **Career save state** — the frontier persists in `SaveData.careerStage`.
7. **Difficulty scaling** — enemy level = baseline (5) + stage index → L5..L16.
8. **Career completion %** — `CompletionPercent = careerStage / 12`, shown on the map.
9. **Retry** — RESULT → PLAY AGAIN re-fights the same queued stage (no repeat reward).
10. **Continue** — RESULT → CAREER MAP returns to the ladder with the next stage
    unlocked; tapping the highlighted frontier resumes.

The player still picks their own 3 monsters (existing team-select), fighting at
their collection levels vs the scaled career opponents. `balance.json` untouched;
difficulty comes only from opponent level, a presentation/meta lever.

## Tests
Full EditMode suite: **43 / 43 pass** (38 prior + 5 new): ladder determinism +
length, monotonic difficulty scaling, fresh-save gating, frontier advance + pay
once + retry gives nothing + no frontier-skip, full 12-stage sweep → complete/100%.
Determinism/replay/save tests still green.

## Known limitations
- 12 stages / 4 leagues is the MVP ladder length; content, not a cap on the system.
- No per-stage star ratings or boss modifiers yet (future meta polish).
- On-device visual QA still needed.

## Constraints
Android primary · determinism preserved · save backward-compatible (new int field
defaults to 0 for old saves) · no functionality removed.
