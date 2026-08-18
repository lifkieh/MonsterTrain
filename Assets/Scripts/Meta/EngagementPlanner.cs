using System.Collections.Generic;
using MTA.Core;

namespace MTA.Meta
{
    // Phase O-2 "tawuran" engagement planner. READ-ONLY over the finished replay: it
    // never touches the sim, outcome, or logHash. Walks the complete event log and
    // derives, per unit, a continuous engagement plan the view renders as a persistent
    // street brawl (instead of slot-based attack-and-return):
    //   * segments  — who each unit is tangled with in which time window (its next real
    //                 interaction's opponent), so target-switching falls out naturally;
    //   * fillers   — presentation-only beats (whiff/block/shove) inserted in the gaps
    //                 between a unit's real events; they carry NO amount and can never
    //                 move HP by construction;
    //   * clashes   — replay indices where two units' real events target each other
    //                 within a small window (rendered as a lunge-collide).
    // Seeded entirely by logHash ⇒ same log ⇒ byte-identical plan. Pure C#, no
    // UnityEngine, edit-mode testable.
    public enum FillerKind { Whiff, Block, Shove }

    public struct EngSegment { public double t0, t1; public int oppKey; }
    public struct FillerBeat { public double t; public int unitKey, oppKey; public FillerKind kind; }

    public class EngagementPlan
    {
        public ulong seed;
        public readonly Dictionary<int, List<EngSegment>> segments = new Dictionary<int, List<EngSegment>>();
        public readonly List<FillerBeat> fillers = new List<FillerBeat>();     // sorted by t
        public readonly HashSet<int> clashEventIdx = new HashSet<int>();       // indices into replay
        public readonly Dictionary<int, float> idlePhase = new Dictionary<int, float>();
        public static int Key(int team, int slot) => team * 100 + slot;
    }

    public static class EngagementPlanner
    {
        // Deterministic xorshift64* (same family as the cinematic director).
        struct Rng
        {
            public ulong s;
            public ulong NextU() { s ^= s >> 12; s ^= s << 25; s ^= s >> 27; return s * 0x2545F4914F6CDD1DUL; }
            public int Range(int lo, int hiInclusive)
            {
                if (hiInclusive <= lo) return lo;
                return lo + (int)(NextU() % (ulong)(hiInclusive - lo + 1));
            }
            public float Frac() => (NextU() >> 40) / 16777216f;
        }

        // Tuning (logged in the phase report).
        const double FILLER_INTERVAL = 0.7;   // seconds between filler beats in a gap
        const double FILLER_LEAD = 0.28;      // keep filler this far from any real event
        const double FILLER_MIN_GAP = 0.62;   // only fill gaps longer than this
        const double CLASH_WINDOW = 0.15;     // reciprocal-event window for a clash

        static bool IsOffense(ReplayEventKind k) =>
            k == ReplayEventKind.Attack || k == ReplayEventKind.Skill || k == ReplayEventKind.Ultimate;

