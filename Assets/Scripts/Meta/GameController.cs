using System.Collections.Generic;
using MTA.Core;

namespace MTA.Meta
{
    // The first-playable's brain: owns flow + session, rolls the enemy, runs the
    // match, and advances phases. Pure C# (no UnityEngine) so the whole loop is
    // edit-mode testable; the MonoBehaviour shell just calls these and renders.
    public class GameController
    {
        public readonly GameFlow Flow = new GameFlow();
        public readonly GameSession Session = new GameSession();

        readonly SpeciesRegistry _reg;
        readonly BalanceConfig _cfg;
        readonly List<string> _pool;
        readonly int _seedBase;
        int _matchCount;
        CareerStage _pending;   // set while a career stage is queued; null = casual match

        public GameController(SpeciesRegistry reg, BalanceConfig cfg, IList<string> speciesPool, int seedBase)
        {
            _reg = reg;
            _cfg = cfg;
            _pool = new List<string>(speciesPool);
            _seedBase = seedBase;
        }

        public IReadOnlyList<string> SpeciesPool => _pool;
        public bool CanStartBattle => Session.PlayerTeamReady;
        public bool PlayerWon => MatchRunner.PlayerWon(Session);

        public bool InCareer => _pending != null;

        public void StartGame() { _pending = null; Flow.GoTeamSelect(); }   // casual: Menu -> TeamSelect

        // Career: queue a stage, then pick a team for it.
        public void SelectCareerStage(CareerStage st)
        {
            _pending = st;
            Session.ClearPlayer();
            Session.lastResult = null;
            Flow.GoTeamSelect();
        }

        public void ToggleSpecies(string id) => Session.TogglePlayer(id);

        // Rolls a seeded enemy, runs the (deterministic) sim, enters Battle.
        // Returns the result for the view to replay; null if team not ready.
        public BattleResult StartBattle()
        {
            if (!Session.PlayerTeamReady) return null;
            Session.matchSeed = _seedBase + _matchCount++;
            if (_pending != null)
            {
                Session.enemyTeam = new List<string>(_pending.enemies);
                Session.enemyLevel = _pending.enemyLevel;
                Session.careerStageIndex = _pending.index;
            }
            else
            {
                Session.enemyTeam = MatchRunner.RandomEnemy(_pool, GameSession.TeamSize,
                    new System.Random(Session.matchSeed));
                Session.enemyLevel = GameSession.Level;
                Session.careerStageIndex = -1;
            }
            var result = MatchRunner.Run(Session, _reg, _cfg);
            Flow.GoBattle();
            return result;
        }

        public void OnBattleFinished() => Flow.GoResult();       // view -> Result

        public void PlayAgain()                                  // Result -> TeamSelect
        {
            Session.ClearPlayer();
            Session.lastResult = null;
            Flow.GoTeamSelect();
        }

        public void ToMenu()                                     // -> MainMenu
        {
            _pending = null;
            Session.ClearPlayer();
            Session.lastResult = null;
            Flow.GoMainMenu();
        }

        public void ToProgress() => Flow.GoProgress();
        public void ToCollection() => Flow.GoCollection();
        public void ToDetail() => Flow.GoDetail();
        public void ToCareer() { _pending = null; Flow.GoCareer(); }   // -> career map
        public void ToDaily() => Flow.GoDaily();
        public void BackToMenu() => Flow.GoMainMenu();
    }
}
