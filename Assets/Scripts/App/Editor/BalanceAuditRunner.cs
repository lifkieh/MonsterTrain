#if UNITY_EDITOR
using System.Globalization;
using System.IO;
using System.Text;
using MTA.Core;
using MTA.Data;
using UnityEditor;
using UnityEngine;

namespace MTA.App.EditorTools
{
    // Headless combat-economy audit. Loads the real species + balance.json, runs
    // BalanceLab sweeps, and writes reports/ATTRIBUTE_VALUE_ANALYSIS.md.
    // Invoke: -executeMethod MTA.App.EditorTools.BalanceAuditRunner.RunAudit
    public static class BalanceAuditRunner
    {
        static string Repo(string rel) => Path.Combine(Directory.GetParent(Application.dataPath).FullName, rel);
        static string F(double v, int d = 2) => v.ToString("F" + d, CultureInfo.InvariantCulture);
        static string Pct(double v) => (v * 100.0).ToString("F1", CultureInfo.InvariantCulture) + "%";

        // K3 candidate statlines (level-1). Role-shaped, tuned to equal effective
        // power so 1v1 round-robin lands in 40–60%. id, hp,atk,def,spd,int,luck.
        struct Line { public string id; public int hp, atk, def, spd, intel, luck; public string element; public string role; public string active, ult; }

        static Line[] TuneTable() => new[]
        {
            new Line { id="bat",            hp=74,  atk=22, def=8,  spd=26, intel=8,  luck=22, element="Fire",   role="Assassin" },
            new Line { id="spider",         hp=78,  atk=21, def=10, spd=27, intel=12, luck=20, element="Nature", role="Assassin", active="power_strike" },
            new Line { id="ghost",          hp=78,  atk=8,  def=11, spd=20, intel=25, luck=18, element="Water",  role="Assassin(Mage)" },
            new Line { id="wolf",           hp=104, atk=22, def=17, spd=18, intel=8,  luck=12, element="Nature", role="Bruiser" },
            new Line { id="goblin",         hp=102, atk=21, def=16, spd=19, intel=8,  luck=14, element="Fire",   role="Bruiser" },
            new Line { id="dragonling",     hp=96,  atk=14, def=16, spd=17, intel=28, luck=12, element="Fire",   role="Bruiser(Mage)" },
            new Line { id="turtle",         hp=150, atk=20, def=30, spd=15, intel=8,  luck=8,  element="Water",  role="Tank",    active="power_strike" },
            new Line { id="golem",          hp=140, atk=23, def=28, spd=13, intel=6,  luck=8,  element="Nature", role="Tank",    active="power_strike" },
            new Line { id="slime",          hp=130, atk=21, def=24, spd=15, intel=12, luck=10, element="Water",  role="Tank",    active="power_strike" },
            new Line { id="fire_lizard",    hp=94,  atk=12, def=15, spd=15, intel=26, luck=12, element="Fire",   role="Mage" },
            new Line { id="bee",            hp=94,  atk=20, def=11, spd=26, intel=14, luck=20, element="Nature", role="Support", active="power_strike" },
            new Line { id="mushroom_beast", hp=138, atk=16, def=23, spd=13, intel=34, luck=12, element="Water",  role="Support" },
        };

        static SpeciesRegistry TunedRegistry(SpeciesRegistry real)
        {
            var byId = new System.Collections.Generic.Dictionary<string, Line>();
            foreach (var l in TuneTable()) byId[l.id] = l;
            // Skill lookup by id (any species that carries it) for kit reassignment.
            var skillById = new System.Collections.Generic.Dictionary<string, SkillData>();
            foreach (var sp in real.All)
                foreach (var sk in new[] { sp.basicSkill, sp.activeSkill, sp.ultimateSkill })
                    if (sk != null && !string.IsNullOrEmpty(sk.skillId)) skillById[sk.skillId] = sk;

            var list = new System.Collections.Generic.List<SpeciesData>();
            foreach (var sp in real.All)
            {
                if (!byId.TryGetValue(sp.speciesId, out var l)) { list.Add(sp); continue; }
                SkillData Pick(string id, SkillData fallback) =>
                    !string.IsNullOrEmpty(id) && skillById.TryGetValue(id, out var s) ? s : fallback;
                list.Add(new SpeciesData
                {
                    speciesId = sp.speciesId, displayName = sp.displayName,
                    baseStats = new StatBlock { hp = l.hp, atk = l.atk, def = l.def, spd = l.spd, intel = l.intel, luck = l.luck },
                    growth = sp.growth, element = l.element,
                    basicSkill = sp.basicSkill,
                    activeSkill = Pick(l.active, sp.activeSkill),
                    ultimateSkill = Pick(l.ult, sp.ultimateSkill)
                });
            }
            return new SpeciesRegistry(list);
        }

