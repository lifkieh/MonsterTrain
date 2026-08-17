using MTA.Meta;
using NUnit.Framework;
using UnityEngine;

namespace MTA.Tests
{
    // Phase I: display-settings persistence + backward-compatible save defaults.
    public class ReleaseTests
    {
        [Test]
        public void NewSave_HasSaneDisplayDefaults()
        {
            var d = new SaveData();
            Assert.AreEqual(60, d.targetFps);
            Assert.AreEqual(1, d.quality);
        }

        [Test]
        public void DisplaySettings_PersistAcrossJson()
        {
            var d = new SaveData { targetFps = 30, quality = 0 };
            var d2 = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(d));
            Assert.AreEqual(30, d2.targetFps);
            Assert.AreEqual(0, d2.quality);
        }

        [Test]
        public void OldSaveMissingDisplayFields_KeepsDefaults()
        {
            // An old save JSON that predates the display fields.
            var d2 = JsonUtility.FromJson<SaveData>("{\"playerName\":\"Trainer\",\"coins\":100}");
            Assert.AreEqual(60, d2.targetFps);
            Assert.AreEqual(1, d2.quality);
            Assert.AreEqual(100, d2.coins);
        }
    }
}
