# Consolidation Report — Train Your Monster

Date: 2026-08-17. Task: consolidate multi-session project documents into a
single knowledge base and prepare for implementation. **No gameplay code
written; no design changed.**

## What was done

1. Located the project at `OneDrive/Desktop/TrainYourMonster` and read every
   source document (GDD v1.0, `.skill` bundle = SKILL.md + game-spec v0.5 +
   code-conventions, Phase 1 battle-prototype spec, the Phase 1 scripts zip, and
   evals test-1/3/4/5/6).
2. Created folders `docs/`, `archive/`, `reports/`.
3. **Archived** all 9 original source files into `/archive` (verified by
   SHA-256 checksum match before removing the copies from `docs/` — no data
   lost, nothing deleted from outside the project).
4. Authored the consolidated knowledge base in `docs/` and `CLAUDE.md` at the
   repo root.

## Files created

- `CLAUDE.md` (repo root) — session bootstrap: what to read, source of truth,
  goals, forbidden work, priorities.
- `docs/PROJECT_KNOWLEDGE.md` — primary source of truth (vision → loop → MVP →
  all systems → architecture → data model → formulas → roster → decisions →
  waves → rules → risks).
- `docs/PROJECT_STATUS.md` — per-area state + Phase 1 success-criteria status.
- `docs/TASKS.md` — prioritized Phase 1-only task list (P0→P3).
- `docs/DECISIONS.md` — approved decisions with reasons + approved rejections.
- `docs/ROADMAP.md` — Compile → Test → Simulate → Balance → Debug Viewer path.
- `reports/CONSOLIDATION_REPORT.md` — this file.

## Folder structure (after)

```
TrainYourMonster/
  CLAUDE.md
  docs/
    PROJECT_KNOWLEDGE.md
    PROJECT_STATUS.md
    TASKS.md
    DECISIONS.md
    ROADMAP.md
  archive/            # originals, preserved (checksum-verified)
    monster-trainer-arena-gdd-v1.md
    monster-trainer-arena.skill
    mta-phase1-battle-prototype-spec.md
    mta-phase1-scripts.zip
    test-1-data-layer.md
    test-3-balance-sheet.md
    test-4-training-redesign.md
    test-5-retention-bronze-wall.md
    test-6-asset-stack.md
  reports/
    CONSOLIDATION_REPORT.md
```

## Key findings

- **Design is complete and internally consistent.** Vision, GDD, architecture,
  and the Phase 1 spec all agree.
- **Phase 1 Core code exists but is unverified.** ~1,322 lines across Core/Data/
  Editor/Tests + `balance.json`, no stubs/TODOs — but never imported into Unity,
  compiled, or run. This is the single biggest gap.
- **No Unity project exists yet** at the repo; the scripts are a portable
  `Assets/` drop awaiting import.
- **Balance is unverified.** `balance.json` holds v0 constants; `speciesGainRates`
  is empty; no sweep has produced duration/fairness data.
- **The debug viewer, sweep EditorWindow, and Android build are not started**
  (view scripts + window wrapper were intentionally excluded from the drop).

## Discrepancies preserved (not resolved silently)

- **Roster numbering:** the balance sheet (test-3) orders the 12 monsters
  differently (Bat #3, Fire Lizard #11) than the GDD/game-spec (Fire Lizard #3).
  Same species/roles/growth tendencies; only row order differs. PROJECT_KNOWLEDGE
  uses the GDD numbering as canonical and flags the difference.
- **Working title:** repo = "Train Your Monster", docs = "Monster Trainer
  Arena." Recorded as the same project in CLAUDE.md and PROJECT_KNOWLEDGE.

## Open items to flag to the developer

- huberthart asset-pack license is **unverified** — confirm before any purchase
  (purchases belong to Build Phase 4 regardless).
- Repo is **not under git** — recommend `git init` before implementation begins.

## Next implementation task

**TASKS.md → P0-1: stand up a Unity 2021.3 LTS+ project, import the archived
scripts drop, and reach zero compile errors** (then P0-2 generate content, P1-1
run the gate tests). Nothing downstream can be verified until the code compiles
and runs.