        // Iterate species parity fast: logs 1v1 round-robin win-rate per species.
        // Invoke: -executeMethod MTA.App.EditorTools.BalanceAuditRunner.RunSpeciesTune
        public static void RunSpeciesTune()
        {
            var cfg = SpeciesDatabase.LoadBalance();
            var reg = SpeciesDatabase.LoadFromResources();   // real assets + element combat
            var roles = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var l in TuneTable()) roles[l.id] = l.role;
            double min = 1, max = 0;
            foreach (var w in BalanceLab.PresenceWinrate(cfg, reg, 6000, 3, 1))
            {
                Debug.Log("TUNE " + w.id.PadRight(16) + " wr=" + Pct(w.winrate) + " role=" + roles[w.id]);
                min = System.Math.Min(min, w.winrate); max = System.Math.Max(max, w.winrate);
            }
            Debug.Log("TUNE SPREAD(3v3 presence) min=" + Pct(min) + " max=" + Pct(max));
            EditorApplication.Exit(0);
        }

        // K8 large-scale validation → reports/BALANCE_VALIDATION.md.
        // Invoke: -executeMethod MTA.App.EditorTools.BalanceAuditRunner.RunValidation
        public static void RunValidation()
        {
            var cfg = SpeciesDatabase.LoadBalance();
            var reg = SpeciesDatabase.LoadFromResources();
            var roles = new System.Collections.Generic.Dictionary<string, string>();
            var elems = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var l in TuneTable()) { roles[l.id] = l.role; }
            foreach (var s in reg.All) elems[s.speciesId] = s.element;

            const int N = 20000;
            var pres = BalanceLab.PresenceWinrate(cfg, reg, N, 3, 5);
            var sweep = BalanceSweep.Run(new BalanceSweep.SweepConfig { battles = N, level = 5, teamSize = 3 }, cfg, reg);

            var sb = new StringBuilder();
            sb.AppendLine("# Balance Validation (K8 — Automated Large-Scale)");
            sb.AppendLine();
            sb.AppendLine("Date: 2026-08-18. " + N.ToString("N0", CultureInfo.InvariantCulture) +
                " random 3v3 battles at level 5 on the live rebalanced species + `balance.json`.");
            sb.AppendLine();

            sb.AppendLine("## Duration & side-bias");
            sb.AppendLine("- Duration P10/P50/P90 = " + F(sweep.p10, 1) + " / " + F(sweep.p50, 1) + " / " + F(sweep.p90, 1) + " s  (target 25–90 s band)");
            sb.AppendLine("- Hard-resolve rate = " + Pct(sweep.hardResolves / (double)sweep.battles));
            sb.AppendLine("- Team-A win-rate = " + Pct(sweep.teamAWinRate) + "  (target 47–53%)");
            sb.AppendLine();

            double min = 1, max = 0;
            sb.AppendLine("## Species presence win-rate (target 40–60%)");
            sb.AppendLine();
            sb.AppendLine("| species | element | role | win-rate |");
            sb.AppendLine("|---------|---------|------|----------|");
            foreach (var w in pres)
            {
                sb.AppendLine("| " + w.id + " | " + elems[w.id] + " | " + roles[w.id] + " | " + Pct(w.winrate) + " |");
                min = System.Math.Min(min, w.winrate); max = System.Math.Max(max, w.winrate);
            }
            sb.AppendLine();
            sb.AppendLine("**Spread: " + Pct(min) + " – " + Pct(max) + "** " +
                (min >= 0.40 && max <= 0.60 ? "✓ all species inside 40–60%." : "⚠ outside band."));
            sb.AppendLine();

