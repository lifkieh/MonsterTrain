# Retention Audit — as a new player

Date: 2026-08-19 · Author: Lifkie Lie · Presentation/meta only (no PvP, no gacha, no
balance/sim/determinism/save-compat change).

## Q1 — "Why open the game again tomorrow?"
**Before:** a 7-day daily-reward cycle (50→200 coins, then it wraps flat) + daily quests.
Real, but the daily reward stopped escalating after a week, so day 8 felt the same as day 1.

**Now, the reasons to return tomorrow:**
- **Daily reward** (7-day cycle) + **daily quests** (Play 3 / Win 2 / Train 1 — reset each day).
- **NEW: login-streak milestones.** Keeping a consecutive streak pays escalating one-time
  bonuses at **Day 3 (+150), Day 7 (+400), Day 14 (+900), Day 30 (+2500)**. The Daily screen
  always shows the *next* streak bonus ("Next streak bonus: Day 7 = +400"), so there is a
  concrete, growing reason to not miss a day.

## Q2 — "Why keep playing for 30 days?"
**Before:** the long-tail was thin — collect 21 monsters (unlock by level + evolve 3), an
18-stage career, 11 achievements, 10 quests. A committed player could exhaust the named goals
in well under a month, then had nothing pulling them forward.

**Now — four chase loops, each extended to weeks of play:**

### 1. Collection completion
- The **Monster Dex** tracks discovered %, and the **Trainer Profile** shows Dex / Collection
  / Career / Achievements completion bars.
- **Reaching 100%** requires seeing every enemy AND evolving the three evolution-only forms —
  now an explicit goal via the **"Discover all monsters" milestone quest** and the
  **"Master Collector" achievement**.

### 2. Evolution chase
- Evolving each of the three lines (Wolf→Dire Wolf, Mantis→Blade Mantis, Salamander→Inferno
  Drake) is surfaced as the **"Evolve 3 monster lines" milestone quest** and the
  **"Evolution Master" achievement** — and it's a prerequisite for 100% collection, so the
  three chases reinforce each other.

### 3. Achievement chase
- Extended from 11 → **15 achievements**, adding genuinely long-tail ones:
  **Centurion** (win 100), **Peak Form** (raise a monster to Lv 30), **Grand Champion**
  (complete all six leagues), **Devoted** (30-day login streak). There is always a next one.

### 4. Quest chase
- Milestones extended with **Win 100**, **Discover all 21**, **Evolve 3 lines**, **Reach
  Trainer Lv 30** (14 quests total). Daily quests keep the short loop fresh; milestones anchor
  the multi-week loop. The menu QUESTS button badges claim-ready quests; the Profile lists the
  next incomplete goals.

## What was deliberately NOT added
- **No PvP, no gacha** (per the brief). No new mechanics, monsters, or balance changes.
- No "energy"/timers or dark patterns — retention comes from *goals and progress*, not
  friction.

## Systems added this pass (all additive / backward-compatible)
- `Assets/Scripts/Meta/Streaks.cs` — login-streak milestone ladder (Day 3/7/14/30).
- `SaveData.streakMilestones` (+ null-guard in `SaveSystem.Load`).
- `Achievements.Defs` +4 long-tail; `Quests.Defs` +4 milestones.
- `GameBootstrap`: streak-milestone rewards granted + surfaced on the Daily screen; new
  achievements fire on claim.

## Retention shape (summary)
- **Day 1–7:** onboarding → first team → career start → daily reward + daily quests + first
  achievements; streak building toward Day-3 and Day-7 bonuses.
- **Week 2–4:** collection/evolution completion, career leagues, milestone quests (win 100,
  discover all, max a monster), Day-14 and Day-30 streak payouts, long-tail achievements.
- **After 100%:** the profile shows completion; remaining hooks are the daily loop + maxing
  every monster. (A seasonal/live-ops layer is the natural next step but is out of scope and
  needs a config/calendar — noted, not built.)

## Validation
- EditMode 75→76 (new Streaks test), PlayMode smoke PASS, Android APK builds.

## Human QA (device)
- [ ] Daily screen shows the next streak bonus; hitting Day 3/7/14/30 pays the bonus once.
- [ ] Quests/Achievements now show the long-tail goals (win 100, discover all, evolve all,
      Lv 30, 30-day login) and they complete over real play.
- [ ] Profile completion bars move toward 100% as you collect/evolve/win.
