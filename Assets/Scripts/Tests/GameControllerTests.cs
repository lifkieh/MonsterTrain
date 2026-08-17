using System.Collections.Generic;
using MTA.Core;
using MTA.Meta;
using NUnit.Framework;

namespace MTA.Tests
{
    // End-to-end first-playable loop, driven headlessly through GameController:
    // Menu -> TeamSelect -> Battle -> Result -> PlayAgain -> Menu.
    public class GameControllerTests
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

        static GameController NewController()
        {
            var pool = new List<string> { "a", "b", "c", "d" };
            var reg = new SpeciesRegistry(new[] { Mon("a", 20), Mon("b", 22), Mon("c", 18), Mon("d", 24) });
            return new GameController(reg, new BalanceConfig(), pool, seedBase: 1000);
        }

        [Test]
        public void FullPlayableLoop()
        {
            var c = NewController();
            Assert.AreEqual(GamePhase.MainMenu, c.Flow.Phase);

            c.StartGame();
            Assert.AreEqual(GamePhase.TeamSelect, c.Flow.Phase);

            Assert.IsFalse(c.CanStartBattle);
            c.ToggleSpecies("a"); c.ToggleSpecies("b"); c.ToggleSpecies("c");
            Assert.IsTrue(c.CanStartBattle);

            var result = c.StartBattle();
            Assert.IsNotNull(result);
            Assert.AreEqual(GamePhase.Battle, c.Flow.Phase);
            Assert.IsTrue(result.winnerTeam == 0 || result.winnerTeam == 1);
            Assert.AreEqual(3, c.Session.enemyTeam.Count);

            c.OnBattleFinished();
            Assert.AreEqual(GamePhase.Result, c.Flow.Phase);
            Assert.AreEqual(result.winnerTeam == 0, c.PlayerWon);

            c.PlayAgain();
            Assert.AreEqual(GamePhase.TeamSelect, c.Flow.Phase);
            Assert.AreEqual(0, c.Session.playerTeam.Count);
            Assert.IsFalse(c.CanStartBattle);

            c.ToMenu();
            Assert.AreEqual(GamePhase.MainMenu, c.Flow.Phase);
        }

        [Test]
        public void StartBattle_BlockedUntilTeamReady()
        {
            var c = NewController();
            c.StartGame();
            c.ToggleSpecies("a");
            Assert.IsNull(c.StartBattle());               // only 1 picked
            Assert.AreNotEqual(GamePhase.Battle, c.Flow.Phase);
        }

        [Test]
        public void SecondMatchUsesDifferentSeed()
        {
            var c = NewController();
            c.StartGame();
            c.ToggleSpecies("a"); c.ToggleSpecies("b"); c.ToggleSpecies("c");
            var r1 = c.StartBattle(); int seed1 = c.Session.matchSeed;
            c.OnBattleFinished(); c.PlayAgain();
            c.ToggleSpecies("a"); c.ToggleSpecies("b"); c.ToggleSpecies("c");
            var r2 = c.StartBattle(); int seed2 = c.Session.matchSeed;
            Assert.AreNotEqual(seed1, seed2);
            Assert.IsNotNull(r1); Assert.IsNotNull(r2);
        }
    }
}
