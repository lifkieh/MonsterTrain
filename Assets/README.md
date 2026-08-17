# Monster Trainer Arena — Phase 1 Scripts

Generated per the Phase 1 Battle Prototype Specification, `code-conventions.md`,
and the locked GDD decisions. No UI code (per request): the view layer
(BattleReplayView/UnitView) and the sweep EditorWindow are the only Phase 1
scripts NOT in this drop.

## Import

1. Copy `Assets/` into your Unity project (2021.3 LTS or newer).
2. Skill/species assets load via `Resources`, so generated content lives under
   `Assets/Resources/{Monsters,Skills}` — run the generator (step 4).
3. `balance.json` goes to `Assets/StreamingAssets/` (included).
4. Menu: **MTA → Generate Phase 1 Content** creates the 10-skill shared pool
   and all 12 species assets from the approved GDD tables.
5. Tests: Window → General → Test Runner → EditMode → Run All
   (`Phase1GateTests` needs no assets — it builds species as pure data, which
   is itself the zero-code content proof).

## Boot / usage

    var cfg = SpeciesDatabase.LoadBalance();
    var registry = SpeciesDatabase.LoadFromResources();
    var result = BattleSimulator.Run(teamA, teamB, seed, cfg, registry);
    // feed result.events to the replay view (Phase 1 step 15)

Sweep from anywhere (or wrap in a one-line [MenuItem]):

    var summary = BalanceSweep.Run(new BalanceSweep.SweepConfig { battles = 1000 }, cfg, registry);
    UnityEngine.Debug.Log(summary);   // P10/P50/P90, hard-resolve %, win rate
    System.IO.File.WriteAllText("sweep.csv", summary.csv);

## Implementation order (matches the spec's build order)

Enums → StatBlock → BalanceConfig(+json) → StatMath → ContentData →
MonsterInstance → LevelMath/TrainingMath → TeamConfig → BattleEvent →
BattleState → ActionTimeline → TargetSelector → SkillResolver →
BattleSimulator → SpeciesRegistryCore → BalanceSweep → Data SOs
(GrowthProfile/SkillDefinition/MonsterSpecies/SpeciesDatabase) →
Editor generator → gate tests.

## Deviations & notes (deliberate, flagged per skill rules)

- **Core purity via POCOs:** the simulator consumes `SpeciesData`/`SkillData`
  plain classes; SOs convert with `ToData()`. This keeps `Core/` free of asset
  references so the 1,000-battle sweep and all gate tests run headless.
- **`BattleSimulator.Run` takes a `SpeciesRegistry` parameter** (the spec's API
  sketch omitted it; instances resolve species by id per conventions).
- **RNG contract** documented at the top of `BattleSimulator.cs`: growth rolls
  (team A then B, slot order, stat order) → crit rolls in resolution order →
  hard-resolve coin flip. Change the order and the determinism hash test fails,
  by design.
- **`TargetRule.AllAllies`** added for the `rally` team ultimate — one loop in
  the resolver, no per-skill code.
- **Freshness decay is absent on purpose** (product-layer choice mechanic;
  Phase 1 validates the balance model, not the training UX).
- **Persona training in sweeps is grade-neutral** (B multiplier) so it composes
  before grades roll; grade-routed gains are unit-tested in `TrainingMath`.
- Levels are set directly in Phase 1; XP wiring is Build Phase 2 and must not
  change `StatMath`/`LevelMath`.
