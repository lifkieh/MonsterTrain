# Phase U — Quests

Date: 2026-08-19 · Presentation/meta only · Author: Lifkie Lie

## Design
Pure-C# `Quests` system (Meta, edit-mode tested), mirroring the `Progression` pattern.
Progress is **derived** from `SaveData` counters (never double-counted); only the per-quest
`claimed` flag is persisted (`SaveData.quests : List<QuestState>`, additive/backward-compatible).

Three kinds (10 quests):
- **Daily** (reset when the day index changes): Play 3, Win 2, Train 1 — read per-day
  counters `dailyBattles/dailyWins/dailyTrains` (reset by `Quests.SyncDay`).
- **Progress**: Win 10, Reach Trainer Lv 5, Collect 12, Evolve a monster.
- **Milestone**: Win 50, Complete a league, Land a 15-hit combo.

Counters are bumped by real events (battle result, train, evolve, career league clear) and
persist in the save; new fields: `winStreak/bestWinStreak, evolutionsDone, trainingsDone,
bestCombo, leaguesCompleted, questDay, dailyWins/Battles/Trains`.

## Rewards / persistence / UI / notifications
- **Rewards:** each quest grants coins + player XP on claim (`Quests.Claim`, applies level-ups).
- **Persistence:** `claimed` in save; daily claims reset per day via `SyncDay`.
- **UI:** Quests screen (menu button) lists quests grouped by kind, each with a live
  progress bar, "x / target  +coins", and a CLAIM button (enabled only when complete);
  claimed rows show CLAIMED.
- **Notifications:** the menu QUESTS button shows a "(N)" badge of claim-ready quests
  (`RefreshMenuBadges`); claiming pops a reward toast.

## Tests / build
- New `MetaSystemsTests`: derived progress, claim-once, daily reset, save round-trip.
- EditMode 75/75, PlayMode smoke PASS.

## Human QA (device)
- [ ] Quests screen shows daily/progress/milestone with correct progress bars.
- [ ] CLAIM only when complete; grants coins+XP; can't double-claim; menu badge updates.
- [ ] Daily quests reset the next day.
