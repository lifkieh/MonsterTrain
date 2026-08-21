# PHASE V7 — LEVEL + ELEMENT ON THE MONSTER

User opted into a gameplay change (level + element). **First, the honest finding from a full code
audit:** these systems **already exist, are wired, deterministic, and saved** — rebuilding them would
be redundant and would risk the deterministic sim + 79 tests. So this phase **surfaces** them on the
monster in battle and makes element **matter**, rather than re-implementing them.

## What already existed (verified, with refs)
- **Level → stats:** `StatMath.EffectiveStats` (StatMath.cs:64) — `base + levelGain*(level-1) + allocated + trained`.
- **Element triangle in damage:** Fire>Nature>Water>Fire, `StatMath.ElementMultiplier` (StatMath.cs:39),
  applied in `SkillResolver.cs:57`.
- **Full progression loop:** battle → XP → level-up → unlock → evolve → **save**
  (`Progression.ApplyBattle` SaveData.cs:152 → `SaveSystem.Save` GameBootstrap.cs:324), persisted
  per-monster (`MonsterSave{level,xp}`), with a Max level 30 XP curve and coin training.
- **Save:** `SaveData` v2, JsonUtility, additive-migration.
- **Determinism:** seeded `BattleSimulator.Run`, FNV-1a64 log hash; 79 tests assert **re-run equality**
  (no golden constant), so changing a balance *value* keeps them green as long as the sim stays
  deterministic.

## What was actually missing → delivered this phase
1. **Level shown on the monster in battle.** New `UnitView.SetLevel` → a dark **"Lv12" pill** to the
   right of each fighter's HP bar; per-fighter level built from the session/profile
   (`GameBootstrap.BuildLevelByKey`, keyed by team/slot). (`showcase_v7/1_arena_1v1_09`,
   `3_arena_3v3_10`.)
2. **Element shown on the monster.** The plain colour dot is now an **element-shaped icon**
   (flame / droplet / leaf / bolt), tinted, left of the HP bar. Instantly reads each monster's type.
3. **Element matchup is felt.** `elementAdvantage` **0.04 → 0.15** (adv ×1.15 / disadv ×0.87) — a
   meaningful, compounding swing instead of a ~4% whisper. Gameplay/balance change (user opted in);
   **deterministic, 79/79 tests still pass** (BalanceParity asserts cyclic + symmetric, not magnitude).
4. **Matchup call-out.** A throttled **"SUPER EFFECTIVE!" / "RESISTED"** floating readout surfaces the
   triangle the sim already applies (`BattleReplayView.ElementAdv` matches `StatMath`). Correct on
   `showcase_v7/1_arena_1v1_09` — Jelly (Water) → Fire Lizard (Fire) = SUPER EFFECTIVE.

## Guarantees
- Sim / determinism: unchanged code paths; only a balance **constant** changed, sim still deterministic
  (79/79 re-run-equality tests pass).
- Save: untouched schema; the leveling/save loop is the existing one.
- Showcase remains READ-ONLY (V6 guard).
- Windows standalone builds; APK to follow.

## Note on scope
The "full leveling system" already shipped. If you want it to go **further** as gameplay — e.g. bigger
per-level stat curve, a visible XP bar in battle, or an element chart screen — those are additional
balance/UX changes I can do now that gameplay is opted in; each will be called out for its balance/save
impact. This phase deliberately made level + element **visible and felt on the monster** without
disturbing the working, tested, deterministic core.
