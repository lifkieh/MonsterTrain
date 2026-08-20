using System.Collections.Generic;
using MTA.Core;
using MTA.Meta;
using NUnit.Framework;

namespace MTA.Tests
{
    // Edit-mode tests for the first-playable Meta layer (flow + match runner).
    // Uses an in-memory registry so it needs no generated assets.
    public class FlowTests
    {
        static SkillData Strike() => new SkillData { skillId = "strike",
            slot = SkillSlot.Basic, scalingStat = Stat.ATK, powerMultiplier = 1f };
        static SkillData Power() => new SkillData { skillId = "power_strike",
            slot = SkillSlot.Active, scalingStat = Stat.ATK, powerMultiplier = 2.8f, cooldownSeconds = 8f };
        static SkillData Rend() => new SkillData { skillId = "savage_rend",
            slot = SkillSlot.Ultimate, scalingStat = Stat.ATK, powerMultiplier = 3.8f, chargeTime = 15f };

        static SpeciesData Mon(string id, int spd = 12, int atk = 20) => new SpeciesData
        {
            speciesId = id, displayName = id,
            baseStats = new StatBlock { hp = 100, atk = atk, def = 12, spd = spd, intel = 10, luck = 8 },
            growth = GrowthWeights.Uniform(),
            basicSkill = Strike(), activeSkill = Power(), ultimateSkill = Rend()
        };

        static SpeciesRegistry Reg() =>
            new SpeciesRegistry(new[] { Mon("a"), Mon("b", 16, 18), Mon("c", 14, 22) });

        [Test]
        public void GameFlow_WalksTheLoop()
        {
            var f = new GameFlow();
            Assert.AreEqual(GamePhase.MainMenu, f.Phase);
            f.GoTeamSelect(); Assert.AreEqual(GamePhase.TeamSelect, f.Phase);
            f.GoBattle();     Assert.AreEqual(GamePhase.Battle, f.Phase);
            f.GoResult();     Assert.AreEqual(GamePhase.Result, f.Phase);
            f.GoTeamSelect(); Assert.AreEqual(GamePhase.TeamSelect, f.Phase);   // play again
            f.GoMainMenu();   Assert.AreEqual(GamePhase.MainMenu, f.Phase);
        }

        [Test]
        public void Session_TeamSelectToggle()
        {
            var s = new GameSession();
            Assert.IsFalse(s.PlayerTeamReady);                        // 0 picked = not ready
            s.TogglePlayer("a"); Assert.IsTrue(s.PlayerTeamReady);    // 1 = 1v1 ready
            s.TogglePlayer("b"); Assert.IsTrue(s.PlayerTeamReady);    // 2 = 2v2 ready
            s.TogglePlayer("c"); Assert.IsTrue(s.PlayerTeamReady);    // 3 = 3v3 ready
            s.TogglePlayer("d"); Assert.AreEqual(3, s.playerTeam.Count); // cap holds at TeamSize
            s.TogglePlayer("a"); Assert.AreEqual(2, s.playerTeam.Count); // deselect
        }

        [Test]
        public void Match_RunsFromSelectedTeam_ProducesWinner()
        {
            var reg = Reg();
            var s = new GameSession { matchSeed = 7 };
            s.TogglePlayer("a"); s.TogglePlayer("b"); s.TogglePlayer("c");
            Assert.IsTrue(s.PlayerTeamReady);
            s.enemyTeam = MatchRunner.RandomEnemy(new List<string> { "a", "b", "c" }, 3, new System.Random(99));

            var r = MatchRunner.Run(s, reg, new BalanceConfig());
            Assert.IsNotNull(r);
            Assert.IsTrue(r.winnerTeam == 0 || r.winnerTeam == 1);
            Assert.Greater(r.events.Count, 2);
            Assert.AreEqual(r.winnerTeam == 0, MatchRunner.PlayerWon(s));
        }
    }
}
