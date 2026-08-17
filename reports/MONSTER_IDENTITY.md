# Monster Identity

Date: 2026-08-17. Presentation pass giving each existing species a distinct
visual identity. **No balance redesign, existing species only, presentation
layer only** — simulation/outcomes/`balance.json` untouched; determinism intact.

## Files changed

**New**
- `Assets/Scripts/Meta/SpeciesIdentity.cs` — deterministic per-species color,
  icon initial, crit word, skill word (pure C#, from speciesId hash).
- `Assets/Scripts/Tests/IdentityTests.cs` — 2 tests.

**Edited (presentation)**
- `Assets/Scripts/Battle/UnitView.cs` — team frame + species body color +
  nameplate + icon badge; spawn pop + victory bounce.
- `Assets/Scripts/Battle/BattleReplayView.cs` — species colors/icons, spawn on
  build, victory anim on winners, per-species crit word + skill banner.
- `Assets/Scripts/App/GameBootstrap.cs` — monster-card redesign (color strip +
  icon), result-screen MVP showcase.
- `Assets/Scripts/Meta/BattleDrama.cs` — expose MVP species/team (top damage).

**Untouched:** `Core/` (sim/determinism) and `balance.json`.

## Tasks

1. **Unique silhouette color per species** — `SpeciesIdentity.ColorFor` maps the
   speciesId hash to a distinct hue; the unit body is tinted with it (team color
   kept as a frame so sides still read).
2. **Species nameplate** — dark plate bar with the species name atop each unit.
3. **Species icon** — circular badge with the species' initial(s) (e.g. "FL" for
   fire_lizard), on units, cards, and the MVP showcase.
4. **Spawn animation** — units pop in (scale 0.2→1, smoothstep) at battle start.
5. **Victory animation** — surviving winners bounce + pulse when the battle ends.
6. **Defeat animation** — losers play the death knockback/fade/sink; the result
   screen shows DEFEAT.
7. **Unique crit text** — per-species crit word ("SMASH!/PIERCE!/BLAST!/…") chosen
   deterministically from the speciesId, shown on crits.
8. **Unique skill banner** — per-species skill word ("Onslaught/Surge/Gambit/…")
   shown when that species casts an active/ultimate.
9. **Monster card redesign** — team-select cards get a species color strip + icon
   badge + name + stats.
10. **Result MVP showcase** — the top damage dealer is highlighted with its icon,
    color, name, and side.

## Tests

Full EditMode suite: **24 / 24 pass** (22 prior + 2 new).
- `Identity_IsDeterministic` — same species → same color + crit/skill word.
- `Identity_InitialsAndColorsVary` — correct initials; distinct species differ in
  color. Determinism/hash guarantees from earlier phases still pass.

## Performance notes

- Identity is computed once per battle (colors/initials) and per result (MVP); no
  per-frame cost. Extra UI is a few Images/Texts per unit. 60 fps target holds.

## Known limitations

- Visual correctness/feel needs on-device human QA (headless can't verify pixels).
- Icons are initials on colored badges (no art sprites yet — Build Phase 4).
- Colors are hash-derived hues; two species could land near each other in hue
  (rare); a hand-tuned palette can replace `ColorFor` later without code changes
  elsewhere.

## Constraints honored

No balance redesign · existing species only · presentation layer only ·
determinism + combat outcomes unchanged · `balance.json` untouched.
