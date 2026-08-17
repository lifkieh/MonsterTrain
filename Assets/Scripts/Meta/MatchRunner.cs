using System.Collections.Generic;
using MTA.Core;

namespace MTA.Meta
{
    // Turns the session's teams into TeamConfigs, runs the headless sim, and
    // stores the result. Player is always team A (index 0). Pure Core deps.
    public static class MatchRunner
    {
        public static TeamConfig Build(IList<string> ids, int level)
        {
            var t = new TeamConfig();
            for (int i = 0; i < ids.Count; i++)
                t.units.Add(new UnitConfig { speciesId = ids[i], level = level });
            return t;
        }

        // Seeded-random enemy team from the available species pool.
        public static List<string> RandomEnemy(IList<string> pool, int size, System.Random rng)
        {
            var e = new List<string>(size);
            for (int i = 0; i < size; i++) e.Add(pool[rng.Next(pool.Count)]);
            return e;
        }

        public static TeamConfig BuildLeveled(IList<string> ids, IDictionary<string, int> levels, int defaultLevel)
        {
            var t = new TeamConfig();
            for (int i = 0; i < ids.Count; i++)
            {
                int lvl = (levels != null && levels.TryGetValue(ids[i], out var l)) ? l : defaultLevel;
                t.units.Add(new UnitConfig { speciesId = ids[i], level = lvl });
            }
            return t;
        }

        public static BattleResult Run(GameSession s, SpeciesRegistry reg, BalanceConfig cfg)
        {
            var a = BuildLeveled(s.playerTeam, s.playerLevels, GameSession.Level);
            var b = Build(s.enemyTeam, GameSession.Level);
            s.lastResult = BattleSimulator.Run(a, b, s.matchSeed, cfg, reg);
            return s.lastResult;
        }

        public static bool PlayerWon(GameSession s) =>
            s.lastResult != null && s.lastResult.winnerTeam == 0;
    }
}
