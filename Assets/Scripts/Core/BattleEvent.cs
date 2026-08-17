using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MTA.Core
{
    // The event log is the ONLY contract between simulation and presentation,
    // and its hash is the determinism test.
    public class BattleEvent
    {
        public double t;
        public string kind;          // Start, Action, Modifier, ModifierExpired, Died, StallTick, End
        public int actorTeam = -1, actorSlot = -1;
        public string skillId = "";
        public int targetTeam = -1, targetSlot = -1;
        public int raw, final;
        public bool crit;
        public string extra = "";

        public string CanonicalLine()
        {
            var ci = CultureInfo.InvariantCulture;
            return string.Join("|",
                t.ToString("R", ci), kind, actorTeam, actorSlot, skillId,
                targetTeam, targetSlot, raw, final, crit ? 1 : 0, extra);
        }
    }

    public class BattleResult
    {
        public int winnerTeam;                    // 0 or 1
        public EndReason endReason;
        public double duration;
        public List<BattleEvent> events = new List<BattleEvent>();
        public ulong logHash;

        public static ulong Fnv1a64(IEnumerable<BattleEvent> events)
        {
            const ulong offset = 14695981039346656037UL, prime = 1099511628211UL;
            ulong hash = offset;
            foreach (var e in events)
            {
                var bytes = Encoding.UTF8.GetBytes(e.CanonicalLine());
                for (int i = 0; i < bytes.Length; i++)
                { hash ^= bytes[i]; hash *= prime; }
                hash ^= (byte)'\n'; hash *= prime;
            }
            return hash;
        }
    }
}
