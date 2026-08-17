using System.Collections.Generic;
using MTA.Core;

namespace MTA.Meta
{
    public enum WinTier { DominantWin, AdvantageWin, CloseWin, ClutchWin }

    // Post-simulation replay statistics + drama classification. PRESENTATION ONLY
    // — reads the finished BattleResult, changes nothing. Pure C#, testable.
    public class BattleDrama
    {
        public int winnerTeam = -1;
        public double duration;
        public int winnerAlive, loserAlive;
        public int leadChanges;
        public readonly int[] totalDamage = new int[2];
        public readonly int[] totalHealing = new int[2];
        public int totalKills;
        public WinTier tier;
        public string bannerTitle = "";
        public string damageLeader = "-", killsLeader = "-", healingLeader = "-";
        public string mvpSpecies = "";     // top damage dealer (for the result showcase)
        public int mvpTeam = -1;

        class U { public int maxHp, curHp, team, slot; public string sp; public bool alive = true; }

        static int Key(int team, int slot) => team * 100 + slot;
        static string Side(int team) => team == 0 ? "You" : "Enemy";

        public static BattleDrama Compute(BattleResult r)
        {
            var d = new BattleDrama { winnerTeam = r.winnerTeam, duration = r.duration };
            var units = new Dictionary<int, U>();
            var dmgBy = new Dictionary<int, int>();
            var healBy = new Dictionary<int, int>();
            var killBy = new Dictionary<int, int>();
            var lastDamager = new Dictionary<int, int>();   // targetKey -> actorKey
            int prevLeader = -2;

            foreach (var e in r.events)
            {
                switch (e.kind)
                {
                    case "Spawn":
                    {
                        int mh = e.final <= 0 ? 1 : e.final;
                        units[Key(e.actorTeam, e.actorSlot)] = new U {
                            maxHp = mh, curHp = mh, team = e.actorTeam, slot = e.actorSlot, sp = e.extra };
                        break;
                    }
                    case "Action":
                    {
                        int akey = Key(e.actorTeam, e.actorSlot);
                        if (e.actorTeam == e.targetTeam)               // heal
                        {
                            if (units.TryGetValue(Key(e.targetTeam, e.targetSlot), out var tu))
                                tu.curHp = System.Math.Min(tu.maxHp, tu.curHp + e.final);
                            healBy[akey] = Get(healBy, akey) + e.final;
                            d.totalHealing[e.actorTeam] += e.final;
                        }
                        else                                          // damage
                        {
                            if (units.TryGetValue(Key(e.targetTeam, e.targetSlot), out var tu))
                            {
                                tu.curHp = System.Math.Max(0, tu.curHp - e.final);
                                lastDamager[Key(e.targetTeam, e.targetSlot)] = akey;
                            }
                            dmgBy[akey] = Get(dmgBy, akey) + e.final;
                            d.totalDamage[e.actorTeam] += e.final;
                        }
                        int leader = Leader(units);
                        if (prevLeader != -2 && leader != -1 && prevLeader != -1 && leader != prevLeader)
                            d.leadChanges++;
                        if (leader != -1) prevLeader = leader;
                        break;
                    }
                    case "Died":
                    {
                        int tkey = Key(e.targetTeam, e.targetSlot);
                        if (units.TryGetValue(tkey, out var tu)) { tu.alive = false; tu.curHp = 0; }
                        d.totalKills++;
                        if (lastDamager.TryGetValue(tkey, out var ak)) killBy[ak] = Get(killBy, ak) + 1;
                        break;
                    }
                }
            }

            foreach (var u in units.Values)
                if (u.alive) { if (u.team == r.winnerTeam) d.winnerAlive++; else d.loserAlive++; }

            d.damageLeader = Leader(units, dmgBy);
            d.killsLeader = Leader(units, killBy);
            d.healingLeader = Leader(units, healBy);

            int mvpKey = TopKey(dmgBy);
            if (mvpKey >= 0 && units.TryGetValue(mvpKey, out var mu)) { d.mvpSpecies = mu.sp; d.mvpTeam = mu.team; }

            // Tier: comebacks (lead changes) => clutch; else by winner survivors.
            if (d.leadChanges >= 2 || (d.winnerAlive <= 1 && d.leadChanges >= 1)) { d.tier = WinTier.ClutchWin; d.bannerTitle = "Clutch Victory"; }
            else if (d.winnerAlive >= 3) { d.tier = WinTier.DominantWin; d.bannerTitle = "Total Domination"; }
            else if (d.winnerAlive == 2) { d.tier = WinTier.AdvantageWin; d.bannerTitle = "Decisive Victory"; }
            else { d.tier = WinTier.CloseWin; d.bannerTitle = "Hard-Fought Win"; }
            return d;
        }

        static int Get(Dictionary<int, int> m, int k) => m.TryGetValue(k, out var v) ? v : 0;

        static int TopKey(Dictionary<int, int> by)
        {
            int bestKey = -1, best = 0;
            foreach (var kv in by) if (kv.Value > best) { best = kv.Value; bestKey = kv.Key; }
            return bestKey;
        }

        // Team with higher summed HP fraction; -1 if tied.
        static int Leader(Dictionary<int, U> units)
        {
            double a = 0, b = 0;
            foreach (var u in units.Values)
            {
                double f = u.curHp / (double)u.maxHp;
                if (u.team == 0) a += f; else b += f;
            }
            if (System.Math.Abs(a - b) < 1e-6) return -1;
            return a > b ? 0 : 1;
        }

        static string Leader(Dictionary<int, U> units, Dictionary<int, int> by)
        {
            int bestKey = -1, best = 0;
            foreach (var kv in by) if (kv.Value > best) { best = kv.Value; bestKey = kv.Key; }
            if (bestKey < 0 || best <= 0) return "-";
            return units.TryGetValue(bestKey, out var u)
                ? u.sp + " (" + Side(u.team) + ") — " + best
                : "-";
        }
    }
}