        public static EngagementPlan Plan(BattleResult result, List<ReplayEvent> replay)
        {
            var plan = new EngagementPlan { seed = result.logHash };
            var rng = new Rng { s = result.logHash == 0UL ? 0x9E3779B97F4A7C15UL : result.logHash };
            double dur = result.duration;

            // Units + death times from the log.
            var units = new List<int>();
            var team = new Dictionary<int, int>();
            var deathT = new Dictionary<int, double>();
            foreach (var e in replay)
                if (e.kind == ReplayEventKind.Spawn)
                {
                    int k = EngagementPlan.Key(e.actorTeam, e.actorSlot);
                    units.Add(k); team[k] = e.actorTeam; deathT[k] = double.MaxValue;
                }
            foreach (var e in replay)
                if (e.kind == ReplayEventKind.Death)
                {
                    int k = EngagementPlan.Key(e.targetTeam, e.targetSlot);
                    if (deathT.ContainsKey(k)) deathT[k] = e.t;
                }

            // Deterministic per-unit idle phase (consumed in spawn order).
            foreach (var k in units) plan.idlePhase[k] = rng.Frac() * 6.2831853f;

            // Per-unit contact list (t, opponent) + real-event times.
            var contacts = new Dictionary<int, List<KeyValuePair<double, int>>>();
            var realTimes = new Dictionary<int, List<double>>();
            foreach (var k in units) { contacts[k] = new List<KeyValuePair<double, int>>(); realTimes[k] = new List<double>(); }

            foreach (var e in replay)
            {
                if (e.isBuff) continue;
                if (IsOffense(e.kind) && e.actorSlot >= 0 && e.targetSlot >= 0 && e.actorTeam != e.targetTeam)
                {
                    int a = EngagementPlan.Key(e.actorTeam, e.actorSlot);
                    int t = EngagementPlan.Key(e.targetTeam, e.targetSlot);
                    if (contacts.ContainsKey(a)) { contacts[a].Add(new KeyValuePair<double, int>(e.t, t)); realTimes[a].Add(e.t); }
                    if (contacts.ContainsKey(t)) { contacts[t].Add(new KeyValuePair<double, int>(e.t, a)); realTimes[t].Add(e.t); }
                }
                else if (e.kind == ReplayEventKind.Heal && e.actorSlot >= 0)
                {
                    int a = EngagementPlan.Key(e.actorTeam, e.actorSlot);
                    if (realTimes.ContainsKey(a)) realTimes[a].Add(e.t);
                }
            }

            // Segments: a unit engages the opponent of its NEXT real interaction, so it is
            // always closing on / tangled with whoever it fights next (target-switch falls out).
            foreach (var k in units)
            {
                var cs = contacts[k];
                cs.Sort((x, y) => x.Key.CompareTo(y.Key));
                double dend = deathT[k] == double.MaxValue ? dur : deathT[k];
                var segs = new List<EngSegment>();
                if (cs.Count == 0)
                {
                    int opp = NearestEnemy(k, team, units);
                    segs.Add(new EngSegment { t0 = 0, t1 = dend, oppKey = opp });
                }
                else
                {
                    segs.Add(new EngSegment { t0 = 0, t1 = cs[0].Key, oppKey = cs[0].Value });
                    for (int i = 0; i < cs.Count; i++)
                    {
                        double t1 = i + 1 < cs.Count ? cs[i + 1].Key : dend;
                        int opp = i + 1 < cs.Count ? cs[i + 1].Value : cs[i].Value;
                        segs.Add(new EngSegment { t0 = cs[i].Key, t1 = t1, oppKey = opp });
                    }
                }
                plan.segments[k] = segs;
            }

            // Filler beats: fill the quiet gaps between a unit's real events. Elastic —
            // never within FILLER_LEAD of a real event, so they can't shift/mask real hits.
            foreach (var k in units)
            {
                var rt = realTimes[k]; rt.Sort();
                double dend = deathT[k] == double.MaxValue ? dur : deathT[k];
                var bounds = new List<KeyValuePair<double, double>>();
                double prev = 0.0;
                for (int i = 0; i < rt.Count; i++) { bounds.Add(new KeyValuePair<double, double>(prev, rt[i])); prev = rt[i]; }
                bounds.Add(new KeyValuePair<double, double>(prev, dend));

                foreach (var gap in bounds)
                {
                    double gs = gap.Key + FILLER_LEAD, ge = gap.Value - FILLER_LEAD;
                    if (ge - gs < FILLER_MIN_GAP) continue;
                    for (double t = gs; t <= ge; t += FILLER_INTERVAL)
                    {
                        double jitter = (rng.Frac() - 0.5) * 0.18;
                        double bt = t + jitter;
                        if (bt <= gap.Key + 0.05 || bt >= gap.Value - 0.05) continue;
                        int opp = OppAt(plan.segments[k], bt);
                        if (opp < 0) continue;
                        plan.fillers.Add(new FillerBeat { t = bt, unitKey = k, oppKey = opp, kind = (FillerKind)rng.Range(0, 2) });
                    }
                }
            }
            plan.fillers.Sort((x, y) => x.t.CompareTo(y.t));

            // Clash detection: a real event and a reciprocal real event within CLASH_WINDOW.
            for (int i = 0; i < replay.Count; i++)
            {
                var e = replay[i];
                if (e.isBuff || !IsOffense(e.kind) || e.actorTeam == e.targetTeam || e.targetSlot < 0) continue;
                for (int j = i + 1; j < replay.Count; j++)
                {
                    var f = replay[j];
                    if (f.t - e.t > CLASH_WINDOW) break;
                    if (f.isBuff || !IsOffense(f.kind) || f.targetSlot < 0) continue;
                    if (f.actorTeam == e.targetTeam && f.actorSlot == e.targetSlot &&
                        f.targetTeam == e.actorTeam && f.targetSlot == e.actorSlot)
                    {
                        plan.clashEventIdx.Add(i); plan.clashEventIdx.Add(j); break;
                    }
                }
            }
            return plan;
        }

        static int OppAt(List<EngSegment> segs, double t)
        {
            for (int i = 0; i < segs.Count; i++) if (t >= segs[i].t0 && t < segs[i].t1) return segs[i].oppKey;
            return segs.Count > 0 ? segs[segs.Count - 1].oppKey : -1;
        }

        static int NearestEnemy(int k, Dictionary<int, int> team, List<int> units)
        {
            int myTeam = team[k], best = -1;
            foreach (var u in units) { if (team[u] == myTeam) continue; if (best < 0 || u < best) best = u; }
            return best;
        }
    }
}
