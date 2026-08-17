namespace MTA.Core
{
    [System.Serializable]
    public struct StatBlock
    {
        public int hp, atk, def, spd, intel, luck;   // "intel" because "int" is reserved

        public int Get(Stat s) => s switch
        {
            Stat.HP => hp, Stat.ATK => atk, Stat.DEF => def,
            Stat.SPD => spd, Stat.INT => intel, Stat.LUCK => luck, _ => 0
        };

        public void Set(Stat s, int value)
        {
            switch (s)
            {
                case Stat.HP: hp = value; break;
                case Stat.ATK: atk = value; break;
                case Stat.DEF: def = value; break;
                case Stat.SPD: spd = value; break;
                case Stat.INT: intel = value; break;
                case Stat.LUCK: luck = value; break;
            }
        }

        public void Add(Stat s, int delta) => Set(s, Get(s) + delta);

        public static StatBlock Sum(StatBlock a, StatBlock b) => new StatBlock
        {
            hp = a.hp + b.hp, atk = a.atk + b.atk, def = a.def + b.def,
            spd = a.spd + b.spd, intel = a.intel + b.intel, luck = a.luck + b.luck
        };
    }
}
