# Phase V — Achievements

Date: 2026-08-19 · Presentation/meta only · Author: Lifkie Lie

## Design
Pure-C# `Achievements` system (Meta, edit-mode tested). Each achievement is a predicate
over `SaveData` (+ total species for the collection goal). Unlocked ids persist in
`SaveData.achievements : List<string>` (additive/backward-compatible).

11 achievements: First Blood (first win), Metamorphosis (evolve), Combo Master (15-hit),
Collector (own 10), Unstoppable (5-win streak), League Champion (complete a league),
Evolution Master (evolve 3), Dedicated Trainer (train 10×), Seasoned Trainer (Lv 10),
Veteran (win 50), Master Collector (discover every monster).

## Save / popup / screen
- **Save support:** `s.achievements`; `Achievements.CheckNew(save, totalSpecies)` unlocks
  any newly-earned ones (idempotent) and returns them for toasts.
- **Popups:** `CheckNew` is called after every battle, evolve, train, and quest claim;
  newly-earned achievements surface as a "★ Achievement: <title>" line in the result/reward
  text and as an ACHIEVEMENT! popup on the detail screen.
- **Achievement screen** (menu button): full list with medal (★ gold earned / ? dim locked),
  title, and description; header shows "X / N unlocked".

## Tests / build
- `MetaSystemsTests`: unlock-once + idempotent, dexmaster needs full roster.
- EditMode 75/75, PlayMode smoke PASS.

## Human QA (device)
- [ ] Winning your first battle pops "First Blood"; it shows earned on the Achievements screen.
- [ ] Locked achievements read as ? with their description; earned show gold ★.
- [ ] No achievement unlocks twice.
