namespace MTA.Battle
{
    // Per-SPECIES motion identity (Character Direction pass). Role/element gave broad classes;
    // this makes each creature move like itself: a forward-leaning impatient Fire Lizard, a low
    // slow-settling Turtle, an over-squashing Jelly, a stalking Wolf, a hovering Phoenix, an
    // immovable Golem. Static traits (stance height, lean) read even in a still; dynamic traits
    // (pace, elasticity, settle, hit style, death style) read in motion. Presentation only.
    public enum HitStyle { Recoil, Ripple, Stiff, Slide, AirWobble }   // how the body takes a blow
    public enum DeathStyle { LaunchSpin, Collapse, Tumble, Dissolve, Scatter }

    public struct CharTraits
    {
        public float lean;      // constant forward body-lean (px at the head) — VISIBLE in stills
        public float stance;    // idle vertical offset (hover up / crouch down) — VISIBLE in stills
        public float freq;      // idle pace multiplier
        public float elastic;   // squash/stretch + hit-wobble intensity (jelly high, golem low)
        public float settle;    // impulse decay rate (low = slow heavy settle, high = snappy)
        public float antic;     // attack wind-up multiplier (>1 slower telegraph, <1 quick snap)
        public HitStyle hit;
        public DeathStyle death;
    }

    public static class CharacterProfile
    {
        // Explicit profiles for the roster. Anything unlisted falls back to role/element defaults.
        public static bool TryGet(string id, out CharTraits t)
        {
            t = default;
            switch (id)
            {
                // ---- Fire / aggressive ----
                case "fire_lizard": t = M(lean: 9, stance: 0, freq: 1.15f, elastic: 1.05f, settle: 6f, antic: 0.82f, hit: HitStyle.Recoil, death: DeathStyle.Dissolve); return true;
                case "salamander":  t = M(lean: 7, stance: -4, freq: 1.12f, elastic: 1.0f, settle: 7f, antic: 0.85f, hit: HitStyle.Recoil, death: DeathStyle.Dissolve); return true;
                case "inferno_drake": t = M(lean: 4, stance: 12, freq: 1.0f, elastic: 0.95f, settle: 4f, antic: 1.0f, hit: HitStyle.AirWobble, death: DeathStyle.Dissolve); return true;
                case "phoenix":     t = M(lean: 0, stance: 22, freq: 1.05f, elastic: 0.9f, settle: 3f, antic: 1.0f, hit: HitStyle.AirWobble, death: DeathStyle.Dissolve); return true;

                // ---- Water / elastic / flowing ----
                case "jelly":       t = M(lean: 0, stance: 0, freq: 1.0f, elastic: 2.4f, settle: 7.5f, antic: 1.05f, hit: HitStyle.Ripple, death: DeathStyle.Dissolve); return true;
                case "slime":       t = M(lean: 0, stance: -6, freq: 0.95f, elastic: 2.2f, settle: 7f, antic: 1.1f, hit: HitStyle.Ripple, death: DeathStyle.Dissolve); return true;
                case "kraken":      t = M(lean: 0, stance: 0, freq: 0.85f, elastic: 1.5f, settle: 5f, antic: 1.05f, hit: HitStyle.Ripple, death: DeathStyle.Dissolve); return true;
                case "turtle":      t = M(lean: 0, stance: -14, freq: 0.55f, elastic: 0.55f, settle: 2.2f, antic: 1.5f, hit: HitStyle.Slide, death: DeathStyle.Collapse); return true;

                // ---- Heavy / immovable ----
                case "golem":       t = M(lean: 0, stance: -12, freq: 0.7f, elastic: 0.4f, settle: 2.0f, antic: 1.35f, hit: HitStyle.Stiff, death: DeathStyle.Collapse); return true;
                case "treant":      t = M(lean: 0, stance: -8, freq: 0.72f, elastic: 0.5f, settle: 2.3f, antic: 1.3f, hit: HitStyle.Stiff, death: DeathStyle.Scatter); return true;
                case "mushroom_beast": t = M(lean: 0, stance: -6, freq: 0.9f, elastic: 0.9f, settle: 4f, antic: 1.1f, hit: HitStyle.Recoil, death: DeathStyle.Scatter); return true;

                // ---- Stalkers / fast snap ----
                case "wolf":        t = M(lean: 8, stance: -10, freq: 1.3f, elastic: 1.0f, settle: 9f, antic: 0.62f, hit: HitStyle.Recoil, death: DeathStyle.Tumble); return true;
                case "dire_wolf":   t = M(lean: 9, stance: -10, freq: 1.28f, elastic: 1.0f, settle: 9f, antic: 0.6f, hit: HitStyle.Recoil, death: DeathStyle.Tumble); return true;

                // ---- Agile / darty ----
                case "mantis":      t = M(lean: 5, stance: 0, freq: 1.45f, elastic: 1.15f, settle: 9f, antic: 0.6f, hit: HitStyle.Recoil, death: DeathStyle.Scatter); return true;
                case "blade_mantis": t = M(lean: 6, stance: 2, freq: 1.5f, elastic: 1.15f, settle: 9.5f, antic: 0.55f, hit: HitStyle.Recoil, death: DeathStyle.Scatter); return true;
                case "spider":      t = M(lean: 3, stance: -6, freq: 1.4f, elastic: 1.05f, settle: 8.5f, antic: 0.65f, hit: HitStyle.Recoil, death: DeathStyle.Tumble); return true;
                case "dragonling":  t = M(lean: 3, stance: 8, freq: 1.3f, elastic: 1.0f, settle: 6f, antic: 0.8f, hit: HitStyle.AirWobble, death: DeathStyle.Tumble); return true;

                // ---- Flyers ----
                case "bat":         t = M(lean: 0, stance: 20, freq: 1.5f, elastic: 0.95f, settle: 3f, antic: 0.9f, hit: HitStyle.AirWobble, death: DeathStyle.Tumble); return true;
                case "bee":         t = M(lean: 0, stance: 16, freq: 1.65f, elastic: 0.95f, settle: 3f, antic: 0.9f, hit: HitStyle.AirWobble, death: DeathStyle.Tumble); return true;

                // ---- Floaty ----
                case "ghost":       t = M(lean: 0, stance: 12, freq: 0.8f, elastic: 1.3f, settle: 2.5f, antic: 1.05f, hit: HitStyle.Ripple, death: DeathStyle.Dissolve); return true;
                case "squire":      t = M(lean: 4, stance: -4, freq: 1.0f, elastic: 0.9f, settle: 5f, antic: 1.1f, hit: HitStyle.Recoil, death: DeathStyle.Collapse); return true;
            }
            return false;
        }

        static CharTraits M(float lean, float stance, float freq, float elastic, float settle, float antic, HitStyle hit, DeathStyle death)
            => new CharTraits { lean = lean, stance = stance, freq = freq, elastic = elastic, settle = settle, antic = antic, hit = hit, death = death };
    }
}
