namespace MTA.Core
{
    // TYM 2.0 Phase 1 — Active + Support combat MATH (pure, deterministic, testable). Given the two
    // chosen support monsters, computes the modifiers applied to the single Active monster (and to the
    // enemy). Hard caps prevent any two supports from stacking into a broken combo (balance rule: no
    // overpowered support combinations). Sim/UI integration wires this into the 1-active battle next.
    public struct ActiveMods
    {
        public float atkMult;            // Buffer: attack
        public float critAdd;            // Buffer: crit chance
        public float speedMult;          // Buffer: speed
        public float ultCostReduction;   // Buffer/Void: ultimate cost / cooldown
        public float dmgReduction;        // Guardian: flat incoming reduction
        public float shieldFrac;          // Guardian: shield as fraction of max HP
        public float redirectFrac;        // Guardian: damage redirected off the active
        public float dodgeFirstHit;       // Guardian: 1 = negate the first hit
        public float regenPerSec;         // Healer: HP/sec
        public float emergencyHeal;       // Healer: burst heal fraction at low HP
        public float cleanse;             // Healer: 1 = periodic debuff cleanse
        public float enemyDefReduction;   // Debuffer
        public float enemySpeedReduction; // Debuffer
        public float dotAmp;              // Debuffer: DoT amplification
        public float enemyAccReduction;   // Debuffer
        public float summonDps;           // Summoner: extra damage-per-second contribution (fraction of atk)

        public static ActiveMods Neutral() => new ActiveMods { atkMult = 1f, speedMult = 1f };
    }

    public static class SupportCombat
    {
        // Anti-OP caps (a single strong effect is fine; two of a kind must not runaway).
        const float CapAtk = 1.5f, CapDmgRed = 0.40f, CapShield = 0.30f, CapCrit = 0.20f,
                    CapSpeed = 1.25f, CapEnemyDef = 0.30f, CapUlt = 0.30f, CapSummon = 0.80f, CapRegen = 0.06f;

        public static ActiveMods Compute(bool hasA, SupportDef a, bool hasB, SupportDef b)
        {
            var m = ActiveMods.Neutral();
            if (hasA) Apply(ref m, a);
            if (hasB) Apply(ref m, b);
            // clamp everything so no combo is broken
            m.atkMult = Clamp(m.atkMult, 1f, CapAtk);
            m.speedMult = Clamp(m.speedMult, 1f, CapSpeed);
            m.critAdd = Clamp(m.critAdd, 0f, CapCrit);
            m.dmgReduction = Clamp(m.dmgReduction, 0f, CapDmgRed);
            m.shieldFrac = Clamp(m.shieldFrac, 0f, CapShield);
            m.redirectFrac = Clamp(m.redirectFrac, 0f, 0.5f);
            m.regenPerSec = Clamp(m.regenPerSec, 0f, CapRegen);
            m.enemyDefReduction = Clamp(m.enemyDefReduction, 0f, CapEnemyDef);
            m.enemySpeedReduction = Clamp(m.enemySpeedReduction, 0f, 0.30f);
            m.enemyAccReduction = Clamp(m.enemyAccReduction, 0f, 0.30f);
            m.ultCostReduction = Clamp(m.ultCostReduction, 0f, CapUlt);
            m.summonDps = Clamp(m.summonDps, 0f, CapSummon);
            m.dodgeFirstHit = m.dodgeFirstHit > 0f ? 1f : 0f;
            m.cleanse = m.cleanse > 0f ? 1f : 0f;
            return m;
        }

        static void Apply(ref ActiveMods m, SupportDef s)
        {
            switch (s.id)
            {
                // Guardian
                case "bulwark": m.redirectFrac += s.magnitude; break;
                case "shell_wall": m.dmgReduction += s.magnitude; break;
                case "bark_ward": m.shieldFrac += s.magnitude; break;
                case "phase_veil": m.dodgeFirstHit = 1f; break;
                // Healer
                case "spore_regen": m.regenPerSec += s.magnitude; break;
                case "gel_mend": m.emergencyHeal += s.magnitude; break;
                case "field_medic": m.emergencyHeal += s.magnitude; break;
                case "split_cleanse": m.cleanse = 1f; break;
                // Buffer
                case "ignite": m.atkMult += s.magnitude; break;
                case "heat_up": m.critAdd += s.magnitude; break;
                case "drake_fury": m.atkMult += s.magnitude; break;
                case "dragon_spirit": m.speedMult += s.magnitude; break;
                case "rebirth_boon": m.ultCostReduction += s.magnitude; break;
                case "paradox_core": m.ultCostReduction += s.magnitude; break;
                // Debuffer
                case "howl": m.enemyDefReduction += s.magnitude; break;
                case "alpha_howl": m.enemyDefReduction += s.magnitude; break;
                case "ink_cloud": m.enemySpeedReduction += s.magnitude; break;
                case "venom": m.dotAmp += s.magnitude; break;
                case "screech": m.enemyAccReduction += s.magnitude; break;
                // Summoner
                case "blade_flurry": m.summonDps += s.magnitude; break;
                case "twin_slash": m.summonDps += s.magnitude * 2f; break;
                case "swarm": m.summonDps += s.magnitude * 3f; break;
            }
        }

        static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
