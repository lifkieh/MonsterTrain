using System;
using System.Collections.Generic;
using MTA.Meta;
using NUnit.Framework;

namespace MTA.Tests
{
    // TYM 2.0 economy verification (gacha / pity / fusion / selling). Pure + deterministic.
    public class MonsterEconomyTests
    {
        static List<string>[] Pool()
        {
            var p = new List<string>[6];
            p[1] = new List<string> { "c1", "c2" };
            p[2] = new List<string> { "r1", "r2" };
            p[3] = new List<string> { "e1" };
            p[4] = new List<string> { "l1" };
            p[5] = new List<string> { "m1" };
            return p;
        }

        [Test]
        public void Summon_Deterministic_SameSeed()
        {
            var a = new SaveData { gachaSeed = 12345 };
            var b = new SaveData { gachaSeed = 12345 };
            for (int i = 0; i < 50; i++)
            {
                var ida = MonsterGacha.Summon(a, Pool(), out var ra, out _);
                var idb = MonsterGacha.Summon(b, Pool(), out var rb, out _);
                Assert.AreEqual(ida, idb);
                Assert.AreEqual(ra, rb);
            }
        }

        [Test]
        public void Rates_RoughlyMatch()
        {
            var sv = new SaveData { gachaSeed = 777 };
            int[] cnt = new int[6];
            const int N = 20000;
            for (int i = 0; i < N; i++) cnt[MonsterGacha.RollRarity(sv)]++;
            Assert.That(cnt[1] / (double)N, Is.InRange(0.45, 0.60), "common ~55%");
            Assert.That(cnt[5] / (double)N, Is.GreaterThan(0.015), "mythical >~2% (rate + pity)");
        }

        [Test]
        public void Pity_GuaranteesTiers()
        {
            var sv = new SaveData { gachaSeed = 999 };
            int e = 0, l = 0, m = 0, maxE = 0, maxL = 0, maxM = 0;
            for (int i = 0; i < 3000; i++)
            {
                int r = MonsterGacha.RollRarity(sv);
                e = r >= 3 ? 0 : e + 1; maxE = Math.Max(maxE, e);
                l = r >= 4 ? 0 : l + 1; maxL = Math.Max(maxL, l);
                m = r >= 5 ? 0 : m + 1; maxM = Math.Max(maxM, m);
            }
            Assert.LessOrEqual(maxE, MonsterGacha.PityEpic - 1, "epic within pity");
            Assert.LessOrEqual(maxL, MonsterGacha.PityLeg - 1, "legendary within pity");
            Assert.LessOrEqual(maxM, MonsterGacha.PityMyth - 1, "mythical within pity");
        }

        [Test]
        public void Fusion_RaisesStar_ConsumesCopies_CapsAt5()
        {
            var mon = new MonsterSave { speciesId = "wolf", star = 1, count = 2 };
            Assert.IsTrue(MonsterFusion.CanFuse(mon));
            Assert.IsTrue(MonsterFusion.Fuse(mon));
            Assert.AreEqual(2, mon.star);
            Assert.AreEqual(1, mon.count);                 // consumed 1 fuel copy
            Assert.IsFalse(MonsterFusion.CanFuse(mon));     // ★2→★3 needs 2 fuel + base = 3 copies
            var maxed = new MonsterSave { star = 5, count = 99 };
            Assert.IsFalse(MonsterFusion.CanFuse(maxed));   // ★5 is the cap (no rarity upgrade)
        }

        [Test]
        public void Selling_NeverSellsLastCopy()
        {
            var sv = new SaveData();
            var mon = new MonsterSave { speciesId = "wolf", count = 1 };
            Assert.IsFalse(MonsterSelling.Sell(sv, mon, 3, out _, out _));   // last copy protected
            mon.count = 3;
            Assert.IsTrue(MonsterSelling.Sell(sv, mon, 3, out var coins, out var ess));
            Assert.AreEqual(2, mon.count);
            Assert.Greater(coins, 0);
            Assert.Greater(ess, 0);
        }
    }
}
