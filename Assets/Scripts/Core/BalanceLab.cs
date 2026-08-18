using System;
using System.Collections.Generic;

namespace MTA.Core
{
    // Headless combat-economy analysis. Pure C#, no UnityEngine — measures the
    // VALUE of each stat (analytic EHP/DPS/TTK/Power + empirical duel win-rate),
    // species round-robin spread, and the power-difference → win-rate curve.
    // Used by the K-phase balance audit + validation; never touches the sim.
    public static class BalanceLab
    {
        // Neutral reference monster with an isolated, pure-basic ATK kit so a duel
        // reflects stats only (active mirrors basic; ult never charges).
        public static SpeciesData Ref(string id, StatBlock s, string element = "")
        {
            SkillData Dmg(string sid, SkillSlot slot, float charge) => new SkillData
            {
                skillId = sid, displayName = sid, slot = slot,
                scalingStat = Stat.ATK, powerMultiplier = 1f, cooldownSeconds = 0f,
                chargeTime = charge, effect = EffectKind.Damage, targetRule = TargetRule.Enemy
            };
            return new SpeciesData
            {
                speciesId = id, displayName = id, baseStats = s, growth = GrowthWeights.Uniform(), element = element,
                basicSkill = Dmg(id + "_b", SkillSlot.Basic, 15f),
                activeSkill = Dmg(id + "_a", SkillSlot.Active, 15f),
                ultimateSkill = Dmg(id + "_u", SkillSlot.Ultimate, 1e9f)
            };
        }

        // Duel with elements assigned — measures the elemental-triangle matchup swing.
        public static double DuelElem(BalanceConfig cfg, StatBlock a, StatBlock b, string ea, string eb, int seeds, int baseSeed = 300000)
        {
            var reg = new SpeciesRegistry(new[] { Ref("duel_a", a, ea), Ref("duel_b", b, eb) });
            int winsA = 0;
            for (int i = 0; i < seeds; i++)
            {
                var ta = new TeamConfig(); ta.units.Add(new UnitConfig { speciesId = "duel_a", level = 1 });
                var tb = new TeamConfig(); tb.units.Add(new UnitConfig { speciesId = "duel_b", level = 1 });
                if (BattleSimulator.Run(ta, tb, baseSeed + i, cfg, reg).winnerTeam == 0) winsA++;
            }
            return winsA / (double)seeds;
        }

        // ---- Analytic value model ----
        public static double CritChance(StatBlock s, BalanceConfig c) => Math.Min(s.luck * c.critPerLuck, c.critCap);
        public static double Dps(StatBlock s, BalanceConfig c) =>
            s.atk * (1.0 + CritChance(s, c) * (c.critMultiplier - 1.0)) * StatMath.AttacksPerSecond(s.spd, c);
        public static double Ehp(StatBlock s, BalanceConfig c) => s.hp * (double)(s.def + c.k) / c.k;
        public static double Power(StatBlock s, BalanceConfig c) => Dps(s, c) * Ehp(s, c);
        public static double Ttk(StatBlock attacker, StatBlock defender, BalanceConfig c) =>
            Ehp(defender, c) / Math.Max(1e-6, Dps(attacker, c));

        // ---- Empirical 1v1 duel: win-rate of statblock `a` vs `b` ----
        public static double Duel(BalanceConfig cfg, StatBlock a, StatBlock b, int seeds, int baseSeed = 100000)
        {
            var reg = new SpeciesRegistry(new[] { Ref("duel_a", a), Ref("duel_b", b) });
            int winsA = 0;
            for (int i = 0; i < seeds; i++)
            {
                var ta = new TeamConfig(); ta.units.Add(new UnitConfig { speciesId = "duel_a", level = 1 });
                var tb = new TeamConfig(); tb.units.Add(new UnitConfig { speciesId = "duel_b", level = 1 });
                if (BattleSimulator.Run(ta, tb, baseSeed + i, cfg, reg).winnerTeam == 0) winsA++;
            }
            return winsA / (double)seeds;
        }

        // Win-rate of (ref + delta on one stat) vs ref — the empirical marginal value.
        public static double StatWinrate(BalanceConfig cfg, StatBlock refb, Stat stat, int delta, int seeds)
        {
            var buff = refb; buff.Set(stat, refb.Get(stat) + delta);
            return Duel(cfg, buff, refb, seeds);
        }

        public static StatBlock Scale(StatBlock s, double f)
        {
            StatBlock r = new StatBlock();
            for (int i = 0; i < 6; i++) r.Set((Stat)i, Math.Max(1, StatMath.RoundStat(s.Get((Stat)i) * f)));
            return r;
        }

        public struct CurvePoint { public double f, diffPct, winrate; }

