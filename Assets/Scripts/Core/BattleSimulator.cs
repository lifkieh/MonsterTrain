using System;
using System.Collections.Generic;

namespace MTA.Core
{
    // Headless, deterministic, seed-driven. No UnityEngine. The view layer
    // replays BattleResult.events and never computes outcomes. (conventions)
    //
    // RNG CONTRACT (single System.Random(seed), consumed in this exact order):
    //   1. Per unit, team A then B (slot order): growth rolls (0..5, skipped when
    //      growthOverride is set) then ONE initial action-time jitter roll.
    //   2. Per Damage resolution (resolution order): crit, dodge, variance — three
    //      rolls, always consumed so the stream is stable regardless of outcome.
    //   3. Hard-resolve coin flip (only if everything else ties).
    public static class BattleSimulator
    {
        // tagMode (opt-in): only the front-living monster of each team fights; the next slot
        // "tags in" when the front dies (reserves genuinely idle + safe until their turn).
        // Default false leaves the brawl path byte-identical (determinism/hash unchanged) —
        // it only re-times which units act; targeting already resolves to the front by the
        // lower-slot tie-break, so reserves are never hit while benched.
        public static BattleResult Run(TeamConfig a, TeamConfig b, int seed,
            BalanceConfig cfg, SpeciesRegistry registry, bool tagMode = false)
        {
            a.Validate(); b.Validate();
            var rng = new Random(seed);
            var state = new BattleState();
            var log = new List<BattleEvent>();

            BuildTeam(a, 0, state.teamA, seed, rng, cfg, registry);
            BuildTeam(b, 1, state.teamB, seed, rng, cfg, registry);
            if (tagMode) { BenchReserves(state.teamA); BenchReserves(state.teamB); }   // only the front acts
            log.Add(new BattleEvent { t = 0, kind = "Start",
                extra = state.teamA.Count + "v" + state.teamB.Count + ";seed=" + seed + (tagMode ? ";tag" : "") });
            // Per-unit spawn events carry maxHp + species so the view can replay
            // HP bars from the log alone (log is the only sim->view contract).
            EmitSpawns(state.teamA, log);
            EmitSpawns(state.teamB, log);

            double lastStallTick = cfg.antiStallStart;

            while (true)
            {
                var actor = ActionTimeline.NextActor(state);
                double when = actor.nextActionTime;

                if (when >= cfg.hardResolveTime)
                    return HardResolve(state, cfg, rng, log);

                state.clock = when;

                if (state.clock > cfg.antiStallStart && state.clock - lastStallTick >= cfg.antiStallInterval)
                {
                    lastStallTick += cfg.antiStallInterval;
                    log.Add(new BattleEvent { t = state.clock, kind = "StallTick",
                        extra = StatMath.StallMultiplier(state.clock, cfg)
                            .ToString(System.Globalization.CultureInfo.InvariantCulture) });
                }

                foreach (var u in state.teamA) u.PurgeExpired(state.clock);
                foreach (var u in state.teamB) u.PurgeExpired(state.clock);

                int skillIndex = ChooseSkill(actor, state, cfg);
                var killed = SkillResolver.Resolve(actor, skillIndex, state, cfg, rng, log, tagMode);
                foreach (var dead in killed)
                    log.Add(new BattleEvent { t = state.clock, kind = "Died",
                        targetTeam = dead.team, targetSlot = dead.slot });

                if (tagMode && killed.Count > 0) TagPromote(state, cfg);   // a front fell → next slot tags in

                if (skillIndex == 1)
                    actor.cooldownReadyAt[1] = state.clock + actor.skills[1].cooldownSeconds;
                if (skillIndex == 2) actor.ultimateUsed = true;

                int enemyTeam = actor.team == 0 ? 1 : 0;
                if (state.TeamWiped(enemyTeam))
                    return End(state, actor.team, EndReason.Elimination, log);

                actor.nextActionTime = state.clock +
                    StatMath.ActionInterval(actor.EffectiveStat(Stat.SPD), cfg);
            }
        }

        // AI: ultimate if charged & valid -> active if ready & valid -> basic. (spec)
        static int ChooseSkill(CombatUnit actor, BattleState s, BalanceConfig cfg)
        {
            var ult = actor.skills[2];
            if (!actor.ultimateUsed && s.clock >= ult.chargeTime &&
                SkillResolver.HasValidTarget(actor, ult, s)) return 2;

            var active = actor.skills[1];
            if (s.clock >= actor.cooldownReadyAt[1] &&
                SkillResolver.HasValidTarget(actor, active, s)) return 1;

            return 0;
        }

