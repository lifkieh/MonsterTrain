using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MTA.Core
{
    // Non-UI sweep runner: callable from edit-mode tests or a one-line MenuItem.
    // 1,000 battles must run in seconds; nothing here allocates per action.
    public static class BalanceSweep
    {
        public class SweepConfig
        {
            public int battles = 1000;
            public int level = 1;
            public int teamSize = 3;             // 1..3
            public int baseSeed = 12345;
            public bool mirror = false;          // same comp both sides
            public int trainingSessionsPerType;  // persona: 0 = untrained
            public int levelBonusTeamA;          // persona: prep-signal test
        }

        public class SweepSummary
        {
            public int battles, hardResolves, subFifteen;
            public double p10, p50, p90;
            public double teamAWinRate;
            public string csv;

            public override string ToString() => string.Format(CultureInfo.InvariantCulture,
                "battles={0} P10={1:F1}s P50={2:F1}s P90={3:F1}s hardResolve={4:P1} " +
                "sub15s={5} teamAWin={6:P1}",
                battles, p10, p50, p90, hardResolves / (double)battles,
                subFifteen, teamAWinRate);
        }

        public static SweepSummary Run(SweepConfig sc, BalanceConfig cfg, SpeciesRegistry registry)
        {
            var speciesIds = new List<string>();
            foreach (var s in registry.All) speciesIds.Add(s.speciesId);
            speciesIds.Sort(StringComparer.Ordinal);          // registry order-independent

            var durations = new List<double>(sc.battles);
            var csv = new StringBuilder("seed,duration,winner,endReason\n");
            int winsA = 0, hard = 0, sub15 = 0;

            for (int i = 0; i < sc.battles; i++)
            {
                int seed = sc.baseSeed + i;
                var compRng = new Random(seed * 31 + 7);      // comp picking is seeded too
                var teamA = RandomTeam(speciesIds, sc, compRng);
                var teamB = sc.mirror ? Clone(teamA) : RandomTeam(speciesIds, sc, compRng);

                ApplyPersona(teamA, sc.trainingSessionsPerType, sc.levelBonusTeamA, cfg);
                if (!sc.mirror) ApplyPersona(teamB, sc.trainingSessionsPerType, 0, cfg);

                var r = BattleSimulator.Run(teamA, teamB, seed, cfg, registry);
                durations.Add(r.duration);
                if (r.winnerTeam == 0) winsA++;
                if (r.endReason == EndReason.HardResolve) hard++;
                if (r.duration < 15.0) sub15++;
                csv.Append(seed).Append(',')
                   .Append(r.duration.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                   .Append(r.winnerTeam).Append(',').Append(r.endReason).Append('\n');
            }

            durations.Sort();
            return new SweepSummary
            {
                battles = sc.battles, hardResolves = hard, subFifteen = sub15,
                p10 = Percentile(durations, 0.10),
                p50 = Percentile(durations, 0.50),
                p90 = Percentile(durations, 0.90),
                teamAWinRate = winsA / (double)sc.battles,
                csv = csv.ToString()
            };
        }

        static TeamConfig RandomTeam(List<string> ids, SweepConfig sc, Random rng)
        {
            var t = new TeamConfig();
            for (int i = 0; i < sc.teamSize; i++)             // duplicates allowed: two Wolves are legal
                t.units.Add(new UnitConfig { speciesId = ids[rng.Next(ids.Count)], level = sc.level });
            return t;
        }

        static TeamConfig Clone(TeamConfig src)
        {
            var t = new TeamConfig();
            foreach (var u in src.units)
                t.units.Add(new UnitConfig { speciesId = u.speciesId, level = u.level,
                    growthOverride = u.growthOverride, allocated = u.allocated, trained = u.trained });
            return t;
        }

        // Persona training is grade-neutral (B multiplier) so it composes before
        // grades are rolled; the per-instance grade fantasy is validated by
        // TrainingMath unit tests, not by the sweep.
        static void ApplyPersona(TeamConfig team, int sessionsPerType, int levelBonus, BalanceConfig cfg)
        {
            foreach (var u in team.units)
            {
                u.level += levelBonus;
                for (int t = 0; t < 4; t++)
                {
                    var type = (TrainingType)t;
                    int gain = StatMath.RoundStat(
                        cfg.trainingBaseYields[t] * cfg.TierMultiplier(GrowthTier.B)) * sessionsPerType;
                    u.trained.Add(TrainingMath.StatFor(type), gain);
                }
            }
        }

        static double Percentile(List<double> sorted, double p)
        {
            if (sorted.Count == 0) return 0;
            double idx = p * (sorted.Count - 1);
            int lo = (int)Math.Floor(idx), hi = (int)Math.Ceiling(idx);
            return sorted[lo] + (sorted[hi] - sorted[lo]) * (idx - lo);
        }
    }
}
