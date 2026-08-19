using System.IO;
using UnityEngine;

namespace MTA.Meta
{
    // JSON save/load to Application.persistentDataPath (works on Android). Atomic
    // temp-then-replace write; backward compatible via saveVersion + list defaults.
    public static class SaveSystem
    {
        static string PathFor() => Path.Combine(Application.persistentDataPath, "save.json");

        public static bool Exists() => File.Exists(PathFor());

        public static SaveData Load()
        {
            try
            {
                var p = PathFor();
                if (!File.Exists(p)) return null;
                var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(p));
                if (d == null) return null;
                if (d.unlocked == null) d.unlocked = new System.Collections.Generic.List<string>();
                if (d.seen == null) d.seen = new System.Collections.Generic.List<string>();
                if (d.rewardHistory == null) d.rewardHistory = new System.Collections.Generic.List<string>();
                if (d.collection == null) d.collection = new System.Collections.Generic.List<MonsterSave>();
                // v2 lists (old saves lack these — default them so nothing NPEs).
                if (d.quests == null) d.quests = new System.Collections.Generic.List<QuestState>();
                if (d.achievements == null) d.achievements = new System.Collections.Generic.List<string>();
                if (d.seenNews == null) d.seenNews = new System.Collections.Generic.List<string>();
                Migrate(d);
                return d;
            }
            catch { return null; }
        }

        public static void Save(SaveData d)
        {
            d.lastSaveUtc = System.DateTime.UtcNow.ToString("o");
            d.saveVersion = SaveData.CurrentVersion;
            var json = JsonUtility.ToJson(d, true);
            var p = PathFor();
            var tmp = p + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(p)) File.Delete(p);
            File.Move(tmp, p);
        }

        public static void Delete() { var p = PathFor(); if (File.Exists(p)) File.Delete(p); }

        // Older saves: absent fields already default; just bump the version.
        static void Migrate(SaveData d)
        {
            if (d.saveVersion < SaveData.CurrentVersion) d.saveVersion = SaveData.CurrentVersion;
        }
    }
}
