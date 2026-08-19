# Phase T — Onboarding (First Launch Experience)

Date: 2026-08-19 · Presentation/meta only · Author: Lifkie Lie

## What was built
A guided first-launch coach flow, gated on the new `SaveData.onboarded` flag (default
false → runs once; backward-compatible additive field). On a fresh profile the game now
opens on Onboarding instead of dropping the player cold onto the Daily screen.

Six skippable pages (`GameBootstrap.OnbPages`), each a title + body on a card with a
NEXT (n/6) button and a SKIP shortcut:
1. **Welcome** — you raise monsters, pick a team of 3, battles auto-resolve, you watch.
2. **Elements** — Fire→Nature→Water→Fire advantage triangle, spelled out (fixes the S
   finding that the triangle was never surfaced).
3. **Roles** — Tank / Bruiser / Assassin / Mage / Support explained.
4. **Grow stronger** — win → XP + coins; TRAIN with coins to level faster.
5. **Evolution** — high-level monsters EVOLVE to a stronger form on the detail screen.
6. **Goals & rewards** — Daily, Quests, Achievements, Dex; then "pick your first team".

The last page ("START PLAYING") sets `onboarded = true`, saves, and drops the player
straight into their first team selection (`StartGame`). SKIP does the same immediately.

## Integration / flow fixes shipped alongside
- **TeamSelect BACK** button added (was a dead-end per audit) — returns to Career map if
  in career, else Menu; plus a "Pick 3 monsters to begin" helper line.
- **Android hardware-back / Esc** now handled per-phase (closes popups/dex-detail first,
  else navigates back); previously the OS back did nothing.
- Menu subtitle changed from dev text "first playable" → "Raise. Evolve. Conquer.".

## Tests / build
- EditMode 75/75 (incl. new meta tests). PlayMode smoke PASS (boots with onboarding as the
  first phase on a fresh save, 0 runtime errors). Determinism/save-compat untouched.

## Human QA (device)
- [ ] Fresh install opens on the welcome flow; NEXT walks all 6 pages; SKIP jumps out.
- [ ] After finishing you land on team select and can play immediately.
- [ ] A returning player (save exists) never sees onboarding again.
- [ ] Back gesture works on every screen.
