using MTA.Meta;
using NUnit.Framework;

namespace MTA.Tests
{
    // Phase B: per-species identity is deterministic (presentation only).
    public class IdentityTests
    {
        [Test]
        public void Identity_IsDeterministic()
        {
            var a = SpeciesIdentity.ColorFor("wolf");
            var b = SpeciesIdentity.ColorFor("wolf");
            Assert.AreEqual(a.r, b.r, 1e-6); Assert.AreEqual(a.g, b.g, 1e-6); Assert.AreEqual(a.b, b.b, 1e-6);
            Assert.AreEqual(SpeciesIdentity.CritWord("wolf"), SpeciesIdentity.CritWord("wolf"));
            Assert.AreEqual(SpeciesIdentity.SkillWord("ghost"), SpeciesIdentity.SkillWord("ghost"));
        }

        [Test]
        public void Identity_InitialsAndColorsVary()
        {
            Assert.AreEqual("FL", SpeciesIdentity.Initial("fire_lizard"));
            Assert.AreEqual("W", SpeciesIdentity.Initial("wolf"));
            Assert.AreEqual("MB", SpeciesIdentity.Initial("mushroom_beast"));
            // Different species should not all collapse to the same colour.
            var w = SpeciesIdentity.ColorFor("wolf");
            var g = SpeciesIdentity.ColorFor("ghost");
            Assert.IsFalse(Mathf3(w.r, g.r) && Mathf3(w.g, g.g) && Mathf3(w.b, g.b), "wolf and ghost share a colour");
        }

        static bool Mathf3(float a, float b) => System.Math.Abs(a - b) < 1e-4;
    }
}
