# Monster Trainer Arena — Phase 1 Battle Prototype Specification v1.0

**Active Role:** Lead Architect. (Technical design explicitly requested. All
locked decisions honored; nothing below redesigns an approved system.)
Compliant with `game-spec.md` v0.5, `code-conventions.md`, and the approved
GDD + pacing review. Balance model stated before numbers, per the balancing
rule.

---

# Objective

Phase 1 exists to answer four questions with evidence, not opinion:

1. **Does the damage model land battles in 30–90 seconds?** If not, every
   downstream system is built on sand.
2. **Does preparation convert to winning?** A trained/leveled team must
   measurably beat an untrained one — this is the core fantasy ("I won because
   I prepared correctly") expressed as a test.
3. **Does the data pipeline hold?** Adding a monster or skill must require
   data only. Proven, not promised.
4. **Is combat deterministic?** Same inputs + same seed = identical battle,
   byte for byte. Required for replays and honest balancing.

Training and leveling are included as **mechanics** (pure functions + debug
triggers), not as product features — no timers, no fees, no screens. That is
the smallest version that lets the simulator test trained monsters.

# Success Criteria

Phase 1 is complete when ALL of the following pass:

1. **Determinism test:** identical teams + seed produce an identical event-log
   hash across 100 repeated runs.
2. **Duration sweep:** 1,000 simulated battles across random valid same-level
   comps yield P10 ≥ 30 s and P90 ≤ 90 s of sim time; ≤ 5% reach the 120 s
   hard resolve; 0 battles fail to terminate.
3. **Mirror fairness:** 2,000 mirror-comp battles produce a 50% ± 3% win rate
   (no side bias).
4. **Preparation signal:** a team given 10 levels + 10 training units per
   monster beats its untrained mirror in ≥ 75% of 1,000 battles.
5. **Zero-code content test:** an automated test constructs a 13th species
   purely from data at runtime and completes a battle with it.
6. **Mechanics correctness:** unit tests for stat math, level gains, and
   training gains match the spec formulas exactly.
7. **Device proof:** a debug replay scene plays any recorded battle on an
   Android device build.

# Prototype Scope

**Included:** headless deterministic simulator · event log · seeded RNG ·
StatMath (single stat pipeline) · level-up and training gain functions with
debug triggers (editor buttons / console) · `balance.json` loader →
`BalanceConfig` · all 12 species as ScriptableObjects · a shared placeholder
skill pool (~10 skill assets reused across species) · variable team sizes
(1v1, 2v2, 3v3 — locked by Bronze onboarding) · buff/debuff as a single timed
stat-modifier implementation · anti-stall + hard resolve · balance sweep
editor tool with CSV output · debug battle replay scene (placeholder art:
CraftPix free golems / colored quads) · Android device build of the debug
scene.

**Excluded:** save/load, coins/fees/timers, career mode, leagues, gates,
capture flow, nicknames, XP-from-battles wiring (levels are set directly in
Phase 1), production UI, final art/VFX/SFX, store listing, per-species skill
sets (Phase 3 content pass authors those; Phase 1 uses the shared pool),
anything on the forbidden list.

# Battle Flow

1. **Load:** resolve each unit's `speciesId` via the registry; compute
   effective StatBlocks through `StatMath` (base + level gains + allocated +
   trained).
2. **Init:** build `BattleState` (HP = effective HP, cooldowns = 0, ultimate
   uncharged); seed `System.Random(seed)`; emit `BattleStart`.
3. **Schedule:** every unit gets `nextActionTime = interval(SPD)` (staggered
   by tie-break rules so openings aren't simultaneous).
4. **Loop — advance clock** to the smallest `nextActionTime`.
5. **Select action** for the acting unit per AI rules (ultimate → active →
   basic).
6. **Select target** per targeting rules for that skill's effect.
7. **Resolve:** roll crit, apply mitigation, apply anti-stall multiplier,
   floor at 1, apply HP/modifier change; emit `UnitAction` (with full math in
   the event) and `UnitDied` if lethal.
8. **Reschedule** the actor: `nextActionTime += interval(SPD)`; tick
   cooldowns/modifier expiries against the clock.
9. **Check end:** a side has no living units → emit `BattleEnd(victory)`;
   clock ≥ 120 s → hard resolve; else goto 4.
10. **Return** `BattleResult { winner, simDuration, endReason, events[] }`.

The view layer never runs this loop; it replays `events[]`.

# Team Structure

- **Team size:** 1–3 units per side. The simulator takes any combination
  (1v1, 2v2, 3v3, and asymmetric for debugging). Career uses the locked
  1v1 → 2v2 → 3v3 Bronze ramp, then 3v3.
- **Positions:** slots 1–3; slot 1 is front. Slot order is the only formation.
- **Target selection (deterministic, total ordering):**
  - Basic attacks → lowest-index *living* enemy slot (the front-most).
  - Damage skills → living enemy with lowest current HP; tie → lower slot.
  - Heals → living ally with lowest HP *percentage*, excluding full-HP
    allies; tie → lower slot.
  - Buffs → self unless the skill data says ally (then lowest-HP% ally).
  - Debuffs → same rule as damage skills.

# Turn System

- **SPD behavior:** attacks per second `aps(SPD) = 0.02 × min(SPD, 25) +
  0.01 × max(0, SPD − 25)`, hard-capped at 1.0 aps. The kink at 25 is the
  locked SPD-stacking brake. Action interval = `1 / aps`.
- **Turn order:** continuous action timeline — no rounds. Next actor = lowest
  `nextActionTime`; ties break by higher SPD, then team A before team B, then
  lower slot index. Ties must be totally ordered or determinism dies.
- **Action frequency:** SPD 10 → one action per 5 s; SPD 25 → one per 2 s;
  SPD 40 → one per ~1.54 s. Buffed SPD re-computes the *next* interval only
  (never retroactively reschedules).

# Combat Formula

All constants live in `balance.json`. Keep exactly these; tune values, not
shapes.

```
mitigation(DEF)   = 1 − DEF / (DEF + K)              K = 50
rawDamage         = scalingStat × powerMultiplier     (ATK or INT per skill)
critChance        = min(LUCK × 0.005, 0.30)           crit multiplier = 1.5
damage            = max(1, round(rawDamage × critMult? × mitigation × stallMult))
heal              = round(INT × powerMultiplier)      capped at maxHP, no mitigation
```

- **Miss chance: none.** Misses are pure variance and directly undermine
  "I won because I prepared correctly." Accuracy is not a stat in the spec;
  do not add one.
- **Defense interaction:** DEF only ever appears inside `mitigation()`. One
  formula, one place (`StatMath`), everywhere.

# AI Behavior

Priority per action (first valid wins):

1. **Ultimate** if charged (see Skills) and a valid target exists.
2. **Active skill** if cooldown ≤ 0 and a valid target exists (a heal with no
   injured ally is invalid).
3. **Basic attack** (always valid while any enemy lives).

Fallbacks: dead units are removed from the timeline; if a chosen target dies
between selection and resolution (impossible in sequential resolution, but
guard it), reselect; if reselection fails, the action becomes a basic attack;
if no enemies live, the battle is already over. No randomness in AI — all
choice is rule-based so outcomes are attributable to preparation.

# Skills

**System:** `SkillDefinition` ScriptableObject per `code-conventions.md`:
`skillId, displayName, slot (Basic/Active/Ultimate), scalingStat (ATK|INT),
powerMultiplier, cooldownSeconds, effect (Damage|Heal|Buff|Debuff), magnitude,
duration, targetRule`. The resolver switches on `effect` — four cases, no
per-skill code, expandable by adding enum values later.

- **Basic Attack:** `powerMultiplier = 1.0`, no cooldown; it is the default
  action. Two shared assets: `strike` (ATK) and `zap` (INT).
- **Active Skills:** 2.5–3.0× budget on 6–10 s cooldowns. Shared pool:
  `power_strike` (ATK dmg), `spark_burst` (INT dmg), `mend` (heal, 2.5×),
  `war_cry` (buff: +20% ATK, 8 s), `slow_hex` (debuff: −20% SPD, 8 s).
- **Ultimate Skills:** charge once per battle — available when battle clock ≥
  `chargeTime` (default 15 s, per-skill data) — 3.5–4.0× budget, authored so
  no single hit exceeds ~45% of an average same-level HP pool (enforced by a
  data-validation test). Shared pool: `savage_rend` (ATK), `mind_blast`
  (INT), `rally` (team buff +15% ATK/SPD, 10 s).
- **Passive Skills: none.** The locked skill model is Basic/Active/Ultimate —
  exactly 3 per monster. Passives would be a fourth slot and new scope;
  self-check verdict: not required for Phase 1, rejected.
- **Buff/Debuff implementation (the one stateful piece):** a timed modifier
  `{stat, ±percent, expiresAt}` applied to effective stats at read time.
  One class, both effects.

Species→skill assignment for Phase 1 is a data table mapping each of the 12
to three pool skills matching its role (casters get `zap`, tanks get `war_cry`
line, etc.). Per-species signature skills are Phase 3 content authoring.

# Win Conditions

- **Victory:** all enemy units at 0 HP. Resolution is sequential, so
  simultaneous wipes cannot occur; the acting side wins the edge case by
  construction.
- **Defeat:** all friendly units at 0 HP.
- **Hard resolve (120 s):** winner = higher `Σ(currentHP / maxHP)` across the
  side's starting units; tie → more living units; tie → deterministic
  seed-derived coin flip. `endReason` records `HardResolve` so sweeps can
  count it.

# Battle Duration Targets

- **Target:** 30–90 s sim time (P10/P90 of the sweep).
- **Minimum:** battles under 15 s are flagged by the sweep as burst anomalies
  (not failures — signals for tuning).
- **Maximum:** 120 s absolute, unreachable in a healthy tuning.
- **Anti-stall:** global damage multiplier
  `stallMult(t) = 1 + 0.05 × floor((t − 75) / 10)` for `t > 75`, else 1.
  Applied inside the damage formula, logged as `AntiStallTick` events, so
  tank-heavy comps end by escalation rather than boredom.

# Data Requirements

- **MonsterSpecies (SO):** `speciesId` (lowercase_snake, append-only),
  `displayName`, `baseStats: StatBlock`, `growth: GrowthProfile` (per-stat
  weights over D–S), `basicSkill / activeSkill / ultimateSkill` refs,
  `portrait`, `battleSprite` (placeholders in Phase 1).
- **MonsterInstance (plain C#):** `instanceId`, `speciesId`, `level`,
  `growth: GrowthTier[6]` (rolled at creation), `allocated: StatBlock`,
  `trained: StatBlock`, `unspentPoints`. (Nickname/exp fields exist per
  conventions; unused in Phase 1.)
- **Skills (SO):** fields listed in the Skills section.
- **Teams:** `TeamConfig { units: List<UnitConfig> }` where `UnitConfig =
  { speciesId, level, growthOverride?, allocated, trained }` —
  `growthOverride` lets tests pin grades; otherwise grades roll from the seed.
- **balance.json:** `K`, aps curve constants + cap, crit constants,
  `tierMultipliers {D:0.6, C:0.8, B:1.0, A:1.25, S:1.5}`, per-species gain
  rates, training yields per type, anti-stall constants, `hardResolveTime`,
  skill-budget guidelines. Loaded once into an immutable `BalanceConfig`.

# Balance Assumptions

Stated per the balancing rule; these are the model every sweep interprets:

- **Damage:** `ATK × mult × (1 − DEF/(DEF+50))`; skills scale INT at 2.5–4×
  on 6–15 s cadence; ultimates ≤ 45% of an average HP pool.
- **SPD:** 0.02 aps per point to 25, 0.01 beyond, cap 1.0 — action economy is
  the strongest multiplier and is deliberately braked.
- **Crits:** LUCK × 0.5%, cap 30%, ×1.5 — bounded variance so preparation,
  not dice, decides.
- **Context:** level parity within a sweep; no equipment, no items; expected
  TTK at parity ≈ 6–10 s for squishy units under focus, 15–25 s for tanks;
  target battle duration 30–90 s.

# Headless Simulator Design

- **Location & purity:** `Assets/Scripts/Core/` — plain C#, zero UnityEngine
  scene dependencies (compiles in an edit-mode test assembly).
- **API:** `BattleResult BattleSimulator.Run(TeamConfig a, TeamConfig b,
  int seed, BalanceConfig cfg)`.
- **Determinism:** one `System.Random(seed)` stream, consumed in a documented
  fixed order (grade rolls first if any, then crit rolls in resolution order);
  total tie-ordering everywhere; doubles are fine *per platform* — balancing
  runs on the dev machine, and devices replay recorded logs rather than
  re-simulating, so cross-platform float drift cannot desync anything.
- **Event log:** ordered `BattleEvent` records — `BattleStart`,
  `UnitAction {t, actor, skillId, target, raw, crit, mitigated, final}`,
  `ModifierApplied/Expired`, `AntiStallTick`, `UnitDied`, `BattleEnd
  {winner, endReason}`. The log is the *only* contract between sim and view,
  and its hash is the determinism test.
- **Sweep runner:** editor window `MTA/Balance Sweep` — inputs: battle count,
  level, comp generator (random-role-valid comps, mirrors, or explicit
  teams), persona (untrained / trained-N); outputs: CSV per battle + summary
  (duration P10/P50/P90, hard-resolve %, win rates, sub-15 s anomalies) and
  pass/fail against the success-criteria thresholds. 1,000 battles must run
  in seconds — no allocations inside the loop beyond the log.

# Testing Plan

- **Unit tests (edit mode):** StatMath per-tier level gains; training gain
  math; mitigation values at DEF 0/25/50/100; crit chance cap; aps curve at
  SPD 10/25/40 + cap; targeting tie-breaks (scripted states); cooldown and
  ultimate-charge timing; anti-stall multiplier at t = 74/75/85/120; modifier
  apply/expire; damage floor of 1; data validation (unique ids, 3 skills per
  species, ultimate budget cap).
- **Simulation tests:** determinism hash ×100; mirror fairness 50 ± 3%;
  termination ≤ 120 s across 1,000 random battles; 1v1 / 2v2 / 3v3 /
  asymmetric all complete; 13th-species-from-data test; log-replay
  consistency (final HP recomputed from events matches state).
- **Balance tests (the Phase 1 gate):** duration percentile sweep;
  preparation-signal test (trained ≥ 75% vs untrained mirror); per-species
  1v1 round-robin table exported as informational CSV for Phase 3 tuning.

# Unity Architecture

```
Assets/
  Scripts/
    Core/                    // plain C#, no scene deps
      StatMath.cs            // the ONLY stat/damage/mitigation math
      BalanceConfig.cs       // immutable, loaded from balance.json
      BattleSimulator.cs     // Run(); owns the loop
      BattleState.cs         // units, HP, cooldowns, modifiers, clock
      ActionTimeline.cs      // next-actor selection + tie ordering
      TargetSelector.cs      // all targeting rules
      SkillResolver.cs       // switch on effect kind
      BattleEvent.cs         // event records + log hashing
      LevelMath.cs           // level gains (tier multipliers)
      TrainingMath.cs        // training yields
    Data/
      MonsterSpecies.cs  GrowthProfile.cs  SkillDefinition.cs
      SpeciesRegistry.cs     // id → asset, built at boot / test setup
    Battle/
      BattleReplayView.cs    // consumes event log only
      UnitView.cs            // placeholder sprite + HP bar
    Editor/
      BalanceSweepWindow.cs  DataValidationTests.cs
  GameData/
    Monsters/ (12 .assets)   Skills/ (10 pool .assets)
  StreamingAssets/
    balance.json
```

**Responsibilities:** `Core` computes and never renders; `Battle` renders and
never computes; `Data` declares and never behaves; `Editor` verifies and
never ships. Any PR that blurs these lines is wrong by definition.

# Build Order

Each step is testable before the next begins; estimates are solo-dev evenings.

1. Project + folders + `balance.json` → `BalanceConfig` loader (1)
2. Enums, `StatBlock`, `StatMath` + unit tests (2)
3. SO types + `SpeciesRegistry` + 3 test species + data-validation tests (2)
4. Simulator skeleton: timeline + basic attacks only, 1v1, event log (3)
5. Determinism hash test ×100 — **do not proceed until green** (1)
6. Targeting rules + variable team sizes (1v1/2v2/3v3/asymmetric) (2)
7. Skills: Damage + Heal, cooldowns, ultimate charge (2)
8. Buff/Debuff timed modifier (1)
9. Anti-stall + hard resolve + endReason (1)
10. Win-condition edge tests + termination sweep (1)
11. Balance Sweep window + CSV + threshold asserts (2)
12. Remaining 9 species + 10-skill pool + species→skill table (2)
13. `LevelMath` + `TrainingMath` + debug triggers + personas in sweep (2)
14. Preparation-signal and mirror-fairness runs; first tuning pass on
    `balance.json` until success criteria 2–4 pass (2–3)
15. `BattleReplayView` + debug scene, placeholder art (3)
16. Android device build of the debug scene; run the full success-criteria
    checklist (2)

**Total ≈ 30 evenings ≈ 4 weeks part-time** — on target for the locked
30-day first-playable, with the riskiest system (the simulator) proven by
evening 10.
