using System.Collections.Generic;
using System.Text;
using MTA.Core;
using MTA.Meta;
using NUnit.Framework;

namespace MTA.Tests
{
    // Phase O-2: the tawuran engagement planner is a READ-ONLY consumer of the finished
    // replay (like the cinematic director). These prove it is fully deterministic from the
    // logHash, and that filler beats can never move HP / are held clear of real events.
    public class EngagementPlannerTests
    {
        static SkillData Sk(string id, SkillSlot slot, Stat sc, float mult, EffectKind e,
            float cd = 0f, float charge = 15f, TargetRule rule = TargetRule.Enemy) => new SkillData
        {
            skillId = id, slot = slot, scalingStat = sc, powerMultiplier = mult,
            cooldownSeconds = cd, chargeTime = charge, effect = e, targetRule = rule
        };

        static SpeciesData Hero() => new SpeciesData
        {
            speciesId = "hero", displayName = "Hero",
            baseStats = new StatBlock { hp = 100, atk = 28, def = 12, spd = 14, intel = 10, luck = 20 },
            growth = GrowthWeights.Uniform(),
            basicSkill = Sk("strike", SkillSlot.Basic, Stat.ATK, 1f, EffectKind.Damage),
            activeSkill = Sk("power_strike", SkillSlot.Active, Stat.ATK, 2.8f, EffectKind.Damage, cd: 8f),
            ultimateSkill = Sk("savage_rend", SkillSlot.Ultimate, Stat.ATK, 3.8f, EffectKind.Damage, charge: 15f)
        };

        static SpeciesData Healer() => new SpeciesData
        {
            speciesId = "healer", displayName = "Healer",
            baseStats = new StatBlock { hp = 120, atk = 8, def = 14, spd = 10, intel = 20, luck = 6 },
            growth = GrowthWeights.Uniform(),
            basicSkill = Sk("zap", SkillSlot.Basic, Stat.INT, 1f, EffectKind.Damage),
            activeSkill = Sk("mend", SkillSlot.Active, Stat.INT, 2.5f, EffectKind.Heal, cd: 10f, rule: TargetRule.Ally),
            ultimateSkill = Sk("savage_rend", SkillSlot.Ultimate, Stat.ATK, 3.8f, EffectKind.Damage, charge: 15f)
        };

        static (BattleResult result, List<ReplayEvent> replay) RunReplay(int seed)
        {
            var reg = new SpeciesRegistry(new[] { Hero(), Healer() });
            var a = new TeamConfig(); var b = new TeamConfig();
            for (int i = 0; i < 3; i++)
            {
                a.units.Add(new UnitConfig { speciesId = "hero", level = 5 });
                b.units.Add(new UnitConfig { speciesId = "healer", level = 5 });
            }
            var result = BattleSimulator.Run(a, b, seed, new BalanceConfig(), reg);
            var slots = ReplayBuilder.SlotMap(new[] { Hero(), Healer() });
            return (result, ReplayBuilder.Build(result, slots));
        }

        static string Sig(EngagementPlan p)
        {
            var sb = new StringBuilder();
            sb.Append(p.seed).Append('|');
            var keys = new List<int>(p.segments.Keys); keys.Sort();
            foreach (var k in keys)
            {
                sb.Append('S').Append(k).Append(':');
                foreach (var s in p.segments[k])
                    sb.Append(s.t0.ToString("F4")).Append(',').Append(s.t1.ToString("F4")).Append(',').Append(s.oppKey).Append(';');
            }
            foreach (var f in p.fillers)
                sb.Append('F').Append(f.t.ToString("F4")).Append(',').Append(f.unitKey).Append(',').Append(f.oppKey).Append(',').Append((int)f.kind).Append(';');
            var cl = new List<int>(p.clashEventIdx); cl.Sort();
            foreach (var c in cl) sb.Append('C').Append(c).Append(';');
            return sb.ToString();
        }

        [Test]
        public void Plan_IsDeterministic_ForSameLog()
        {
            foreach (int seed in new[] { 7, 555, 2024 })
            {
                var (r1, rep1) = RunReplay(seed);
                var (r2, rep2) = RunReplay(seed);   // fresh run, same seed → same log → same plan
                Assert.AreEqual(Sig(EngagementPlanner.Plan(r1, rep1)), Sig(EngagementPlanner.Plan(r2, rep2)),
                    "engagement plan must be byte-identical for the same battle (seed " + seed + ")");
            }
        }

        [Test]
        public void FillerBeats_CarryNoDamage_AndAvoidRealEvents()
        {
            var (result, replay) = RunReplay(123);
            var plan = EngagementPlanner.Plan(result, replay);

            // team of each unit key, and every real (damaging/heal) event time per unit.
            var team = new Dictionary<int, int>();
            var realTimes = new Dictionary<int, List<double>>();
            foreach (var e in replay)
                if (e.kind == ReplayEventKind.Spawn) { int k = e.actorTeam * 100 + e.actorSlot; team[k] = e.actorTeam; realTimes[k] = new List<double>(); }
            foreach (var e in replay)
            {
                if (e.isBuff) continue;
                bool off = e.kind == ReplayEventKind.Attack || e.kind == ReplayEventKind.Skill || e.kind == ReplayEventKind.Ultimate;
                if (off && e.actorSlot >= 0 && e.targetSlot >= 0 && e.actorTeam != e.targetTeam)
                {
                    realTimes[e.actorTeam * 100 + e.actorSlot].Add(e.t);
                    realTimes[e.targetTeam * 100 + e.targetSlot].Add(e.t);
                }
                else if (e.kind == ReplayEventKind.Heal && e.actorSlot >= 0)
                    realTimes[e.actorTeam * 100 + e.actorSlot].Add(e.t);
            }

            Assert.IsNotEmpty(plan.fillers, "a full battle should schedule some filler beats");
            foreach (var f in plan.fillers)
            {
                // opponent is a live enemy (different team) — filler is directed at an enemy.
                Assert.IsTrue(team.ContainsKey(f.oppKey) && team[f.oppKey] != team[f.unitKey],
                    "filler opponent must be an enemy");
                // filler is held clear of every real event for that unit (elastic; never masks a real hit).
                foreach (var rt in realTimes[f.unitKey])
                    Assert.GreaterOrEqual(System.Math.Abs(f.t - rt), 0.09,
                        "filler beat too close to a real event at t=" + rt);
            }
            // FillerBeat has no amount/HP field by construction — it cannot mutate HP.
        }
    }
}
