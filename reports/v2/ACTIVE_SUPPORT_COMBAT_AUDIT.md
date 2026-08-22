# PHASE 1 — ACTIVE + SUPPORT COMBAT AUDIT (BattleSimulator)

Audited before any change. `BattleSimulator.Run(TeamConfig a, TeamConfig b, int seed, cfg, registry,
bool tagMode)` — headless, deterministic, no UnityEngine.

## Attack loop (`Run`, lines 42–81)
`while(true)`: `ActionTimeline.NextActor(state)` picks the unit with the smallest `nextActionTime`
(tie-break by `initiativeKey`). Advance `state.clock` to that time; anti-stall tick; purge expired
modifiers; `ChooseSkill`; `SkillResolver.Resolve`; log `Died`; win check (`TeamWiped`); reschedule the
actor at `clock + ActionInterval(SPD)`. Ends on elimination or `hardResolveTime`.

## Target selection (`SkillResolver` + `TargetSelector`)
Damage basic/tag → `FrontMost(enemies)`; damage active → `LowestHpEnemy`; heal → `MostInjuredAlly`;
buff/debuff → self / all-allies / injured-ally / front-or-lowest enemy.

## Ultimate flow (`ChooseSkill`, lines 85–96)
`skills[2]` if `!ultimateUsed && clock ≥ ult.chargeTime && HasValidTarget` → sets `ultimateUsed`.
Else `skills[1]` if off cooldown. Else basic `skills[0]`.

## Damage flow (`SkillResolver.Resolve` Damage, 34–71)
`raw = EffectiveStat(scalingStat) * powerMultiplier`. **RNG contract — exactly 3 draws per Damage in
order: crit, dodge, variance** (all consumed regardless of outcome). Element 2.0 forces crit on
advantage AFTER the roll (roll consumed → order intact). `mitigated = raw × Mitigation(DEF) ×
Stall × Element × variance × damageScale`. Dodge → 0. Applies to `currentHp`, logs `Action`.

## Buff/debuff flow
`StatModifier{stat, percent, expiresAt}` added to a unit's `modifiers`; applied at read-time in
`CombatUnit.EffectiveStat` (mult = 1 + Σ percent). Logged as `Modifier`. **This is the clean insertion
point for support buffs/debuffs — deterministic, no RNG.**

## Replay flow (sim → view contract)
`BattleResult.events` = `List<BattleEvent>` (kinds: Start, Spawn, Action, Modifier, ModifierExpired,
Died, StallTick, End) + `logHash = FNV-1a64(canonical log)`. `ReplayBuilder` → `ReplayEvent`
(Spawn/Attack/Skill/Ultimate/Heal/Death/Victory) for `BattleReplayView`. Tests assert **re-run
equality** of `logHash` (no golden constant).

## Determinism levers I must NOT disturb
Single `System.Random(seed)`; draw order growth→jitter (per unit A then B) → per-Damage crit→dodge→
variance → hard-resolve flip; `BattleEvent.CanonicalLine` format; `StatMath` formulas.

## DESIGN — how support preserves everything
- **Opt-in via `TeamConfig.supports` (0–2 ids).** Empty ⇒ existing path, byte-identical ⇒ 79 tests +
  showcase + save unaffected (exactly the `tagMode` precedent).
- **Active+Support battle:** `units = [active]` (1 built unit/side); the 2 supports are **virtual** —
  never built as `CombatUnit`s, never on the timeline.
- **Pipeline SupportPhase → ModifierPhase → CombatResolution, all RNG-free:**
  - *ModifierPhase (OnBattleStart):* Buffer/Debuffer → `StatModifier` on the active/enemy; Guardian →
    new `dmgReductionPct` / shield HP / `dodgeFirst` fields on `CombatUnit` (default 0 ⇒ no effect for
    non-support battles). Deterministic math.
  - *SupportPhase (per active action tick — deterministic clock):* Healer regen + emergency, Summoner
    strike, cleanse. Emitted as new `Support` `BattleEvent`s so the view can render callouts.
  - *CombatResolution:* unchanged `SkillResolver`, plus `dmgReductionPct`/`dodgeFirst` reads (guarded).
- **No `System.Random` use anywhere in support** ⇒ the crit/dodge/variance stream is never touched;
  support battles are deterministic with their own hash; non-support battles are identical to today.
