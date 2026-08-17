namespace MTA.Meta
{
    public struct IdColor
    {
        public float r, g, b;
        public IdColor(float r, float g, float b) { this.r = r; this.g = g; this.b = b; }
    }

    // Deterministic per-species visual identity (color, icon initial, crit word,
    // skill word) derived from the speciesId. Pure C#, no UnityEngine, testable.
    // Presentation only — no gameplay effect, no balance change.
    public static class SpeciesIdentity
    {
        static uint Hash(string s)
        {
            uint h = 2166136261u;
            if (s != null) foreach (char c in s) { h ^= c; h *= 16777619u; }
            return h;
        }

        public static IdColor ColorFor(string id)
        {
            uint h = Hash(id);
            float hue = (h % 360u) / 360f;
            return HsvToRgb(hue, 0.55f, 0.85f);
        }

        public static string Initial(string id)
        {
            if (string.IsNullOrEmpty(id)) return "?";
            var parts = id.Split('_');
            string s = parts[0].Substring(0, 1);
            if (parts.Length > 1 && parts[1].Length > 0) s += parts[1].Substring(0, 1);
            return s.ToUpperInvariant();
        }

        static readonly string[] Crits = { "SMASH!", "CRUSH!", "PIERCE!", "SLICE!", "BLAST!", "SHATTER!", "RUPTURE!", "DEVASTATE!" };
        static readonly string[] Skills = { "Unleashed", "Focus Strike", "Onslaught", "Channeling", "Fury", "Surge", "Rampage", "Gambit" };

        public static string CritWord(string id) => Crits[Hash(id) % (uint)Crits.Length];
        public static string SkillWord(string id) => Skills[(Hash(id) / 7u) % (uint)Skills.Length];

        static IdColor HsvToRgb(float h, float s, float v)
        {
            float i = (float)System.Math.Floor(h * 6f);
            float f = h * 6f - i;
            float p = v * (1f - s);
            float q = v * (1f - f * s);
            float t = v * (1f - (1f - f) * s);
            switch (((int)i) % 6)
            {
                case 0: return new IdColor(v, t, p);
                case 1: return new IdColor(q, v, p);
                case 2: return new IdColor(p, v, t);
                case 3: return new IdColor(p, q, v);
                case 4: return new IdColor(t, p, v);
                default: return new IdColor(v, p, q);
            }
        }
    }
}
