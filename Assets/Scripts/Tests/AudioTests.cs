using System;
using MTA.Battle;
using MTA.Meta;
using NUnit.Framework;

namespace MTA.Tests
{
    // Phase D: SFX are synthesised (non-empty clips) and mute persists.
    public class AudioTests
    {
        [Test]
        public void Sfx_GeneratesEveryClip()
        {
            foreach (Sfx id in Enum.GetValues(typeof(Sfx)))
            {
                var c = SfxLibrary.Generate(id);
                Assert.IsNotNull(c, id.ToString());
                Assert.Greater(c.samples, 0, id.ToString());
            }
        }

        [Test]
        public void Mute_PersistsInSave()
        {
            var d = new SaveData { muted = true };
            var j = UnityEngine.JsonUtility.ToJson(d);
            var d2 = UnityEngine.JsonUtility.FromJson<SaveData>(j);
            Assert.IsTrue(d2.muted);
        }
    }
}
