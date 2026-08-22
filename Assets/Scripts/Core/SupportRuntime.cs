using System;
using System.Collections.Generic;

namespace MTA.Core
{
    // TYM 2.0 Active+Support runtime. Supports are VIRTUAL (never built as combat units). Pipeline:
    //   ModifierPhase (OnBattleStart)  → apply the team's 2 supports to its Active + the enemy Active.
    //   SupportPhase   (per Active action, deterministic clock) → regen / emergency / cleanse / summon.
    // NO System.Random use anywhere → the crit/dodge/variance RNG stream is untouched, so normal
    // battles stay byte-identical and support battles are fully deterministic. Emits "Support"
    // BattleEvents so the view can render trigger callouts.
    public static class SupportRuntime
    {
        static CombatUnit Active(List<CombatUnit> team)
        {
            for (int i = 0; i < team.Count; i++) if (team[i].slot == 0) return team[i];
            return team.Count > 0 ? team[0] : null;
        }

        static void AddMod(CombatUnit u, Stat stat, float percent)
            => u.modifiers.Add(new StatModifier { stat = stat, percent = percent, expiresAt = double.MaxValue });

        // ModifierPhase — apply the two supports' battle-start modifiers.
        public static void ApplyBattleStart(BattleState state, int teamId, List<string> supportIds, List<BattleEvent> log)
        {
            if (supportIds == null || supportIds.Count == 0) return;
            var active = Active(state.Team(teamId));
            var enemyActive = Active(state.Enemies(teamId));
            if (active == null) return;

            var defs = new List<SupportDef>();
            foreach (var id in supportIds) if (SupportAbility.TryGet(id, out var d)) defs.Add(d);
            SupportDef a = defs.Count > 0 ? defs[0] : default, b = defs.Count > 1 ? defs[1] : default;
            var m = SupportCombat.Compute(defs.Count > 0, a, defs.Count > 1, b);

            // Buffer → self stat modifiers (whole battle)
            if (m.atkMult > 1f) AddMod(active, Stat.ATK, m.atkMult - 1f);
            if (m.speedMult > 1f) AddMod(active, Stat.SPD, m.speedMult - 1f);
            active.bonusCrit += m.critAdd;
            active.ultCostReduction += m.ultCostReduction;
            // Guardian
            active.dmgReductionPct += m.dmgReduction;
            active.shieldHp += (int)(active.maxHp * m.shieldFrac);   // separate pool, absorbed before HP
            if (m.dodgeFirstHit > 0f) active.dodgeFirst = true;
            // Healer
            active.regenPerSec += m.regenPerSec;
            active.emergencyHeal += m.emergencyHeal;
            if (m.cleanse > 0f) active.cleanse = true;
            // Summoner
            active.summonFrac += m.summonDps;
            // Debuffer → enemy stat modifiers
            if (enemyActive != null)
            {
                if (m.enemyDefReduction > 0f) AddMod(enemyActive, Stat.DEF, -m.enemyDefReduction);
                if (m.enemySpeedReduction > 0f) AddMod(enemyActive, Stat.SPD, -m.enemySpeedReduction);
            }

            foreach (var d in defs)
                log.Add(new BattleEvent { t = 0, kind = "Support", actorTeam = teamId, actorSlot = 0, skillId = d.id, extra = d.category + ":start" });
        }

        // SupportPhase — fired after the Active's own action. `interval` = the action interval just used.
        // Returns the enemy killed by a summon strike (or null) so the caller can log Died + check wipe.
        public static CombatUnit OnActiveAction(BattleState state, CombatUnit active, double interval, List<BattleEvent> log)
        {
            if (active == null || !active.Alive) return null;

            // Healer regen (per action, scaled by the interval)
            if (active.regenPerSec > 0f && active.currentHp < active.maxHp)
            {
                int heal = (int)(active.maxHp * active.regenPerSec * interval);
                if (heal > 0)
                {
                    int applied = Math.Min(heal, active.maxHp - active.currentHp);
                    active.currentHp += applied;
                    log.Add(SEvent(active.team, "regen", applied, state.clock));
                }
            }
            // Emergency heal once at low HP
            if (active.emergencyHeal > 0f && !active.emergencyUsed && active.currentHp < active.maxHp * 0.30f)
            {
                int heal = (int)(active.maxHp * active.emergencyHeal);
                active.currentHp = Math.Min(active.maxHp, active.currentHp + heal);
                active.emergencyUsed = true;
                log.Add(SEvent(active.team, "emergency", heal, state.clock));
            }
            // Periodic cleanse (every 3 s)
            if (active.cleanse && state.clock - active.lastCleanse >= 3.0)
            {
                int removed = active.modifiers.RemoveAll(mm => mm.percent < 0f);
                active.lastCleanse = state.clock;
                if (removed > 0) log.Add(SEvent(active.team, "cleanse", removed, state.clock));
            }
            // Summoner strike — extra damage to the enemy active (fraction of ATK over the interval)
            if (active.summonFrac > 0f)
            {
                var enemy = Active(state.Enemies(active.team));
                if (enemy != null && enemy.Alive)
                {
                    int dmg = Math.Max(1, (int)(active.EffectiveStat(Stat.ATK) * active.summonFrac * interval));
                    enemy.currentHp = Math.Max(0, enemy.currentHp - dmg);
                    log.Add(new BattleEvent { t = state.clock, kind = "Support", actorTeam = active.team, actorSlot = 0,
                        targetTeam = enemy.team, targetSlot = enemy.slot, final = dmg, skillId = "summon", extra = "summon" });
                    if (!enemy.Alive) return enemy;
                }
            }
            return null;
        }

        static BattleEvent SEvent(int team, string id, int amount, double t)
            => new BattleEvent { t = t, kind = "Support", actorTeam = team, actorSlot = 0, final = amount, skillId = id, extra = id };
    }
}
