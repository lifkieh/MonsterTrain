# Phase X — Retention

Date: 2026-08-19 · Presentation/meta only · Author: Lifkie Lie

## Chosen implementation
Rather than a bolt-on system, retention is delivered as an integrated set of long-term
goals plus a **Trainer Profile dashboard** that makes progress legible — the most durable
retention pattern for a collection auto-battler, and it reuses systems already built:

1. **Streaks**
   - *Daily login streak* (existing `loginStreak`, 7-day daily-reward cycle).
   - *Win streak* (new `winStreak` / `bestWinStreak`) — increments on a win, resets on a
     loss; drives the "Unstoppable" achievement.
2. **Completion goals** shown as live progress bars on the profile:
   - Monster Dex % (discovered / 21), Collection owned %, Career %, Achievements %.
3. **Progression goals** — the Quest (U) daily/progress/milestone ladder and the
   Achievement (V) set are the concrete long-tail objectives; the profile surfaces the
   **next 4 incomplete goals** so there's always a visible "next thing to do".
4. **Evolution goals** — evolving all three pairs is required for 100% Dex and powers the
   Evolution Master / Master Collector achievements.

## Trainer Profile screen (rebuilt PROGRESS)
The old plain-text roster dump is replaced by a dashboard: name + level, coins, win rate,
current/best win streak + daily streak, four completion bars (Dex/Collection/Career/
Achievements), and a "NEXT GOALS" list of the nearest incomplete quests. The menu also
shows a persistent Lv + coins wallet line and a quest-ready badge.

## Persistence
All retention state is in the (backward-compatible) save: `winStreak, bestWinStreak,
loginStreak`, plus the derived completion from `unlocked/seen/achievements/careerStage`.

## Out of scope (noted)
Seasonal/time-limited objectives were considered but deferred — they need a live-ops
calendar/config and don't fit an offline single-player build yet.

## Tests / build
- EditMode 75/75, PlayMode smoke PASS.

## Human QA (device)
- [ ] Profile shows correct win rate, streaks, and four completion bars.
- [ ] Winning raises the win streak; losing resets it; best is retained.
- [ ] "Next goals" lists incomplete quests and updates as you complete them.
