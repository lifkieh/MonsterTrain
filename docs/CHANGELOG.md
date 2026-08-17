# CHANGELOG — Knowledge Base

Records structural changes to the project knowledge base (not gameplay/design
changes — those live in DECISIONS.md and the source docs).

---

## 2026-08-17 — PROJECT_KNOWLEDGE consolidated to a single canonical file

### What changed

- **`docs/PROJECT_KNOWLEDGE.md` is the sole canonical source of truth.**
- The earlier consolidation `PROJECT_KNOWLEDGE 2.md` was moved (not deleted) to
  `archive/superseded/PROJECT_KNOWLEDGE_2.md`.
- Nothing was merged automatically. Candidate additions are listed under
  "Proposed additions" below and await approval before touching the canonical
  file.

### Why PROJECT_KNOWLEDGE.md is canonical

It is the newer, developer-authored document and it captures the **approved
pacing review**, which post-dates the GDD and every earlier consolidation.
Where the two files disagreed, the canonical file was authoritative in **every**
case — the differences are pacing-review decisions the earlier draft never had:

| Decision | Canonical PK (kept) | Superseded PK_2 | Origin |
|---|---|---|---|
| Bronze scout rungs | **1 & 4** (pacing fix); others 3 & 6 | 3 & 6 everywhere | Pacing review |
| Depth gates (Gold/Plat/Master) | 4≥**12** / 5≥**17** / 6≥**21** | 4≥14 / 5≥20 / 6≥24 | Pacing review (retunes GDD) |
| Catch-up XP | present (2–3× below band; replays full XP, reduced coins) | absent | Pacing review |
| `rally` ultimate charge | **18 s** | 15 s (default) | Confirmed in `SpeciesAssetGenerator.cs` |
| Speedster → Support rename | recorded | not recorded | game-spec v0.2 |
| Pacing estimate | ≈20–22 calendar days, ~5 h play | absent | Pacing review |
| Governance precedence | "**this file wins** over game-spec v0.5" + v0.6 fold pending | weaker | Skill v5 governance |

The canonical file also carries material the earlier draft lacked entirely:
Lessons Learned, Executive Summary, "Never change without approval" list,
Current Repository State, and the spec-drift risk (v0.6 fold pending).

### Why PROJECT_KNOWLEDGE 2.md was superseded

It was the first pass (built straight from GDD v1.0 + game-spec v0.5 + evals)
and predates the pacing-review decisions above. On every conflicting number it
held the older value. It is kept in `archive/superseded/` for provenance, not
for use. Two `PROJECT_KNOWLEDGE` files = two sources of truth = a hazard;
retiring one restores a single source. `CLAUDE.md` already points implementers
at the canonical `docs/PROJECT_KNOWLEDGE.md`.

### Decisions adopted from each document

- **From the canonical PK (retained as-is):** all pacing-review fixes (Bronze
  scout rungs 1 & 4, retuned depth gates 12/17/21, catch-up XP, replays full
  XP / reduced coins), `rally` 18 s, Speedster→Support, the ~20–22-day pacing
  estimate, governance precedence over game-spec v0.5, and the narrative
  sections (Lessons, Exec Summary, Never-Change list, Repo State, Risks).
- **From the superseded PK_2 (candidate content only — NOT auto-merged):** the
  level-1 base-stat table, the mastery-grades retention lever, and a few
  implementation footnotes (heal formula, damage floor). Their disposition is
  in "Proposed additions" below.

### Gap check — is the canonical PK missing anything implementation-critical?

**No Phase-1-implementation-critical information is missing from the canonical
PK.** Everything PK_2 held that PK lacks is either (a) data that already lives
in code, or (b) already stated in the Phase 1 Spec / code-conventions, or (c) a
post-MVP nice-to-have. Specifically:

1. **Level-1 base-stat table** — this is *data*, not design. It already lives in
   `Assets/Scripts/Editor/SpeciesAssetGenerator.cs` (which builds the
   `MonsterSpecies` assets) and is destined to be tuned by the Stage 3 sweep.
   See the provenance trace below. **Recommendation: keep in balance data (B).**
2. **Mastery grades (flawless / swift)** — an *approved retention lever* from the
   retention eval, classified Nice-to-have / first-patch. Genuinely absent from
   the canonical PK, but **not Phase 1 critical**. Candidate addition.
