using System.Collections.Generic;
using MTA.Core;

namespace MTA.Meta
{
    // Presentation-layer typed events, CLASSIFIED from the simulator's event log.
    // The simulator log is untouched (determinism/hash unchanged); this only
    // reinterprets existing events for the view. Crit/damage/heal are carried as
    // fields on the cast event so "who crits / who heals" stays visible.
    public enum ReplayEventKind { Spawn, Attack, Skill, Ultimate, Heal, Death, Victory }

    public class ReplayEvent
    {
        public double t;
        public ReplayEventKind kind;
        public int actorTeam = -1, actorSlot = -1;
        public int targetTeam = -1, targetSlot = -1;
        public int amount;            // damage (Attack/Skill/Ultimate) or heal (Heal)
        public bool crit;
        public bool isBuff;           // non-damage cast (buff/debuff → Modifier events)
        public string skillId = "";
        public int winnerTeam = -1;   // Victory only
    }

    // Turns a BattleResult's raw event log into an ordered ReplayEvent stream.
    // Pure C#, edit-mode testable. Consumes simulator events only.
    public static class ReplayBuilder
    {
        public static List<ReplayEvent> Build(BattleResult result, IReadOnlyDictionary<string, SkillSlot> slotOf)
        {
            var outp = new List<ReplayEvent>(result.events.Count);
            foreach (var e in result.events)
            {
                switch (e.kind)
                {
                    case "Spawn":
                        outp.Add(new ReplayEvent {
                            t = e.t, kind = ReplayEventKind.Spawn,
                            actorTeam = e.actorTeam, actorSlot = e.actorSlot, skillId = e.extra });
                        break;
                    case "Action":
                    {
                        bool heal = e.actorTeam == e.targetTeam;
                        var kind = heal ? ReplayEventKind.Heal : CastKind(e.skillId, slotOf);
                        outp.Add(new ReplayEvent {
                            t = e.t, kind = kind,
                            actorTeam = e.actorTeam, actorSlot = e.actorSlot,
                            targetTeam = e.targetTeam, targetSlot = e.targetSlot,
                            amount = e.final, crit = e.crit, skillId = e.skillId });
                        break;
                    }
                    case "Modifier":
                    {
                        var kind = CastKind(e.skillId, slotOf);
                        if (kind == ReplayEventKind.Attack) kind = ReplayEventKind.Skill;
                        outp.Add(new ReplayEvent {
                            t = e.t, kind = kind, isBuff = true,
                            actorTeam = e.actorTeam, actorSlot = e.actorSlot,
                            targetTeam = e.targetTeam, targetSlot = e.targetSlot, skillId = e.skillId });
                        break;
                    }
                    case "Died":
                        outp.Add(new ReplayEvent {
                            t = e.t, kind = ReplayEventKind.Death,
                            targetTeam = e.targetTeam, targetSlot = e.targetSlot });
                        break;
                    case "End":
                        outp.Add(new ReplayEvent {
                            t = e.t, kind = ReplayEventKind.Victory, winnerTeam = e.actorTeam });
                        break;
                    // "Start", "StallTick": no visual event.
                }
            }
            return outp;
        }

        static ReplayEventKind CastKind(string skillId, IReadOnlyDictionary<string, SkillSlot> slotOf)
        {
            if (slotOf != null && slotOf.TryGetValue(skillId, out var s))
            {
                if (s == SkillSlot.Ultimate) return ReplayEventKind.Ultimate;
                if (s == SkillSlot.Active) return ReplayEventKind.Skill;
            }
            return ReplayEventKind.Attack;
        }

        // skillId -> slot, built from the loaded species (no hardcoded ids).
        public static Dictionary<string, SkillSlot> SlotMap(IEnumerable<SpeciesData> species)
        {
            var m = new Dictionary<string, SkillSlot>();
            foreach (var sp in species)
            {
                Add(m, sp.basicSkill); Add(m, sp.activeSkill); Add(m, sp.ultimateSkill);
            }
            return m;
        }

        static void Add(Dictionary<string, SkillSlot> m, SkillData s)
        {
            if (s != null && !string.IsNullOrEmpty(s.skillId)) m[s.skillId] = s.slot;
        }
    }
}
