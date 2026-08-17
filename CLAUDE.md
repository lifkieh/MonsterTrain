# CLAUDE.md — Train Your Monster (Monster Trainer Arena)

Instructions for every future Claude session on this repository. Read this
first, then the source-of-truth docs, before doing anything.

## What this project is

A solo-developer, Android-first **monster raising + auto-battler** built in
Unity (C#), internally named *Monster Trainer Arena (MTA)*. Primary goal:
**release an MVP on the Play Store within 3–6 months.** Core fantasy:
**"I raised this monster."**

## Read these first (in order)

1. **`docs/PROJECT_KNOWLEDGE.md`** — the primary source of truth. Consolidated
   vision, loop, MVP scope, every approved/rejected system, architecture, data
   model, formulas, roster, and rules.
2. **`docs/PROJECT_STATUS.md`** — current state of each area (COMPLETE / IN
   PROGRESS / NOT STARTED / BLOCKED) and Phase 1 success-criteria status.
3. **`docs/DECISIONS.md`** — the approved decisions (with reasons) you must not
   silently contradict.
4. **`docs/TASKS.md`** — the prioritized Phase 1 task list. Work top-down.
5. **`docs/ROADMAP.md`** — the Compile → Test → Simulate → Balance → Debug
   Viewer execution path to a playable Phase 1 prototype.

Deep design detail and the original session documents live in **`/archive`**
(GDD v1.0, game-spec v0.5, code-conventions, Phase 1 spec, the Phase 1 scripts
zip, and evals). `/reports` holds analysis outputs (start with
`CONSOLIDATION_REPORT.md`).

## Source of truth

`docs/PROJECT_KNOWLEDGE.md` is authoritative. It supersedes memory and any
single archived doc. The archived `game-spec.md` and `code-conventions.md`
remain the canonical detail references *for the numbers and code model they
define*; where they and PROJECT_KNOWLEDGE ever disagree, fix the discrepancy
explicitly and update both — never guess. speciesIds and skillIds are
**append-only** (never rename/remove — saves depend on them).

## Current goal

Get the **Phase 1 Battle Prototype** playable and verified. The Phase 1 Core
scripts are already drafted (see the archived scripts zip) but have **never been
imported into a Unity project, compiled, or run.** The immediate job is to make
them live and prove the four Phase 1 questions with evidence. Follow `TASKS.md`
from **P0-1**.

## Forbidden work (do NOT do any of this now)

- **Do not modify the gameplay design, add mechanics, or redesign approved
  systems.** The design is locked and consolidated.
- **Do not build or design:** evolution · traits · equipment · PvP ·
  multiplayer · open world · farming · crafting · guilds · breeding · gacha ·
  battle pass · daily missions · social/live-service features. (Some are Wave
  1–3 seams only — seams, not implementations.)
- **Do not start Build Phase 2+ work** (coins/timers, save/load, career UI,
  capture flow, production UI) while Phase 1 is unfinished.
- **Do not purchase or commit to assets** — placeholders only through Build
  Phase 3; the huberthart pack license is **unverified**.
- **Do not delete or overwrite anything in `/archive`** — it is the preserved
  project history.

## Priorities & how to work

1. **Shipping > Polish · Fun > Features · Retention > Complexity.**
2. Obey the **two-week rule** (>2 weeks solo effort → default Delay/Reject) and
   the **fun check** before entertaining any new feature.
3. **Data-driven or it's wrong:** adding a monster or skill must require data
   only — no code. Keep the layer separation strict: `Core/` computes and never
   renders; `Battle/` renders and never computes; `Data/` declares; `Editor/`
   verifies.
4. **Balancing rule:** state damage/attack-speed/crit assumptions + target
   duration before any numbers; tune `balance.json` values, never formula
   shapes.
5. **Decision persistence:** when a real decision changes, update
   `docs/PROJECT_KNOWLEDGE.md` + `docs/DECISIONS.md` (and the archived spec if
   the number lives there). Never change specs silently.
6. This is not yet a git repo — consider `git init` before large changes so work
   is recoverable.

## Working title note

Repo folder = "Train Your Monster"; design docs = "Monster Trainer Arena." Same
project; don't treat them as two.
