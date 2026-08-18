using System.Collections.Generic;
using MTA.Core;

namespace MTA.Meta
{
    public enum ChoreoMove { Approach, Combo, Skill, Ultimate, Heal, Death, Victory }
    public enum ChoreoCam { Normal, ZoomCombo, ShakeCrit, CinematicZoom, SlowMoFinisher, ZoomWinner }
    public enum FinisherKind { None, TotalDomination, CloseComeback, ClutchSlowMo }

    // One choreography instruction, paired 1:1 (same index/order) with a ReplayEvent.
    public struct ChoreoBeat
    {
        public double t;
        public ChoreoMove move;
        public int actorTeam, actorSlot, targetTeam, targetSlot;
        public int hits;         // procedural combo length
        public bool crit;
        public bool dodge;        // target sidesteps before the hit lands (visual only)
        public bool launch;       // knock target into the air
        public float knockback;   // px push on the connecting hit
        public float hitStop;     // seconds (0.03–0.10)
        public int amount;
        public bool isBuff;
        public ChoreoCam cam;
        public string skillId;
        public bool endsBattle;   // the finishing blow
        public FinisherKind finisher;
    }

    public class Choreography
    {
        public readonly List<ChoreoBeat> beats = new List<ChoreoBeat>();
        public FinisherKind finisher;
        public ulong seed;
    }

    // Turns a finished battle's classified replay into fight choreography — dashes,
    // combos, dodges, knockbacks, launches, camera cues, finishers.
    //
    // READ-ONLY over the simulator output: it never writes back to the sim, so the
    // winner and logHash are guaranteed unchanged. It is seeded ENTIRELY by
    // result.logHash, so the same battle seed (⇒ same log ⇒ same hash) yields
    // byte-identical choreography every time. Pure C#, edit-mode testable.
    public static class BattleCinematicDirector
    {
        // Deterministic xorshift64* — no UnityEngine.Random, no wall-clock.
        struct Rng
        {
            public ulong s;
            public ulong NextU() { s ^= s >> 12; s ^= s << 25; s ^= s >> 27; return s * 0x2545F4914F6CDD1DUL; }
            public int Range(int lo, int hiInclusive)
            {
                if (hiInclusive <= lo) return lo;
                return lo + (int)(NextU() % (ulong)(hiInclusive - lo + 1));
            }
            public float Frac() => (NextU() >> 40) / 16777216f;   // [0,1)
        }

        public static Choreography Choreograph(BattleResult result, List<ReplayEvent> replay)
        {
            var cho = new Choreography { seed = result.logHash };
            var rng = new Rng { s = result.logHash == 0UL ? 0x9E3779B97F4A7C15UL : result.logHash };

            // Damage tiers (light / medium) from all connecting hits.
            var dmgs = new List<int>();
            foreach (var e in replay)
                if (!e.isBuff && IsOffense(e.kind)) dmgs.Add(e.amount);
            dmgs.Sort();
            int loTier = dmgs.Count > 0 ? dmgs[(int)(dmgs.Count * 0.34)] : 0;
            int hiTier = dmgs.Count > 0 ? dmgs[System.Math.Min(dmgs.Count - 1, (int)(dmgs.Count * 0.67))] : int.MaxValue;

            // Finisher class from the battle's shape.
            var drama = BattleDrama.Compute(result);
            FinisherKind fin =
                (drama.winnerAlive >= 3 && drama.leadChanges == 0) ? FinisherKind.TotalDomination :
                (drama.winnerAlive <= 1) ? FinisherKind.CloseComeback : FinisherKind.ClutchSlowMo;
            cho.finisher = fin;

            // Locate the finishing death (last Death at/before the Victory event).
            int victoryIdx = -1;
            for (int i = 0; i < replay.Count; i++) if (replay[i].kind == ReplayEventKind.Victory) { victoryIdx = i; break; }
            int endDeathIdx = -1;
            int scanTo = victoryIdx >= 0 ? victoryIdx : replay.Count;
            for (int i = 0; i < scanTo; i++) if (replay[i].kind == ReplayEventKind.Death) endDeathIdx = i;

            for (int i = 0; i < replay.Count; i++)
            {
                var e = replay[i];
                var b = new ChoreoBeat
                {
                    t = e.t,
                    actorTeam = e.actorTeam, actorSlot = e.actorSlot,
                    targetTeam = e.targetTeam, targetSlot = e.targetSlot,
                    amount = e.amount, crit = e.crit, isBuff = e.isBuff, skillId = e.skillId,
                    cam = ChoreoCam.Normal,
                };

                switch (e.kind)
                {
                    case ReplayEventKind.Attack:
                    case ReplayEventKind.Skill:
                    case ReplayEventKind.Ultimate:
                    {
                        bool ult = e.kind == ReplayEventKind.Ultimate;
                        if (e.isBuff)
                        {
                            b.move = ult ? ChoreoMove.Ultimate : ChoreoMove.Skill;
                            b.cam = ult ? ChoreoCam.CinematicZoom : ChoreoCam.Normal;
                            break;
                        }
                        b.move = ult ? ChoreoMove.Ultimate : (e.kind == ReplayEventKind.Skill ? ChoreoMove.Skill : ChoreoMove.Combo);

                        // Combo length by intensity.
                        if (ult) b.hits = rng.Range(8, 15);
                        else if (e.crit) b.hits = rng.Range(5, 8);
                        else if (e.amount > hiTier) b.hits = rng.Range(3, 5);   // medium
                        else b.hits = rng.Range(2, 3);                          // light

                        // Presentation-only dodge: low-damage, non-crit, non-ult hits
                        // sometimes get sidestepped/blocked before landing.
                        if (!ult && !e.crit && e.amount <= loTier)
                        {
                            float chance = 0.20f + rng.Frac() * 0.20f;   // 20–40%
                            b.dodge = rng.Frac() < chance;
                        }

                        b.knockback = ult ? 160f : e.crit ? 120f : 55f;
                        b.launch = ult;
                        b.hitStop = (e.crit || ult) ? 0.09f : 0.05f;
                        b.cam = ult ? ChoreoCam.CinematicZoom : e.crit ? ChoreoCam.ShakeCrit : ChoreoCam.ZoomCombo;
                        break;
                    }
                    case ReplayEventKind.Heal:
                        b.move = ChoreoMove.Heal;
                        break;
                    case ReplayEventKind.Death:
                    {
                        bool ends = i == endDeathIdx;
                        b.move = ChoreoMove.Death;
                        b.launch = true;
                        b.knockback = ends ? 240f : 170f;
                        b.hitStop = ends ? 0.10f : 0.06f;
                        b.endsBattle = ends;
                        b.finisher = ends ? fin : FinisherKind.None;
                        b.cam = ends ? ChoreoCam.SlowMoFinisher : ChoreoCam.ShakeCrit;
                        break;
                    }
                    case ReplayEventKind.Victory:
                        b.move = ChoreoMove.Victory;
                        b.cam = ChoreoCam.ZoomWinner;
                        break;
                    default:
                        b.move = ChoreoMove.Approach;
                        break;
                }
                cho.beats.Add(b);
            }
            return cho;
        }

        static bool IsOffense(ReplayEventKind k) =>
            k == ReplayEventKind.Attack || k == ReplayEventKind.Skill || k == ReplayEventKind.Ultimate;
    }
}
