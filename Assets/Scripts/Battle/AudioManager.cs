using System;
using System.Collections.Generic;
using UnityEngine;

namespace MTA.Battle
{
    public enum Sfx { Click, Hit, Crit, Skill, Ultimate, Heal, Death, Victory }

    // Procedurally-synthesised SFX clips (no audio assets). Deterministic.
    public static class SfxLibrary
    {
        public static AudioClip Generate(Sfx id)
        {
            var rng = new System.Random(1234 + (int)id);
            float N() => (float)(rng.NextDouble() * 2.0 - 1.0);
            switch (id)
            {
                case Sfx.Click: return Make("click", 0.06f, (t) => Sin(1200, t) * Env(t, 40));
                case Sfx.Hit: return Make("hit", 0.12f, (t) => Sin(180, t) * Env(t, 22) + N() * 0.3f * Env(t, 30));
                case Sfx.Crit: return Make("crit", 0.18f, (t) => Sin(900, t) * Env(t, 14) + N() * 0.4f * Env(t, 20));
                case Sfx.Skill: return Make("skill", 0.25f, (t) => Mathf.Sin(2 * Mathf.PI * (400 + 800 * t) * t) * Env(t, 8));
                case Sfx.Ultimate: return Make("ult", 0.5f, (t) => Mathf.Sin(2 * Mathf.PI * (120 + 60 * Mathf.Sin(6 * t)) * t) * Env(t, 4) + N() * 0.3f * Env(t, 6));
                case Sfx.Heal: return Make("heal", 0.3f, (t) => Mathf.Sin(2 * Mathf.PI * (500 + 300 * t) * t) * Env(t, 6) * 0.6f);
                case Sfx.Death: return Make("death", 0.35f, (t) => Mathf.Sin(2 * Mathf.PI * (300 - 200 * t) * t) * Env(t, 7));
                default: return Make("victory", 0.5f, (t) => Sin(t < 0.25f ? 660 : 880, t) * Env(t, 3) * 0.6f);
            }
        }

        static float Sin(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);
        static float Env(float t, float k) => Mathf.Exp(-t * k);

        static AudioClip Make(string name, float dur, Func<float, float> wave)
        {
            const int rate = 44100;
            int n = Mathf.Max(1, (int)(rate * dur));
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)rate) * 0.4f, -1f, 1f);
            var c = AudioClip.Create(name, n, 1, rate, false);
            c.SetData(data, 0);
            return c;
        }
    }

    // Runtime SFX player with an AudioSource pool + persisted mute. Singleton.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        public static bool Muted;

        readonly Dictionary<Sfx, AudioClip> _clips = new Dictionary<Sfx, AudioClip>();
        AudioSource[] _pool; int _next;

        public static AudioManager Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            return go.AddComponent<AudioManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            foreach (Sfx id in Enum.GetValues(typeof(Sfx))) _clips[id] = SfxLibrary.Generate(id);
            _pool = new AudioSource[6];
            for (int i = 0; i < _pool.Length; i++) { _pool[i] = gameObject.AddComponent<AudioSource>(); _pool[i].playOnAwake = false; }
        }

        public static void Play(Sfx id)
        {
            if (Muted || Instance == null) return;
            Instance.PlayInternal(id);
        }

        public static void PlayClick() => Play(Sfx.Click);

        void PlayInternal(Sfx id)
        {
            if (!_clips.TryGetValue(id, out var clip) || clip == null || _pool == null) return;
            var src = _pool[_next]; _next = (_next + 1) % _pool.Length;
            src.PlayOneShot(clip);
        }
    }
}
