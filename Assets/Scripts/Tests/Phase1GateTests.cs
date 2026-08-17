using System.Collections.Generic;
using MTA.Core;
using NUnit.Framework;

namespace MTA.Tests
{
    // Edit-mode gate tests for the Phase 1 success criteria that must never
    // regress. Pure Core — no scenes, no assets: species are built as data,
    // which doubles as the zero-code content proof.
    public class Phase1GateTests
    {
        static BalanceConfig Cfg() => new BalanceConfig();

        static SkillData Strike() => new SkillData { skillId = "strike",
            slot = SkillSlot.Basic, scalingStat = Stat.ATK, powerMultiplier = 1f };
        static SkillData Power() => new SkillData { skillId = "power_strike",
            slot = SkillSlot.Active, scalingStat = Stat.ATK, powerMultiplier = 2.8f,
            cooldownSeconds = 8f };
        static SkillData Rend() => new SkillData { skillId = "savage_rend",
            slot = SkillSlot.Ultimate, scalingStat = Stat.ATK, powerMultiplier = 3.8f,
            chargeTime = 15f };

        static SpeciesData Testmon(string id, int hp = 100, int atk = 20, int def = 12,
            int spd = 12, int intel = 10, int luck = 8) => new SpeciesData
        {
            speciesId = id, displayName = id,
            baseStats = new StatBlock { hp = hp, atk = atk, def = def, spd = spd, intel = intel, luck = luck },
            growth = GrowthWeights.Uniform(),
            basicSkill = Strike(), activeSkill = Power(), ultimateSkill = Rend()
        };

        static SpeciesRegistry Registry(params SpeciesData[] s) => new SpeciesRegistry(s);

        static TeamConfig Team(int size, string id, int level = 5)
        {
            var t = new TeamConfig();
            for (int i = 0; i < size; i++)
                t.units.Add(new UnitConfig { speciesId = id, level = level });
            return t;
        }

        [Test]
        public void StatMath_MatchesSpecFormulas()
        {
            var c = Cfg();
            Assert.AreEqual(0.5f, StatMath.Mitigation(50, c), 1e-4f);       // 1 - 50/100
            Assert.AreEqual(0.2f, StatMath.AttacksPerSecond(10, c), 1e-4f); // SPD 10 -> 0.2
            Assert.AreEqual(0.5f, StatMath.AttacksPerSecond(25, c), 1e-4f); // kink
            Assert.AreEqual(0.65f, StatMath.AttacksPerSecond(40, c), 1e-4f);// brake beyond 25
            Assert.AreEqual(0.30f, StatMath.CritChance(200, c), 1e-4f);     // capped
            Assert.AreEqual(1f, StatMath.StallMultiplier(74, c), 1e-4f);
            Assert.AreEqual(1.05f, StatMath.StallMultiplier(85.1, c), 1e-4f);
            // level gain: rate 1.0 x S(1.5) = 2 (AwayFromZero rounding)
            Assert.AreEqual(2, StatMath.LevelGain("x", Stat.ATK, GrowthTier.S, c));
        }

        [Test]
        public void Training_RoutesThroughGrowthGrade()
        {
            var c = Cfg();
            var rng = new System.Random(1);
            var sp = Testmon("t");
            var inst = MonsterInstance.Create(sp, 1, rng,
                new[] { GrowthTier.B, GrowthTier.S, GrowthTier.B, GrowthTier.B, GrowthTier.B, GrowthTier.B });
            int gain = TrainingMath.ApplySession(inst, TrainingType.Strength, c);
            Assert.AreEqual(3, gain);                       // 2 x 1.5 = 3 — grade discovery
            Assert.AreEqual(3, inst.trained.atk);
        }

        [Test]
        public void Determinism_SameSeedSameHash_100Runs()
        {
            var c = Cfg();
            var reg = Registry(Testmon("a"), Testmon("b", spd: 16, atk: 18));
            var ta = Team(3, "a"); var tb = Team(3, "b");
            ulong first = BattleSimulator.Run(ta, tb, 424242, c, reg).logHash;
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(first, BattleSimulator.Run(ta, tb, 424242, c, reg).logHash);
        }

        [Test]
        public void AllTeamSizes_Terminate_Within_HardResolve()
        {
            var c = Cfg();
            var reg = Registry(Testmon("a"), Testmon("b", def: 20, hp: 140));
            foreach (int size in new[] { 1, 2, 3 })
            {
                var r = BattleSimulator.Run(Team(size, "a"), Team(size, "b"), 7 + size, c, reg);
                Assert.LessOrEqual(r.duration, c.hardResolveTime);
                Assert.IsTrue(r.winnerTeam == 0 || r.winnerTeam == 1);
            }
            // asymmetric debug case must also run clean
            var asym = BattleSimulator.Run(Team(1, "a"), Team(3, "b"), 99, c, reg);
            Assert.LessOrEqual(asym.duration, c.hardResolveTime);
        }

        [Test]
        public void ThirteenthSpecies_FromPureData_ZeroCode()
        {
            var c = Cfg();
            var reg = Registry(Testmon("slime_t"), Testmon("newcomer_13", hp: 90, atk: 22, spd: 15));
            var r = BattleSimulator.Run(Team(2, "newcomer_13"), Team(2, "slime_t"), 5, c, reg);
            Assert.IsNotNull(r);
            Assert.Greater(r.events.Count, 2);
        }

        [Test]
        public void PreparationSignal_TrainedBeatsUntrained()
        {
            var c = Cfg();
            var reg = Registry(Testmon("a"));
            int winsPrepared = 0, n = 300;
            for (int i = 0; i < n; i++)
            {
                var prepared = Team(3, "a", level: 15);
                foreach (var u in prepared.units)
                    u.trained = new StatBlock { atk = 20, hp = 40, spd = 6, intel = 10 };
                var untrained = Team(3, "a", level: 5);
                if (BattleSimulator.Run(prepared, untrained, 1000 + i, c, reg).winnerTeam == 0)
                    winsPrepared++;
            }
            Assert.GreaterOrEqual(winsPrepared / (double)n, 0.75,
                "'I won because I prepared correctly' must be mechanically true.");
        }

        [Test]
        public void MirrorComps_NoSideBias()
        {
            var c = Cfg();
            var reg = Registry(Testmon("a"), Testmon("b", spd: 18, atk: 16));
            var sweep = BalanceSweep.Run(new BalanceSweep.SweepConfig
                { battles = 400, mirror = true, level = 5, teamSize = 3, baseSeed = 555 }, c, reg);
            Assert.That(sweep.teamAWinRate, Is.InRange(0.42, 0.58),
                "Mirror comps must sit near 50%: " + sweep);
        }
    }
}