        // Win-rate after moving `amt` budget from one stat to another (vs baseline).
        // ~50% ⇒ the two stats carry equal value per point at this base.
        public static double BudgetSwap(BalanceConfig cfg, StatBlock baseb, Stat from, Stat to, int amt, int seeds)
        {
            var v = baseb;
            v.Set(from, Math.Max(1, baseb.Get(from) - amt));
            v.Set(to, baseb.Get(to) + amt);
            return Duel(cfg, v, baseb, seeds);
        }

        // Power-difference → win-rate: scale all of A's stats by f, measure vs baseline.
        public static List<CurvePoint> PowerCurve(BalanceConfig cfg, StatBlock baseb, int seeds)
        {
            double p0 = Power(baseb, cfg);
            var fs = new[] { 0.85, 0.90, 0.95, 0.97, 0.98, 0.99, 1.0, 1.01, 1.02, 1.03, 1.05, 1.10, 1.15 };
            var outp = new List<CurvePoint>();
            foreach (var f in fs)
            {
                var a = Scale(baseb, f);
                outp.Add(new CurvePoint { f = f, diffPct = Power(a, cfg) / p0 - 1.0, winrate = Duel(cfg, a, baseb, seeds) });
            }
            return outp;
        }

        public struct SpeciesWr { public string id; public double winrate; public int games; }

        // Real-species round-robin: each species vs every other, both sides, 1v1.
        public static List<SpeciesWr> RoundRobin(BalanceConfig cfg, SpeciesRegistry reg, int level, int seedsPerPair)
        {
            var ids = new List<string>();
            foreach (var s in reg.All) if (!s.evolutionOnly) ids.Add(s.speciesId);   // wild pool only
            ids.Sort(StringComparer.Ordinal);
            var wins = new Dictionary<string, int>(); var games = new Dictionary<string, int>();
            foreach (var id in ids) { wins[id] = 0; games[id] = 0; }

            for (int i = 0; i < ids.Count; i++)
                for (int j = 0; j < ids.Count; j++)
                {
                    if (i == j) continue;
                    for (int k = 0; k < seedsPerPair; k++)
                    {
                        var ta = new TeamConfig(); ta.units.Add(new UnitConfig { speciesId = ids[i], level = level });
                        var tb = new TeamConfig(); tb.units.Add(new UnitConfig { speciesId = ids[j], level = level });
                        int seed = 500000 + (i * ids.Count + j) * seedsPerPair + k;
                        var r = BattleSimulator.Run(ta, tb, seed, cfg, reg);
                        games[ids[i]]++;
                        if (r.winnerTeam == 0) wins[ids[i]]++;
                    }
                }

            var outp = new List<SpeciesWr>();
            foreach (var id in ids) outp.Add(new SpeciesWr { id = id, winrate = wins[id] / (double)Math.Max(1, games[id]), games = games[id] });
            outp.Sort((x, y) => y.winrate.CompareTo(x.winrate));
            return outp;
        }

        // Presence win-rate: random teams both sides; each species is credited a
        // win for every unit of it on the winning team. This is the real team-game
        // balance metric (utility/tank kits pay off here, unlike 1v1).
        public static List<SpeciesWr> PresenceWinrate(BalanceConfig cfg, SpeciesRegistry reg,
            int battles, int teamSize, int level, int baseSeed = 700000)
        {
            var ids = new List<string>();
            foreach (var s in reg.All) if (!s.evolutionOnly) ids.Add(s.speciesId);   // wild pool only
            ids.Sort(StringComparer.Ordinal);
            var wins = new Dictionary<string, int>(); var games = new Dictionary<string, int>();
            foreach (var id in ids) { wins[id] = 0; games[id] = 0; }

            for (int i = 0; i < battles; i++)
            {
                int seed = baseSeed + i;
                var pick = new Random(seed * 31 + 13);
                var ta = new TeamConfig(); var tb = new TeamConfig();
                for (int u = 0; u < teamSize; u++)
                {
                    ta.units.Add(new UnitConfig { speciesId = ids[pick.Next(ids.Count)], level = level });
                    tb.units.Add(new UnitConfig { speciesId = ids[pick.Next(ids.Count)], level = level });
                }
                var r = BattleSimulator.Run(ta, tb, seed, cfg, reg);
                foreach (var un in ta.units) { games[un.speciesId]++; if (r.winnerTeam == 0) wins[un.speciesId]++; }
                foreach (var un in tb.units) { games[un.speciesId]++; if (r.winnerTeam == 1) wins[un.speciesId]++; }
            }

            var outp = new List<SpeciesWr>();
            foreach (var id in ids) outp.Add(new SpeciesWr { id = id, winrate = wins[id] / (double)Math.Max(1, games[id]), games = games[id] });
            outp.Sort((x, y) => y.winrate.CompareTo(x.winrate));
            return outp;
        }

        public static StatBlock Neutral() => new StatBlock { hp = 100, atk = 30, def = 20, spd = 20, intel = 20, luck = 20 };
    }
}