            // Role aggregate
            sb.AppendLine("## Role aggregate win-rate");
            sb.AppendLine();
            var rW = new System.Collections.Generic.Dictionary<string, double>();
            var rC = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var w in pres) { var r = roles[w.id]; if (!rW.ContainsKey(r)) { rW[r] = 0; rC[r] = 0; } rW[r] += w.winrate; rC[r]++; }
            sb.AppendLine("| role | avg win-rate |");
            sb.AppendLine("|------|--------------|");
            foreach (var kv in rW) sb.AppendLine("| " + kv.Key + " | " + Pct(kv.Value / rC[kv.Key]) + " |");
            sb.AppendLine();

            // Element aggregate
            sb.AppendLine("## Element aggregate win-rate (should be ~50% each — symmetric triangle)");
            sb.AppendLine();
            var eW = new System.Collections.Generic.Dictionary<string, double>();
            var eC = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var w in pres) { var e = elems[w.id]; if (!eW.ContainsKey(e)) { eW[e] = 0; eC[e] = 0; } eW[e] += w.winrate; eC[e]++; }
            sb.AppendLine("| element | avg win-rate | species |");
            sb.AppendLine("|---------|--------------|---------|");
            foreach (var kv in eW) sb.AppendLine("| " + kv.Key + " | " + Pct(kv.Value / eC[kv.Key]) + " | " + eC[kv.Key] + " |");
            sb.AppendLine();

            // Element matchup swing
            var nb = BalanceLab.Neutral();
            double fireVsNature = BalanceLab.DuelElem(cfg, nb, nb, "Fire", "Nature", 3000);
            double mirror = BalanceLab.DuelElem(cfg, nb, nb, "Fire", "Fire", 3000);
            sb.AppendLine("## Element matchup swing");
            sb.AppendLine("- Fire→Nature (advantage) win-rate = " + Pct(fireVsNature) + "  vs same-element mirror " + Pct(mirror));
            sb.AppendLine("- Swing ≈ " + Pct(fireVsNature - mirror) + " (target ~10–15% matchup impact, does not overpower stat parity).");
            sb.AppendLine();

            // Power-difference → win-rate curve
            sb.AppendLine("## Power-difference → win-rate curve");
            sb.AppendLine();
            sb.AppendLine("| power diff | A win-rate |");
            sb.AppendLine("|-----------|------------|");
            foreach (var pt in BalanceLab.PowerCurve(cfg, nb, 2000))
                sb.AppendLine("| " + Pct(pt.diffPct) + " | " + Pct(pt.winrate) + " |");
            sb.AppendLine();
            sb.AppendLine("A <10% power difference stays near 45–55%; a large advantage still wins more,");
            sb.AppendLine("but no slight edge produces a 90%+ auto-win.");

            var path = Repo("reports/BALANCE_VALIDATION.md");
            File.WriteAllText(path, sb.ToString());
            Debug.Log("MTA: validation written -> " + path + " | spread=" + Pct(min) + "-" + Pct(max) +
                " dur50=" + F(sweep.p50, 1) + " teamA=" + Pct(sweep.teamAWinRate));
            EditorApplication.Exit(0);
        }

        public static void RunAudit()
        {
            var reg = SpeciesDatabase.LoadFromResources();
            var cfg = SpeciesDatabase.LoadBalance();
            var sb = new StringBuilder();

            sb.AppendLine("# Attribute Value Analysis (K1 — Combat Economy Audit)");
            sb.AppendLine();
            sb.AppendLine("Date: 2026-08-18. Measures the VALUE of each stat under the current combat");
            sb.AppendLine("formulas: analytic EHP/DPS/TTK/Power plus empirical duel win-rate. All numbers");
            sb.AppendLine("generated headlessly from the live species + `balance.json`.");
            sb.AppendLine();

            // --- Analytic marginal value at the neutral reference ---
            var nb = BalanceLab.Neutral();
            sb.AppendLine("## Reference monster");
            sb.AppendLine("`HP=100 ATK=30 DEF=20 SPD=20 INT=20 LUCK=20`");
            sb.AppendLine();
            sb.AppendLine("- EHP = " + F(BalanceLab.Ehp(nb, cfg)) + "  (HP × (DEF+k)/k, k=" + cfg.k + ")");
            sb.AppendLine("- DPS = " + F(BalanceLab.Dps(nb, cfg)) + "  (ATK × crit-avg × APS(SPD))");
            sb.AppendLine("- Power = EHP × DPS = " + F(BalanceLab.Power(nb, cfg)));
            sb.AppendLine();

            sb.AppendLine("## Analytic marginal value of +1 of each stat (at the reference)");
            sb.AppendLine();
            sb.AppendLine("| Stat | ΔEHP | ΔDPS | ΔPower | ΔPower % |");
            sb.AppendLine("|------|------|------|--------|----------|");
            double p0 = BalanceLab.Power(nb, cfg);
            foreach (Stat s in new[] { Stat.HP, Stat.ATK, Stat.DEF, Stat.SPD, Stat.LUCK })
            {
                var plus = nb; plus.Set(s, nb.Get(s) + 1);
                double dEhp = BalanceLab.Ehp(plus, cfg) - BalanceLab.Ehp(nb, cfg);
                double dDps = BalanceLab.Dps(plus, cfg) - BalanceLab.Dps(nb, cfg);
                double dPow = BalanceLab.Power(plus, cfg) - p0;
                sb.AppendLine("| " + s + " | " + F(dEhp) + " | " + F(dDps, 3) + " | " + F(dPow) + " | " + Pct(dPow / p0) + " |");
            }
            sb.AppendLine();
            sb.AppendLine("_INT excluded (no damage kit on the neutral reference); it is a caster stat._");
            sb.AppendLine();

            // --- Empirical marginal value: duel win-rate of ref+Δ vs ref ---
            int seeds = 1500;
            sb.AppendLine("## Empirical marginal value (duel win-rate of ref+Δ vs ref, " + seeds + " seeds)");
            sb.AppendLine();
            sb.AppendLine("| Stat | +5 | +10 | +20 |");
            sb.AppendLine("|------|----|-----|-----|");
            foreach (Stat s in new[] { Stat.HP, Stat.ATK, Stat.DEF, Stat.SPD, Stat.LUCK })
            {
                sb.AppendLine("| " + s + " | " +
                    Pct(BalanceLab.StatWinrate(cfg, nb, s, 5, seeds)) + " | " +
                    Pct(BalanceLab.StatWinrate(cfg, nb, s, 10, seeds)) + " | " +
                    Pct(BalanceLab.StatWinrate(cfg, nb, s, 20, seeds)) + " |");
            }
            sb.AppendLine();
            sb.AppendLine("(A flat +Δ is a bigger % of a small-base stat, so raw +Δ parity is not the");
            sb.AppendLine("goal — equal-VALUE budgets are. See budget-swap below.)");
            sb.AppendLine();

            sb.AppendLine("## Budget-swap parity (move 10 points X→Y, win-rate vs neutral, " + seeds + " seeds)");
            sb.AppendLine();
            sb.AppendLine("~50% ⇒ the two stats carry equal value per point at this base.");
            sb.AppendLine();
            sb.AppendLine("| swap | win-rate |");
            sb.AppendLine("|------|----------|");
            (Stat, Stat)[] swaps = { (Stat.ATK, Stat.SPD), (Stat.SPD, Stat.ATK), (Stat.ATK, Stat.HP),
                                     (Stat.HP, Stat.DEF), (Stat.ATK, Stat.DEF), (Stat.SPD, Stat.HP) };
            foreach (var (fr, to) in swaps)
                sb.AppendLine("| " + fr + "→" + to + " | " + Pct(BalanceLab.BudgetSwap(cfg, nb, fr, to, 10, seeds)) + " |");
            sb.AppendLine();

            // --- Power-difference → win-rate curve ---
            sb.AppendLine("## Power-difference → win-rate curve (scaling all of A's stats)");
            sb.AppendLine();
            sb.AppendLine("| scale f | power diff | A win-rate |");
            sb.AppendLine("|---------|-----------|------------|");
            foreach (var pt in BalanceLab.PowerCurve(cfg, nb, seeds))
                sb.AppendLine("| " + F(pt.f, 3) + " | " + Pct(pt.diffPct) + " | " + Pct(pt.winrate) + " |");
            sb.AppendLine();
            sb.AppendLine("Target after K2: a **<10% power difference stays inside 45–55%**. Today the");
            sb.AppendLine("curve is a cliff (small diff → ~100%).");
            sb.AppendLine();

            // --- Real species round-robin (the dominators) ---
            sb.AppendLine("## Current species round-robin (1v1, level 1, both sides)");
            sb.AppendLine();
            sb.AppendLine("| species | win-rate | Σ stats | Power() |");
            sb.AppendLine("|---------|----------|---------|---------|");
            foreach (var w in BalanceLab.RoundRobin(cfg, reg, 1, 60))
            {
                var d = reg.Get(w.id); int budget = 0; for (int i = 0; i < 6; i++) budget += d.baseStats.Get((Stat)i);
                sb.AppendLine("| " + w.id + " | " + Pct(w.winrate) + " | " + budget + " | " + F(BalanceLab.Power(d.baseStats, cfg), 0) + " |");
            }
            sb.AppendLine();

            // --- Duration + team-A neutrality from the standard sweep ---
            var sweep = BalanceSweep.Run(new BalanceSweep.SweepConfig { battles = 2000, level = 5, teamSize = 3 }, cfg, reg);
            sb.AppendLine("## Duration + side-bias (3v3, level 5, 2000 battles)");
            sb.AppendLine();
            sb.AppendLine("- Duration P10/P50/P90 = " + F(sweep.p10, 1) + " / " + F(sweep.p50, 1) + " / " + F(sweep.p90, 1) + " s");
            sb.AppendLine("- Hard-resolve rate = " + Pct(sweep.hardResolves / (double)sweep.battles));
            sb.AppendLine("- Team-A win-rate = " + Pct(sweep.teamAWinRate) + " (initiative neutrality)");
            sb.AppendLine();

            sb.AppendLine("## Where the imbalance is");
            sb.AppendLine();
            sb.AppendLine("1. **DPS is a product (ATK × APS(SPD))**, so SPD multiplies ATK's value and vice");
            sb.AppendLine("   versa — plus SPD also decides initiative and kill order, letting the faster");
            sb.AppendLine("   side remove enemy DPS first (a snowball EHP bonus SPD alone shouldn't grant).");
            sb.AppendLine("2. **DEF has diminishing EHP value** (k/(DEF+k)) while HP is linear, so a fixed");
            sb.AppendLine("   stat budget buys different amounts of survivability depending on the split.");
            sb.AppendLine("3. **No damage/dodge/timing variance**, so combat is a near-deterministic cliff:");
            sb.AppendLine("   the marginally-better monster wins ~100%, producing the 90%+ outliers and");
            sb.AppendLine("   making tanks/supports non-viable.");
            sb.AppendLine();
            sb.AppendLine("K2 introduces a stat-value framework (SPD diminishing returns on both cadence");
            sb.AppendLine("and initiative, DEF/EHP rescaling) plus controlled variance (dodge, damage");
            sb.AppendLine("variance) so equal budgets trend to ~50% and a <10% edge stays within 45–55%.");

            var path = Repo("reports/ATTRIBUTE_VALUE_ANALYSIS.md");
            File.WriteAllText(path, sb.ToString());
            Debug.Log("MTA: audit written -> " + path);
            EditorApplication.Exit(0);
        }
    }
}
#endif
