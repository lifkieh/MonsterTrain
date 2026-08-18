# Content Expansion — Species, Career, Evolution

Date: 2026-08-18. Adds roster, career, and a monster evolution system on top of the
Phase K parity framework — all new content validated to keep the balance band.

## New species (12 → 18)
Six new obtainable species, cloned from proven archetype statlines/kits so they land
in-band without extra tuning. 2 per element, varied roles:

| species | element | role |
|---------|---------|------|
| salamander | Fire | Bruiser |
| phoenix | Fire | Mage |
| kraken | Water | Bruiser |
| jelly | Water | Support |
| treant | Nature | Tank |
| mantis | Nature | Assassin |

They auto-join every pool (unlocks, career opponents, collection) because those
derive from the registry. **20,000-battle validation: all 18 species 42.4%–55.7%**
(target 40–60%), duration P50 30.4 s, team-A 49.6%.

## Career expansion (12 → 18 stages)
`Career.Stages = 18`, two new leagues (**Champion**, **Legend**) — 6 leagues × 3
stages. Enemy level scales 5→22 across the ladder. Data-driven, so save state
(the `careerStage` frontier) stays compatible.

## Evolution system
- `SpeciesData` gains `evolvesTo`, `evolveLevel`, `evolutionOnly`.
- Three chains (level 10): **wolf → Dire Wolf**, **salamander → Inferno Drake**,
  **mantis → Blade Mantis** — stronger forms (~+40% power) that are *earned*, not rolled.
- `Progression.Evolve` transforms the owned monster **in place** (keeps level + xp),
  unlocks the evolved species. Detail screen shows an **EVOLVE** button when eligible
  (owned, ≥ evolve level), or a greyed "EVOLVE (Lv N)" hint otherwise.
- **Evolution-only forms are excluded from wild pools** (random unlocks, career
  opponents, sweeps, and the balance validation) — they're power upgrades, so they
  intentionally sit above the 40–60% band and never appear as random enemies.

## Verification
- **66 / 66 EditMode tests pass** (+3 new: evolve transforms & preserves level/xp,
  can't evolve below level, can't evolve terminal/unowned).
- Balance band re-validated with evolved forms excluded — still 42.4%–55.7%.
- Determinism preserved (self-referential hash tests green).

## Save compatibility
New `SaveData` behavior only reads existing fields; `careerStage` frontier and the
collection carry forward. Evolution mutates a collection entry's `speciesId` in place
(keeps level/xp) — old saves load unchanged.
