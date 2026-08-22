using System.Collections.Generic;
using MTA.Core;
using NUnit.Framework;

namespace MTA.Tests
{
    // TYM 2.0 Phase 8 — Active+Support determinism + integration. Support monsters are virtual (need
    // not be in the registry); only the Active species does.
    public class ActiveSupportTests
    {
        static SkillData Strike() => new SkillData { skillId = "strike", slot = SkillSlot.Basic, scalingStat = Stat.ATK, powerMultiplier = 1f };
        static SkillData Power() => new SkillData { skillId = "power", slot = SkillSlot.Active, scalingStat = Stat.ATK, powerMultiplier = 2.5f, cooldownSeconds = 8f };
        static SkillData Ult() => new SkillData { skillId = "ult", slot = SkillSlot.Ultimate, scalingStat = Stat.ATK, powerMultiplier = 3.5f, chargeTime = 15f };
        static SpeciesData Mon(string id) => new SpeciesData
        {
            speciesId = id, displayName = id, element = "",
            baseStats = new StatBlock { hp = 130, atk = 22, def = 12, spd = 12, intel = 10, luck = 8 },
            growth = GrowthWeights.Uniform(), basicSkill = Strike(), activeSkill = Power(), ultimateSkill = Ult()
        };
        static SpeciesRegistry Reg() => new SpeciesRegistry(new[] { Mon("hero"), Mon("enemy") });

        static TeamConfig ActiveTeam(string id, params string[] supports)
        {
            var t = new TeamConfig();
            t.units.Add(new UnitConfig { speciesId = id, level = 5 });
            t.supports = new List<string>(supports);
            return t;
        }

        [Test]
        public void SupportBattle_IsDeterministic()
        {
            var reg = Reg(); var c = new BalanceConfig();
            ulong h1 = BattleSimulator.Run(ActiveTeam("hero", "turtle", "golem"), ActiveTeam("enemy"), 4242, c, reg).logHash;
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(h1, BattleSimulator.Run(ActiveTeam("hero", "turtle", "golem"), ActiveTeam("enemy"), 4242, c, reg).logHash);
        }

        [Test]
        public void Support_Activates_And_EmitsEvents()
        {
            var reg = Reg(); var c = new BalanceConfig();
            // healer (regen) + summoner (strike) → should emit Support events during the fight
            var r = BattleSimulator.Run(ActiveTeam("hero", "mushroom_beast", "mantis"), ActiveTeam("enemy"), 77, c, reg);
            Assert.Greater(r.events.FindAll(e => e.kind == "Support").Count, 0, "support events emitted");
        }

        [Test]
        public void Guardian_ImprovesSurvival()
        {
            var reg = Reg(); var c = new BalanceConfig();
            int wins = 0;
            for (int seed = 1; seed <= 40; seed++)
            {
                var g = BattleSimulator.Run(ActiveTeam("hero", "turtle", "golem"), ActiveTeam("enemy"), seed, c, reg);   // shell + bulwark
                if (g.winnerTeam == 0) wins++;
            }
            // a strong guardian pair on an otherwise mirror matchup should win clearly more than half
            Assert.Greater(wins, 24, "guardian supports should tilt a mirror in the active's favor: " + wins + "/40");
        }

        [Test]
        public void NormalBattle_HasNoSupportEvents()
        {
            var reg = Reg(); var c = new BalanceConfig();
            var r = BattleSimulator.Run(ActiveTeam("hero"), ActiveTeam("enemy"), 5, c, reg);
            Assert.IsFalse(r.events.Exists(e => e.kind == "Support"));
        }

        [Test]
        public void SupportSlots_Validate_NoDuplicate()
        {
            var t = ActiveTeam("hero", "turtle", "turtle");   // duplicate support
            Assert.Throws<System.ArgumentException>(() => t.Validate());
        }
    }
}
