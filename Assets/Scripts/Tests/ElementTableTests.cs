using MTA.Core;
using NUnit.Framework;

namespace MTA.Tests
{
    // Element system 2.0 balance verification (TYM 2.0 Phase 2/3).
    public class ElementTableTests
    {
        [Test]
        public void EveryElement_HasStrengthAndWeakness()
        {
            foreach (var e in ElementTable.All)
            {
                Assert.Greater(ElementTable.StrengthCount(e), 0, e + " has no strength");
                Assert.Greater(ElementTable.WeaknessCount(e), 0, e + " has no weakness");
            }
        }

        [Test]
        public void Table_IsSymmetricConsistent()
        {
            // Valid pairings: normal (A>B ⇒ B<A), mutual counters (Light↔Shadow: both strong), or mutual
            // neutral. NEVER one-sided (one advantaged while the other sees neutral) and never mutual
            // disadvantage.
            foreach (var a in ElementTable.All)
                foreach (var b in ElementTable.All)
                {
                    if (a == b) continue;
                    int ab = ElementTable.Advantage(a, b), ba = ElementTable.Advantage(b, a);
                    Assert.AreEqual(ab == 0, ba == 0, a + "/" + b + " one-sided into neutral");
                    Assert.IsFalse(ab == -1 && ba == -1, a + "/" + b + " mutual disadvantage (impossible)");
                    if (ab == -1) Assert.AreEqual(1, ba, a + "<" + b + " must be beaten");
                }
        }

        [Test]
        public void Void_IsPureNeutral()
        {
            foreach (var e in ElementTable.All)
            {
                Assert.AreEqual(0, ElementTable.Advantage("Void", e));
                Assert.AreEqual(0, ElementTable.Advantage(e, "Void"));
            }
            Assert.AreEqual(0, ElementTable.Advantage("Void", "Void"));
        }

        [Test]
        public void NoElement_StrictlyDominates()
        {
            // net (strengths − weaknesses) may not exceed +1 for any element (no OP element).
            foreach (var e in ElementTable.All)
                Assert.LessOrEqual(ElementTable.StrengthCount(e) - ElementTable.WeaknessCount(e), 1,
                    e + " is net-dominant");
        }
    }
}
