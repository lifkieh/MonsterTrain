# Phase H — Daily Rewards & Retention

Date: 2026-08-17. Adds a daily-login reward loop: a 7-day escalating streak,
reset-on-gap, claim popup, reward history, and clock-tamper guards. Save
compatible; deterministic sim untouched.

## Files changed
- **New** `Assets/Scripts/Meta/DailyRewards.cs` — pure-C# streak/reward rules.
- **New** `Assets/Scripts/Tests/DailyTests.cs` (7 tests).
- Edited `Meta/SaveData.cs` (`lastClaimDay`, `loginStreak`, `rewardHistory`),
  `Meta/SaveSystem.cs` (null-guard for `rewardHistory`),
  `Meta/GameFlow.cs` + `GameController.cs` (Daily phase),
  `App/GameBootstrap.cs` (daily screen, menu button, launch auto-open).

## Tasks
1. **Daily reward** — one claim per local calendar day.
2. **Login streak** — consecutive days increment; a missed day resets to 1.
3. **Reward popup** — claim shows a "DAILY REWARD — Day N, +X coins" popup.
4. **Local time handling** — day = `DateTime.Now.Date` mapped to a day index; the
   pure rules take the time in as a parameter (testable), runtime feeds `Now`.
5. **Reward history** — last 30 claims stored; last 12 shown on the screen.
6. **Save integration** — streak/last-claim/history persist in `SaveData`.
7. **Anti-cheat sanity checks** — no double claim same day; a rolled-back clock
   (today ≤ last claim) is rejected, so date-spoofing can't farm rewards.
8. **Reward balancing** — 7-day table `{50,60,75,90,110,130,200}`, day 7 the peak,
   then the cycle repeats while the streak keeps counting up.
9. **UI flow** — `DAILY` menu button + auto-open on launch when a claim is available.
10. **Tests** — below.

## Tests
Full EditMode suite: **50 / 50 pass** (43 prior + 7 new): fresh claim grants +
starts streak, same-day double-claim blocked, consecutive days build streak, gap
resets, clock-rollback blocked, 7-day cycle wraps (day 8 == day 1 reward), and a
JSON round-trip preserves streak/last-claim/history. Determinism/replay/save
tests still green.

## Known limitations
- Rewards are coins only (no gacha pulls / item rewards yet).
- Anti-cheat is local-clock sanity only; there is no server time authority (MVP is
  offline single-player).
- On-device visual QA still needed.

## Constraints
Android primary · determinism preserved · save backward-compatible (new fields
default to 0 / empty for old saves) · no functionality removed.
