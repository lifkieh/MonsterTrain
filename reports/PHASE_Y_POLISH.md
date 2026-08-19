# Phase Y — Polish

Date: 2026-08-19 · Presentation only · Author: Lifkie Lie · Target: 1080×2340 portrait

Every screen was audited (Phase S) and the concrete issues fixed. Highlights:

## Fixed
- **Side clipping on real devices (High).** Canvas `matchWidthOrHeight` 0.5 → **0 (match
  width)**. At 19.5:9 (1080×2340) the old 0.5 blend shrank usable width to ~±489 and cut
  ~51px off each side of every grid/screen; match-width pins the design to ±540 so nothing
  clips horizontally on 1080-wide phones.
- **Vertical envelope (cross-device).** All new screens (Onboarding, Quests, Achievements,
  Dex, Trainer Profile) were pulled into the same ±900 vertical envelope the existing
  screens use, so they also fit shorter 16:9/18:9 phones (not just the 2340 target). Dex
  tiles and achievement rows resized to fit 7 rows / 11 rows on-screen.
- **Android hardware-back / Esc (High).** Now handled per-phase (closes popups and the dex
  detail first, then navigates back). Previously the OS back gesture did nothing.
- **TeamSelect dead-end (High).** BACK button added (returns to Career map if in career,
  else Menu) + a "Pick 3 monsters to begin" helper line.
- **Raw snake_case leaks (High/Med).** The "NEW MONSTER!" popup and the result screen's
  "Unlocked:" / "Leveled up:" lines now show Title-Case names ("Wolf reached Lv 4") via
  `Nice()`/`NiceLeveled()` instead of `flame_pup` / `wolf 3->4`.
- **Menu redesign.** Single overflowing column → title + tagline + **Lv/coins wallet** +
  big PLAY + a 2-column grid (Career, Quests, Daily, Collection, Dex, Achievements,
  Progress, Settings) + Quit. QUESTS shows a "(N)" claim-ready badge. Dev subtitle "first
  playable" → "Raise. Evolve. Conquer."; the misleading duplicate "CONTINUE" was removed.
- **About copy.** Dev "MVP soft-launch candidate" → proper CC0 asset credits
  (isaiah658 / CodeManu / Kenney / CleytonRX / rubberduck) + thanks.
- **Progress screen** rebuilt from a raw text dump into the Trainer Profile dashboard
  (Phase X) — readable stat blocks, completion bars, and next-goal list.

## Deferred (documented, non-blocking)
- Skip-to-result button in battle (Medium) — speed is already 0.5–4×; a true skip needs
  result-plumbing care; left for a later pass.
- Enemy-lineup preview on team select (Medium) — casual enemy is rolled at StartBattle.
- Quit confirmation dialog (Low), detail-art micro-overlap with the XP bar (Low).
- Returning players still open on the Daily reward if claimable (a positive, not the cold
  Daily-first that new players used to get — new players now get onboarding first).

## Tests / build
- EditMode 75/75, PlayMode smoke PASS (walks every screen, 0 runtime errors, layout
  warnings are warn-only at the batch's 640×480 window).

## Human QA (device, 1080×2340)
- [ ] Nothing clips at the left/right edges on any screen; grids sit inside the frame.
- [ ] Back gesture works everywhere; menu reads cleanly; names are all Title-Case.
- [ ] New screens (Quests/Achievements/Dex/Profile) fit without top/bottom cut-off.
