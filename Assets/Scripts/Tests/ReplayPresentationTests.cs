using System.Collections.Generic;
using MTA.Core;
using MTA.Meta;
using NUnit.Framework;

namespace MTA.Tests
{
    // Phase J: the presentation layer must consume events in order and NEVER
    // change the simulator outcome or its log hash.
    public class ReplayPresentationTests
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

        static (SpeciesRegistry reg, Dictionary<string, SkillSlot> slots) Setup()
        {
            var list = new[] { Hero(), Healer() };
            var reg = new SpeciesRegistry(list);
            var slots = ReplayBuilder.SlotMap(list);
            return (reg, slots);
        }

        static BattleResult Run(SpeciesRegistry reg, int seed)
        {
            var a = new TeamConfig(); var b = new TeamConfig();
            for (int i = 0; i < 3; i++) { a.units.Add(new UnitConfig { speciesId = "hero", level = 5 });
                                          b.units.Add(new UnitConfig { speciesId = "healer", level = 5 }); }
            return BattleSimulator.Run(a, b, seed, new BalanceConfig(), reg);
        }

        [Test]
        public void Replay_EventsAreTimeOrdered()
        {
            var (reg, slots) = Setup();
            var events = ReplayBuilder.Build(Run(reg, 42), slots);
            Assert.Greater(events.Count, 3);
            double last = -1;
            foreach (var e in events) { Assert.GreaterOrEqual(e.t, last - 1e-9); last = e.t; }
        }

        [Test]
        public void Replay_ShowsAllActionKinds()
        {
            var (reg, slots) = Setup();
            var events = ReplayBuilder.Build(Run(reg, 42), slots);
            var kinds = new HashSet<ReplayEventKind>();
            foreach (var e in events) kinds.Add(e.kind);
            Assert.IsTrue(kinds.Contains(ReplayEventKind.Spawn), "spawns");
            Assert.IsTrue(kinds.Contains(ReplayEventKind.Attack), "attacks");
            Assert.IsTrue(kinds.Contains(ReplayEventKind.Death), "deaths");
            Assert.IsTrue(kinds.Contains(ReplayEventKind.Victory), "victory");
            Assert.IsTrue(kinds.Contains(ReplayEventKind.Skill) || kinds.Contains(ReplayEventKind.Ultimate),
                "at least one skill/ultimate cast");
        }

        [Test]
        public void Replay_DoesNotChangeOutcomeOrHash()
        {
            var (reg, _) = Setup();
            var r1 = Run(reg, 777);
            ulong h1 = r1.logHash;
            var slots = ReplayBuilder.SlotMap(new[] { Hero(), Healer() });
            _ = ReplayBuilder.Build(r1, slots);          // build replay (read-only)
            _ = BattleDrama.Compute(r1);                 // compute drama (read-only)
            // Re-run same seed: outcome + hash identical (presentation changed nothing).
            var r2 = Run(reg, 777);
            Assert.AreEqual(r1.winnerTeam, r2.winnerTeam);
            Assert.AreEqual(h1, r2.logHash);
            Assert.AreEqual(h1, r1.logHash);             // building didn't mutate r1
        }

        [Test]
        public void Drama_ClassifiesAndReportsLeaders()
        {
            var (reg, _) = Setup();
            var d = BattleDrama.Compute(Run(reg, 42));
            Assert.Contains(d.tier, new[] { WinTier.DominantWin, WinTier.AdvantageWin, WinTier.CloseWin, WinTier.ClutchWin });
            Assert.IsNotEmpty(d.bannerTitle);
            Assert.Greater(d.duration, 0);
            Assert.GreaterOrEqual(d.totalDamage[d.winnerTeam], 1);
            Assert.IsTrue(d.winnerTeam == 0 || d.winnerTeam == 1);
            Assert.IsNotEmpty(d.damageLeader);
        }
    }
}
