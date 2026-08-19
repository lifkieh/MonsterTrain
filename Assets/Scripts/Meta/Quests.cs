using System;
using System.Collections.Generic;

namespace MTA.Meta
{
    // Quest system (Phase U). Pure C#, no UnityEngine — edit-mode testable.
    // Progress is DERIVED from SaveData counters (never double-counted); only the
    // "claimed" flag is persisted per quest. Daily quests reset when the day index
    // changes. Rewards are granted on claim (coins + player XP). Presentation/meta
    // only — never touches balance or the battle sim.
    public enum QuestKind { Daily, Progress, Milestone }

    public class QuestDef
    {
        public readonly string id, title;
        public readonly int target, coins, xp;
        public readonly QuestKind kind;
        public readonly Func<SaveData, int> progress;   // current progress from save state

        public QuestDef(string id, string title, int target, int coins, int xp, QuestKind kind, Func<SaveData, int> progress)
        { this.id = id; this.title = title; this.target = target; this.coins = coins; this.xp = xp; this.kind = kind; this.progress = progress; }
    }

    public static class Quests
    {
        // Daily quests read per-day counters (reset by SyncDay); the rest read lifetime stats.
        public static readonly QuestDef[] Defs =
        {
            new QuestDef("daily_play",  "Play 3 battles today",   3, 30, 20, QuestKind.Daily, s => s.dailyBattles),
            new QuestDef("daily_win",   "Win 2 battles today",    2, 45, 30, QuestKind.Daily, s => s.dailyWins),
            new QuestDef("daily_train", "Train a monster today",  1, 35, 25, QuestKind.Daily, s => s.dailyTrains),

            new QuestDef("prog_win10",  "Win 10 battles",        10, 120, 100, QuestKind.Progress, s => s.battlesWon),
            new QuestDef("prog_level5", "Reach Trainer Lv 5",     5, 100,  80, QuestKind.Progress, s => s.playerLevel),
            new QuestDef("prog_coll12", "Collect 12 monsters",   12, 140, 110, QuestKind.Progress, s => s.unlocked.Count),
            new QuestDef("prog_evolve", "Evolve a monster",       1, 130, 100, QuestKind.Progress, s => s.evolutionsDone),

            new QuestDef("mile_win50",  "Win 50 battles",        50, 400, 300, QuestKind.Milestone, s => s.battlesWon),
            new QuestDef("mile_league", "Complete a league",      1, 300, 240, QuestKind.Milestone, s => s.leaguesCompleted),
            new QuestDef("mile_combo",  "Land a 15-hit combo",   15, 260, 200, QuestKind.Milestone, s => s.bestCombo),
        };

        // Ensure daily counters/claims reset when the day rolls over.
        public static void SyncDay(SaveData s, int dayIndex)
        {
            if (s.questDay == dayIndex) return;
            s.questDay = dayIndex;
            s.dailyWins = s.dailyBattles = s.dailyTrains = 0;
            foreach (var def in Defs)
                if (def.kind == QuestKind.Daily)
                {
                    var q = s.Quest(def.id);
                    if (q != null) { q.claimed = false; q.progress = 0; }
                }
        }

        public static int Progress(SaveData s, QuestDef def) => Math.Min(def.target, def.progress(s));
        public static bool IsComplete(SaveData s, QuestDef def) => def.progress(s) >= def.target;
        public static bool IsClaimed(SaveData s, QuestDef def) { var q = s.Quest(def.id); return q != null && q.claimed; }
        public static bool CanClaim(SaveData s, QuestDef def) => IsComplete(s, def) && !IsClaimed(s, def);

        // Claim a completed, unclaimed quest → grants coins + player XP (levels handled
        // by the caller's UI if desired). Returns false if not claimable.
        public static bool Claim(SaveData s, QuestDef def, out int coins, out int xp)
        {
            coins = 0; xp = 0;
            if (!CanClaim(s, def)) return false;
            var q = s.Quest(def.id);
            if (q == null) { q = new QuestState { id = def.id }; s.quests.Add(q); }
            q.claimed = true; q.progress = def.target;
            coins = def.coins; xp = def.xp;
            s.coins += coins;
            s.playerXp += xp;
            while (s.playerLevel < Progression.MaxLevel && s.playerXp >= Progression.PlayerXpForNext(s.playerLevel))
            { s.playerXp -= Progression.PlayerXpForNext(s.playerLevel); s.playerLevel++; }
            return true;
        }

        // How many quests are complete and waiting to be claimed (for a menu badge).
        public static int UnclaimedReady(SaveData s)
        {
            int n = 0;
            foreach (var def in Defs) if (CanClaim(s, def)) n++;
            return n;
        }
    }
}