        static void BuildTeam(TeamConfig cfgTeam, int teamId, List<CombatUnit> into,
            int seed, Random rng, BalanceConfig cfg, SpeciesRegistry registry)
        {
            for (int slot = 0; slot < cfgTeam.units.Count; slot++)
            {
                var uc = cfgTeam.units[slot];
                var species = registry.Get(uc.speciesId);
                var inst = MonsterInstance.Create(species, uc.level, rng, uc.growthOverride);
                inst.allocated = uc.allocated;
                inst.trained = uc.trained;

                var effective = StatMath.EffectiveStats(inst, species, cfg);
                var unit = new CombatUnit
                {
                    team = teamId, slot = slot,
                    speciesId = species.speciesId, displayName = species.displayName,
                    element = species.element,
                    stats = effective,
                    maxHp = Math.Max(1, effective.hp),
                    skills = new[] { species.basicSkill, species.activeSkill, species.ultimateSkill }
                };
                unit.currentHp = unit.maxHp;
                // Initial action time carries a small jitter so raw SPD no longer
                // guarantees the first strike (initiative-snowball brake).
                double jitter = 1.0 + cfg.timingJitter * (2.0 * rng.NextDouble() - 1.0);
                unit.nextActionTime = StatMath.ActionInterval(unit.EffectiveStat(Stat.SPD), cfg) * jitter;
                // Fair, deterministic tie-break key: pure hash of seed+identity.
                // Consumes NO rng, so the growth->crit->flip RNG contract is intact.
                unit.initiativeKey = InitiativeKey(seed, teamId, slot, species.speciesId);
                into.Add(unit);
            }
        }

        // FNV-1a 64 over (seed, team, slot, speciesId). Deterministic per seed,
        // team-neutral in aggregate: removes the old team-A initiative bias
        // without touching the System.Random stream.
        static ulong InitiativeKey(int seed, int team, int slot, string speciesId)
        {
            const ulong offset = 14695981039346656037UL, prime = 1099511628211UL;
            ulong h = offset;
            h = MixInt(h, seed, prime);
            h = MixInt(h, team, prime);
            h = MixInt(h, slot, prime);
            for (int i = 0; i < speciesId.Length; i++)
            {
                char c = speciesId[i];
                h = (h ^ (byte)c) * prime;
                h = (h ^ (byte)(c >> 8)) * prime;
            }
            return h;
        }

        static ulong MixInt(ulong h, int v, ulong prime)
        {
            for (int i = 0; i < 4; i++)
                h = (h ^ (byte)(v >> (i * 8))) * prime;
            return h;
        }

        // Tag mode: park every non-front unit's action clock at +inf so the timeline never
        // picks it (targeting still ignores them because the front is always the lowest slot).
        static void BenchReserves(List<CombatUnit> team)
        {
            int frontSlot = int.MaxValue;
            for (int i = 0; i < team.Count; i++) if (team[i].Alive && team[i].slot < frontSlot) frontSlot = team[i].slot;
            for (int i = 0; i < team.Count; i++) if (team[i].slot != frontSlot) team[i].nextActionTime = double.MaxValue;
        }

        // Tag mode: whenever a team's front dies, give the newly-promoted front a live action
        // clock (it was benched at +inf) so it enters the fight from the current moment.
        static void TagPromote(BattleState s, BalanceConfig cfg) { PromoteFront(s.teamA, s, cfg); PromoteFront(s.teamB, s, cfg); }
        static void PromoteFront(List<CombatUnit> team, BattleState s, BalanceConfig cfg)
        {
            CombatUnit front = null;
            for (int i = 0; i < team.Count; i++) if (team[i].Alive && (front == null || team[i].slot < front.slot)) front = team[i];
            if (front != null && front.nextActionTime == double.MaxValue)
                front.nextActionTime = s.clock + StatMath.ActionInterval(front.EffectiveStat(Stat.SPD), cfg);
        }

        static void EmitSpawns(List<CombatUnit> team, List<BattleEvent> log)
        {
            for (int i = 0; i < team.Count; i++)
            {
                var u = team[i];
                log.Add(new BattleEvent { t = 0, kind = "Spawn",
                    actorTeam = u.team, actorSlot = u.slot, final = u.maxHp, extra = u.speciesId });
            }
        }

        static BattleResult HardResolve(BattleState s, BalanceConfig cfg, Random rng,
            List<BattleEvent> log)
        {
            s.clock = cfg.hardResolveTime;
            double pctA = HpPercentSum(s.teamA), pctB = HpPercentSum(s.teamB);
            int aliveA = AliveCount(s.teamA), aliveB = AliveCount(s.teamB);
            int winner = pctA > pctB ? 0 : pctA < pctB ? 1
                       : aliveA > aliveB ? 0 : aliveA < aliveB ? 1
                       : rng.Next(2);            // deterministic seed-derived flip
            return End(s, winner, EndReason.HardResolve, log);
        }

        static BattleResult End(BattleState s, int winner, EndReason reason,
            List<BattleEvent> log)
        {
            log.Add(new BattleEvent { t = s.clock, kind = "End",
                actorTeam = winner, extra = reason.ToString() });
            return new BattleResult { winnerTeam = winner, endReason = reason,
                duration = s.clock, events = log, logHash = BattleResult.Fnv1a64(log) };
        }

        static double HpPercentSum(List<CombatUnit> team)
        {
            double sum = 0;
            for (int i = 0; i < team.Count; i++)
                sum += team[i].currentHp / (double)team[i].maxHp;
            return sum;
        }

        static int AliveCount(List<CombatUnit> team)
        {
            int n = 0;
            for (int i = 0; i < team.Count; i++) if (team[i].Alive) n++;
            return n;
        }
    }
}
