using System;
using System.Collections.Generic;

namespace MTA.Core
{
    // Switches on EffectKind — four cases, zero per-skill code. New skills are
    // data; new effect kinds are one added case here. (conventions)
    public static class SkillResolver
    {
        public static bool HasValidTarget(CombatUnit actor, SkillData skill, BattleState s)
        {
            switch (skill.effect)
            {
                case EffectKind.Damage:
                case EffectKind.Debuff:
                    return TargetSelector.FrontMost(s.Enemies(actor.team)) != null;
                case EffectKind.Heal:
                    return TargetSelector.MostInjuredAlly(s.Team(actor.team)) != null;
                default:                          // Buff: self/allies always valid while alive
                    return actor.Alive;
            }
        }

        // Emits events; returns units killed this action. Crit RNG is consumed
        // ONLY for Damage effects, in resolution order (RNG contract).
        public static List<CombatUnit> Resolve(CombatUnit actor, int skillIndex,
            BattleState s, BalanceConfig cfg, Random rng, List<BattleEvent> log, bool tagMode = false)
        {
            var skill = actor.skills[skillIndex];
            var killed = new List<CombatUnit>();

            switch (skill.effect)
            {
                case EffectKind.Damage:
                {
                    // Tag mode: the ONLY valid enemy is the front-living one (reserves are off
                    // the field), so skill-target "lowest HP" must not reach across to a benched
                    // reserve that happens to have a smaller max-HP roll.
                    var target = (tagMode || skillIndex == 0)
                        ? TargetSelector.FrontMost(s.Enemies(actor.team))
                        : TargetSelector.LowestHpEnemy(s.Enemies(actor.team));
                    if (target == null) break;

                    float raw = actor.EffectiveStat(skill.scalingStat) * skill.powerMultiplier;

                    // Fixed 3-roll order per Damage (RNG contract): crit, dodge, variance.
                    int atkLuck = actor.EffectiveStat(Stat.LUCK);
                    int defLuck = target.EffectiveStat(Stat.LUCK);
                    bool crit = rng.NextDouble() < StatMath.CritChance(atkLuck, cfg) + actor.bonusCrit;   // + Buffer support crit (0 for normal battles)
                    if (StatMath.ElementForcesCrit(actor.element, target.element)) crit = true;   // element 2.0: advantage = guaranteed crit (roll already consumed → determinism intact)
                    bool dodged = rng.NextDouble() < StatMath.DodgeChance(atkLuck, defLuck, cfg);
                    float varFactor = 1f + cfg.damageVariance * (float)(2.0 * rng.NextDouble() - 1.0);

                    if (crit) raw *= cfg.critMultiplier;
                    float mitigated = raw
                        * StatMath.Mitigation(target.EffectiveStat(Stat.DEF), cfg)
                        * StatMath.StallMultiplier(s.clock, cfg)
                        * StatMath.ElementMultiplier(actor.element, target.element, cfg)
                        * varFactor
                        * cfg.damageScale
                        * (1f - target.dmgReductionPct);   // Guardian support (0 for normal battles)
                    int final = dodged ? 0 : Math.Max(cfg.minDamage, StatMath.RoundStat(mitigated));

                    // Guardian support: negate the first incoming hit, then absorb with a shield pool
                    // (both default off ⇒ no effect / determinism intact for normal battles).
                    if (final > 0 && target.dodgeFirst && !target.dodgeFirstUsed) { target.dodgeFirstUsed = true; final = 0; dodged = true; }
                    if (final > 0 && target.shieldHp > 0) { int ab = Math.Min(target.shieldHp, final); target.shieldHp -= ab; final -= ab; }

                    target.currentHp = Math.Max(0, target.currentHp - final);
                    log.Add(new BattleEvent { t = s.clock, kind = "Action",
                        actorTeam = actor.team, actorSlot = actor.slot, skillId = skill.skillId,
                        targetTeam = target.team, targetSlot = target.slot,
                        raw = StatMath.RoundStat(raw), final = final, crit = crit,
                        extra = dodged ? "dodge" : "" });
                    if (!target.Alive) killed.Add(target);
                    break;
                }
                case EffectKind.Heal:
                {
                    var target = TargetSelector.MostInjuredAlly(s.Team(actor.team));
                    if (target == null) break;
                    int amount = Math.Max(1, StatMath.RoundStat(
                        actor.EffectiveStat(Stat.INT) * skill.powerMultiplier));
                    int applied = Math.Min(amount, target.maxHp - target.currentHp);
                    target.currentHp += applied;
                    log.Add(new BattleEvent { t = s.clock, kind = "Action",
                        actorTeam = actor.team, actorSlot = actor.slot, skillId = skill.skillId,
                        targetTeam = target.team, targetSlot = target.slot,
                        raw = amount, final = applied });
                    break;
                }
                case EffectKind.Buff:
                case EffectKind.Debuff:
                {
                    var targets = SelectModifierTargets(actor, skill, s, tagMode);
                    float sign = skill.effect == EffectKind.Buff ? 1f : -1f;
                    for (int i = 0; i < targets.Count; i++)
                    {
                        targets[i].modifiers.Add(new StatModifier {
                            stat = skill.affectedStat,
                            percent = sign * Math.Abs(skill.magnitudePercent),
                            expiresAt = s.clock + skill.durationSeconds });
                        log.Add(new BattleEvent { t = s.clock, kind = "Modifier",
                            actorTeam = actor.team, actorSlot = actor.slot, skillId = skill.skillId,
                            targetTeam = targets[i].team, targetSlot = targets[i].slot,
                            extra = skill.affectedStat + ":" +
                                    (sign * Math.Abs(skill.magnitudePercent))
                                        .ToString(System.Globalization.CultureInfo.InvariantCulture) });
                    }
                    break;
                }
            }
            return killed;
        }

        static List<CombatUnit> SelectModifierTargets(CombatUnit actor, SkillData skill, BattleState s, bool tagMode)
        {
            var list = new List<CombatUnit>();
            switch (skill.targetRule)
            {
                case TargetRule.Self: if (actor.Alive) list.Add(actor); break;
                case TargetRule.AllAllies:
                    var team = s.Team(actor.team);
                    for (int i = 0; i < team.Count; i++) if (team[i].Alive) list.Add(team[i]);
                    break;
                case TargetRule.Ally:
                    var ally = TargetSelector.MostInjuredAlly(s.Team(actor.team));
                    list.Add(ally ?? actor);      // nobody injured -> self
                    break;
                default:                          // Enemy (debuffs) — tag mode restricts to the front
                    var enemy = tagMode ? TargetSelector.FrontMost(s.Enemies(actor.team))
                                        : TargetSelector.LowestHpEnemy(s.Enemies(actor.team));
                    if (enemy != null) list.Add(enemy);
                    break;
            }
            return list;
        }
    }
}
