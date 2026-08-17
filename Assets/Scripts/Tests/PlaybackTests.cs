using System.Collections.Generic;
using MTA.Core;
using MTA.Meta;
using NUnit.Framework;

namespace MTA.Tests
{
    // Verifies BattlePlayback reconstructs a battle purely from the event log
    // (Spawn/Action/Died/End) — the sim->view contract, headlessly checked.
    public class PlaybackTests
    {
        static SkillData Strike() => new SkillData { skillId = "strike",
            slot = SkillSlot.Basic, scalingStat = Stat.ATK, powerMultiplier = 1f };
        static SkillData Power() => new SkillData { skillId = "power_strike",
            slot = SkillSlot.Active, scalingStat = Stat.ATK, powerMultiplier = 2.8f, cooldownSeconds = 8f };
        static SkillData Rend() => new SkillData { skillId = "savage_rend",
            slot = SkillSlot.Ultimate, scalingStat = Stat.ATK, powerMultiplier = 3.8f, chargeTime = 15f };

        static SpeciesData Mon(string id, int atk) => new SpeciesData
        {
            speciesId = id, displayName = id,
            baseStats = new StatBlock { hp = 100, atk = atk, def = 12, spd = 12, intel = 10, luck = 8 },
            growth = GrowthWeights.Uniform(),
            basicSkill = Strike(), activeSkill = Power(), ultimateSkill = Rend()
        };

        static BattleResult RunMatch(int seed)
        {
            var reg = new SpeciesRegistry(new[] { Mon("hero", 26), Mon("foe", 14) });
            var a = new TeamConfig(); var b = new TeamConfig();
            for (int i = 0; i < 3; i++) { a.units.Add(new UnitConfig { speciesId = "hero", level = 5 });
                                          b.units.Add(new UnitConfig { speciesId = "foe", level = 5 }); }
            return BattleSimulator.Run(a, b, seed, new BalanceConfig(), reg);
        }

        [Test]
        public void Playback_SpawnsAtFullHp()
        {
            var r = RunMatch(11);
            var pb = new BattlePlayback();
            pb.Init(r);
            pb.ProcessUpTo(0);                       // spawns only
            Assert.AreEqual(6, pb.Units.Count);      // 3v3
            foreach (var u in pb.Units)
            {
                Assert.AreEqual(u.maxHp, u.currentHp);
                Assert.IsTrue(u.Alive);
            }
        }

        [Test]
        public void Playback_ReconstructsFromLog()
        {
            var r = RunMatch(11);
            int diedInLog = 0;
            foreach (var e in r.events) if (e.kind == "Died") diedInLog++;

            var pb = new BattlePlayback();
            pb.Init(r);
            pb.ProcessAll();

            Assert.IsTrue(pb.Finished);
            Assert.AreEqual(r.winnerTeam, pb.WinnerTeam);
            Assert.AreEqual(diedInLog, pb.DeathsProcessed);
            Assert.Greater(pb.AliveCount(r.winnerTeam), 0);       // winner has survivors
            if (r.endReason == EndReason.Elimination)
                Assert.AreEqual(0, pb.AliveCount(1 - r.winnerTeam)); // loser wiped
        }
    }
}
