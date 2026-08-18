using System;
using System.Collections.Generic;
using UnityEngine;

namespace MTA.Battle
{
    public enum Sfx { Click, Hover, Hit, Crit, Skill, Ultimate, Heal, Death, Victory, Defeat, LevelUp, Evolution, Reward }
    public enum Music { None, Menu, Battle, Boss, Victory, Defeat }
    public enum AudioBus { Sfx, Ui }

    // Procedurally-synthesised SFX clips (no audio assets). Deterministic.
    public static class SfxLibrary
    {
        public static AudioBus Bus(Sfx id) =>
            (id == Sfx.Click || id == Sfx.Hover || id == Sfx.Reward) ? AudioBus.Ui : AudioBus.Sfx;

        public static AudioClip Generate(Sfx id)
        {
            var rng = new System.Random(1234 + (int)id);
            float N() => (float)(rng.NextDouble() * 2.0 - 1.0);
            switch (id)
            {
                case Sfx.Click: return Make("click", 0.06f, t => Sin(1200, t) * Env(t, 40));
                case Sfx.Hover: return Make("hover", 0.05f, t => Sin(1600, t) * Env(t, 55) * 0.5f);
                case Sfx.Hit: return Make("hit", 0.12f, t => Sin(180, t) * Env(t, 22) + N() * 0.3f * Env(t, 30));
                case Sfx.Crit: return Make("crit", 0.18f, t => Sin(900, t) * Env(t, 14) + N() * 0.4f * Env(t, 20));
                case Sfx.Skill: return Make("skill", 0.25f, t => Mathf.Sin(2 * Mathf.PI * (400 + 800 * t) * t) * Env(t, 8));
                case Sfx.Ultimate: return Make("ult", 0.5f, t => Mathf.Sin(2 * Mathf.PI * (120 + 60 * Mathf.Sin(6 * t)) * t) * Env(t, 4) + N() * 0.3f * Env(t, 6));
                case Sfx.Heal: return Make("heal", 0.3f, t => Mathf.Sin(2 * Mathf.PI * (500 + 300 * t) * t) * Env(t, 6) * 0.6f);
                case Sfx.Death: return Make("death", 0.35f, t => Mathf.Sin(2 * Mathf.PI * (300 - 200 * t) * t) * Env(t, 7));
                case Sfx.Defeat: return Make("defeat", 0.6f, t => Sin(220 - 80 * t, t) * Env(t, 3) * 0.6f);
                case Sfx.LevelUp: return Make("levelup", 0.4f, t => Sin(t < 0.13f ? 523 : t < 0.26f ? 659 : 784, t) * Env(t % 0.13f, 10) * 0.6f);
                case Sfx.Evolution: return Make("evolve", 0.7f, t => Mathf.Sin(2 * Mathf.PI * (300 + 500 * t) * t) * Env(t, 3) * 0.6f + Sin(t < 0.5f ? 523 : 1046, t) * Env(t, 2) * 0.3f);
                case Sfx.Reward: return Make("reward", 0.3f, t => Sin(t < 0.1f ? 784 : 1046, t) * Env(t % 0.1f, 14) * 0.55f);
                default: return Make("victory", 0.5f, t => Sin(t < 0.25f ? 660 : 880, t) * Env(t, 3) * 0.6f);
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

    // Procedural looping background music (no audio assets). Chord arpeggio + bass.
    public static class MusicLibrary
    {
        public static AudioClip Generate(Music m)
        {
            switch (m)
            {
                case Music.Menu: return Loop("menu", 80, new[] { 0, 4, 7, 12 }, minor: false, drive: 0.35f, dur: 7.2f);
                case Music.Battle: return Loop("battle", 128, new[] { 0, 3, 7, 10 }, minor: true, drive: 0.7f, dur: 3.75f);
                case Music.Boss: return Loop("boss", 150, new[] { 0, 3, 6, 10 }, minor: true, drive: 0.9f, dur: 3.2f);
                case Music.Victory: return Loop("victory_m", 120, new[] { 0, 4, 7, 12 }, minor: false, drive: 0.6f, dur: 4f);
                default: return Loop("defeat_m", 66, new[] { 0, 3, 7, 8 }, minor: true, drive: 0.3f, dur: 5.4f);
            }
        }

        static float Freq(int semi) => 220f * Mathf.Pow(2f, semi / 12f);

        static AudioClip Loop(string name, float bpm, int[] chord, bool minor, float drive, float dur)
        {
            const int rate = 44100;
            int n = Mathf.Max(1, (int)(rate * dur));
            var data = new float[n];
            float beat = 60f / bpm;
            int rootSemi = minor ? -5 : 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float bass = Mathf.Sin(2 * Mathf.PI * Freq(rootSemi - 12) * t) * 0.18f * (0.6f + 0.4f * Mathf.Sin(2 * Mathf.PI * t / beat));
                int step = (int)(t / (beat * 0.5f));
                int note = chord[step % chord.Length] + rootSemi;
                float local = (t / (beat * 0.5f)) - step;
                float pluck = Mathf.Sin(2 * Mathf.PI * Freq(note) * t) * Mathf.Exp(-local * 6f) * 0.22f * drive;
                float pad = 0f;
                for (int c = 0; c < chord.Length; c++) pad += Mathf.Sin(2 * Mathf.PI * Freq(chord[c] + rootSemi) * t);
                pad *= 0.04f;
                // gentle fade at loop seam
                float seam = Mathf.Min(1f, Mathf.Min(t, dur - t) / 0.15f);
                data[i] = Mathf.Clamp((bass + pluck + pad) * seam, -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }

    // Central audio: SFX/UI pools + crossfading music, category volumes (PlayerPrefs),
    // master mute, and a dynamic battle-intensity hook. Singleton, DontDestroyOnLoad.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        public static bool Muted;

        const string KMusic = "vol_music", KSfx = "vol_sfx", KUi = "vol_ui";
        public static float MusicVolume = 0.6f, SfxVolume = 0.9f, UiVolume = 0.9f;

        readonly Dictionary<Sfx, AudioClip> _clips = new Dictionary<Sfx, AudioClip>();
        readonly Dictionary<Music, AudioClip> _music = new Dictionary<Music, AudioClip>();
        AudioSource[] _pool; int _next;
        AudioSource _musicA, _musicB, _activeSrc, _oldSrc;
        Music _current = Music.None; float _intensity = 0f, _fade = 1f;

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
            MusicVolume = PlayerPrefs.GetFloat(KMusic, 0.6f);
            SfxVolume = PlayerPrefs.GetFloat(KSfx, 0.9f);
            UiVolume = PlayerPrefs.GetFloat(KUi, 0.9f);
            foreach (Sfx id in Enum.GetValues(typeof(Sfx))) _clips[id] = SfxLibrary.Generate(id);
            _pool = new AudioSource[8];
            for (int i = 0; i < _pool.Length; i++) { _pool[i] = gameObject.AddComponent<AudioSource>(); _pool[i].playOnAwake = false; }
            _musicA = gameObject.AddComponent<AudioSource>(); _musicB = gameObject.AddComponent<AudioSource>();
            _musicA.loop = _musicB.loop = true; _musicA.playOnAwake = _musicB.playOnAwake = false;
        }

        // ---- volume persistence ----
        public static void SetVolume(AudioBus bus, float v) { if (bus == AudioBus.Ui) UiVolume = v; else SfxVolume = v; Save(); }
        public static void SetMusicVolume(float v) { MusicVolume = Mathf.Clamp01(v); Save(); }
        static void Save()
        {
            PlayerPrefs.SetFloat(KMusic, MusicVolume); PlayerPrefs.SetFloat(KSfx, SfxVolume); PlayerPrefs.SetFloat(KUi, UiVolume);
            PlayerPrefs.Save();
        }

        // ---- SFX ----
        public static void Play(Sfx id) { if (!Muted && Instance != null) Instance.PlayInternal(id); }
        public static void PlayClick() => Play(Sfx.Click);

        void PlayInternal(Sfx id)
        {
            if (!_clips.TryGetValue(id, out var clip) || clip == null || _pool == null) return;
            float vol = SfxLibrary.Bus(id) == AudioBus.Ui ? UiVolume : SfxVolume;
            var src = _pool[_next]; _next = (_next + 1) % _pool.Length;
            src.PlayOneShot(clip, Mathf.Clamp01(vol));
        }

        // ---- Music ----
        public static void PlayMusic(Music m) { if (Instance != null) Instance.PlayMusicInternal(m); }
        public static void StopMusic() { if (Instance != null) { Instance._current = Music.None; } }
        public static void SetBattleIntensity(float x) { if (Instance != null) Instance._intensity = Mathf.Clamp01(x); }

        void PlayMusicInternal(Music m)
        {
            if (m == _current) return;
            _current = m; _intensity = 0f; _fade = 0f;
            _oldSrc = _activeSrc;
            if (m == Music.None) { _activeSrc = null; return; }
            if (!_music.TryGetValue(m, out var clip)) { clip = MusicLibrary.Generate(m); _music[m] = clip; }
            _activeSrc = (_activeSrc == _musicA) ? _musicB : _musicA;   // the free source
            _activeSrc.clip = clip; _activeSrc.volume = 0f; _activeSrc.Play();
        }

        void Update()
        {
            if (_musicA == null) return;
            _fade = Mathf.Min(1f, _fade + Time.unscaledDeltaTime * 1.5f);
            float target = Muted ? 0f : MusicVolume * (0.75f + 0.25f * _intensity);
            if (_activeSrc != null) { _activeSrc.volume = target * _fade; _activeSrc.pitch = 1f + 0.06f * _intensity; }
            if (_oldSrc != null)
            {
                _oldSrc.volume = MusicVolume * (1f - _fade);
                if (_fade >= 1f) { _oldSrc.Stop(); _oldSrc = null; }
            }
        }
    }
}