3. **Heal formula** `round(INT × powerMultiplier)` (cap maxHP, no mitigation) and
   **damage floor** `max(1, …)` — implementation footnotes already present in the
   Phase 1 Spec and in `StatMath`/`SkillResolver`. Optional footnote in PK.

### Proposed additions to PROJECT_KNOWLEDGE.md — DECLINED (see Resolution)

These were candidates only and were **not applied**. All declined by the
developer on 2026-08-17 (see Resolution). Kept here for record:

1. Mastery grades (flawless / swift) retention note.
2. Consolidated Nice-to-have list.
3. Base-stat pointer.
4. Heal-formula + damage-floor footnote.

### Resolution (2026-08-17)

Developer decisions, recorded per instruction:

- **`PROJECT_KNOWLEDGE.md` remains the canonical source of truth** and is not to
  be modified.
- **`PROJECT_KNOWLEDGE_2.md` has been archived** (`archive/superseded/`).
- **The level-1 base-stat values are v0 balance data originating from the
  Balance Sheet** — balance data, not approved game design. They stay **out of**
  `PROJECT_KNOWLEDGE.md`.
- **Balance values should eventually live in `balance.json`**, not in project
  design documentation.
- No mastery grades, heal formulas, damage floors, or other implementation
  details are added to `PROJECT_KNOWLEDGE.md` at this time.
- Project is in **implementation preparation mode**: documentation changes are
  **frozen** unless required to unblock implementation. Next focus: **P0-1**
  (Unity project setup + script import).

---

## Provenance trace — level-1 base-stat table

**Question:** is the level-1 base-stat table (1) approved design, (2) balance
draft, or (3) test artifact?

**Verdict: (2) balance draft** — origin traced to the Balance Sheet eval, still
labeled first-pass / v0, explicitly meant to be tuned by simulation.

Evidence:

| Source | Contains the base-stat numbers? | What it actually holds |
|---|---|---|
| **GDD v1.0** | No | Roster table has role, silhouette, and growth *tendencies* (grades) + training preference — no base stat numbers. |
| **Phase 1 Spec v1.0** | No | Describes the model and `MonsterSpecies.baseStats` as a field; gives no per-species numbers. |
| **Balance Sheet (test-3)** | **Yes — the source** | "First-pass level-1 balance sheet"; full HP/ATK/DEF/SPD/INT/LUCK table. Scope: "MVP Safe — numbers and curves only." Recommendation: "Adopt as **v0 of balance.json** … re-tune from the duration histogram rather than from feel." |
| **PROJECT_KNOWLEDGE.md (canonical)** | No (by design) | Records tier multipliers + growth tendencies; deliberately keeps base numbers out of the design doc. |
| **PROJECT_KNOWLEDGE 2.md (superseded)** | Yes | Reproduced the table verbatim under "Level-1 base stats (v0 of balance.json, from the balance sheet)." |
| **`SpeciesAssetGenerator.cs` (code)** | **Yes — live copy** | Hardcodes the same numbers to build the SO assets (e.g. `Species("slime", … B(120,16,18,8,10,6) …)`). Values match the Balance Sheet exactly. Comment says "GDD base stats" but the GDD has no such numbers — the true origin is the Balance Sheet. |

Corroboration: the same generator sets `rally` charge = 18 s and per-species
skill sets, and `balance.json` ships `"speciesGainRates": []` (empty) with a
"run once, then tune in the inspector" note — all consistent with **draft-to-be-
tuned**, not locked design.

**Recommendation: B — keep separate in balance data.**

- These are draft v0 values whose purpose is to be tuned by the Stage 3 sweep;
  promoting them into the canonical design doc would misrepresent draft numbers
  as locked and create a second place to maintain them.
- The canonical PK's boundary — design intent in the doc, numbers in balance
  data — is correct and should hold.
- They already have a home (`SpeciesAssetGenerator.cs` → `MonsterSpecies`
  assets). *Future task (not a doc edit):* lift them out of generator code into
  `balance.json`/CSV so they are tunable without recompiling — reinforces the
  zero-code-content invariant.
- The only doc change worth making is a **pointer** in PK (proposed addition #3),
  not the table itself.

Rejected alternatives: **A. Promote to PK** — no; freezes draft numbers as
design. **C. Archive** — no; they are live v0 seed values in active use by the
generator, not dead.
