using System;
using System.Collections.Generic;
using UnityEngine;

namespace MTA.Battle
{
    public enum Sfx { Click, Hover, Hit, Crit, Skill, Ultimate, Heal, Death, Victory, Defeat, LevelUp, Evolution, Reward, VoFight, VoCounter, VoKO, VoVictory, Bass, Whoosh }
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
                // Announcer stingers (synth fallback — overridden by a CC0 voice pack if dropped in).
                case Sfx.VoFight: return Make("vo_fight", 0.42f, t => (Sin(300 + 220 * Mathf.Clamp01(t / 0.42f), t) + 0.5f * Sin(600, t)) * Env(t % 0.42f, 3) * 0.5f);
                case Sfx.VoCounter: return Make("vo_counter", 0.34f, t => (Sin(460, t) + 0.4f * Sin(920, t)) * Env(t, 5) * 0.5f);
                case Sfx.VoKO: return Make("vo_ko", 0.5f, t => (Sin(320 - 150 * t, t) + 0.5f * Sin(150, t)) * Env(t, 4) * 0.6f + N() * 0.2f * Env(t, 8));
                case Sfx.VoVictory: return Make("vo_victory", 0.72f, t => Sin(t < 0.22f ? 523 : t < 0.44f ? 659 : 784, t) * Env(t % 0.22f, 6) * 0.5f);
                case Sfx.Bass: return Make("bass", 0.3f, t => (Sin(60, t) + 0.5f * Sin(92, t)) * Env(t, 11));
                case Sfx.Whoosh: return Make("whoosh", 0.22f, t => N() * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.22f)) * 0.5f);
                default: return Make("victory", 0.5f, t => Sin(t < 0.25f ? 660 : 880, t) * Env(t, 3) * 0.6f);
            }
        }

        // Element-signature impact layer (P4 audio identity): fire crackles/sizzles, water
        // splashes with a downward bloop, nature lands with an organic low thud + rustle.
        // Synthesised so each element SOUNDS like a different world. Deterministic.
        public static AudioClip GenerateElement(string element)
        {
            var rng = new System.Random(999 + (element == null ? 0 : element.GetHashCode()));
            float N() => (float)(rng.NextDouble() * 2.0 - 1.0);
            switch (element)
            {
                case "Fire":   return Make("el_fire", 0.22f, t => (N() * Env(t, 15) + 0.4f * Sin(1400f + 700f * Mathf.Sin(70f * t), t) * Env(t, 9)) * 0.85f);
                case "Water":  return Make("el_water", 0.28f, t => (Sin(540f - 380f * Mathf.Clamp01(t / 0.28f), t) * Env(t, 8) + 0.5f * N() * Env(t, 34)) * 0.85f);
                case "Nature": return Make("el_nature", 0.26f, t => (Sin(140f, t) * Env(t, 7) + 0.32f * Sin(300f, t) * Env(t, 15) + 0.1f * N() * Env(t, 22)) * 0.9f);   // woody knock (mid), distinct from fire's sizzle + water's bloop
                default: return null;
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
                case Music.Battle:
                {
                    var real = Resources.Load<AudioClip>("Audio/music_battle");   // CC0 downloaded track
                    return real != null ? real : Loop("battle", 128, new[] { 0, 3, 7, 10 }, minor: true, drive: 0.7f, dur: 3.75f);
                }
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
        readonly Dictionary<string, AudioClip> _elemClips = new Dictionary<string, AudioClip>();   // element-signature impact layers (P4)
        readonly Dictionary<Music, AudioClip> _music = new Dictionary<Music, AudioClip>();
        AudioSource[] _pool; int _next;
        AudioSource _musicA, _musicB, _activeSrc, _oldSrc;
        Music _current = Music.None; float _intensity = 0f, _fade = 1f, _duckT;

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
            foreach (var el in new[] { "Fire", "Water", "Nature" }) _elemClips[el] = SfxLibrary.GenerateElement(el);   // element impact layers
            // Override key combat SFX with real CC0 creature sounds if present.
            OverrideSfx(Sfx.Hit, "Audio/sfx_bug_02");
            OverrideSfx(Sfx.Crit, "Audio/sfx_roar_01");
            OverrideSfx(Sfx.Ultimate, "Audio/sfx_roar_01");
            OverrideSfx(Sfx.Death, "Audio/sfx_burble_01");
            OverrideSfx(Sfx.Heal, "Audio/sfx_cute_03");
            // Optional CC0 announcer + impact packs (drop into Resources/Audio to override synth).
            OverrideSfx(Sfx.VoFight, "Audio/vo_fight");
            OverrideSfx(Sfx.VoCounter, "Audio/vo_counter");
            OverrideSfx(Sfx.VoKO, "Audio/vo_ko");
            OverrideSfx(Sfx.VoVictory, "Audio/vo_victory");
            OverrideSfx(Sfx.Bass, "Audio/impact_bass");
            _pool = new AudioSource[8];
            for (int i = 0; i < _pool.Length; i++) { _pool[i] = gameObject.AddComponent<AudioSource>(); _pool[i].playOnAwake = false; }
            _musicA = gameObject.AddComponent<AudioSource>(); _musicB = gameObject.AddComponent<AudioSource>();
            _musicA.loop = _musicB.loop = true; _musicA.playOnAwake = _musicB.playOnAwake = false;
        }

        void OverrideSfx(Sfx id, string resourcePath)
        {
            var c = Resources.Load<AudioClip>(resourcePath);
            if (c != null) _clips[id] = c;
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
            src.pitch = 1f;
            src.PlayOneShot(clip, Mathf.Clamp01(vol));
        }

        // Play with a pitch (for seeded ±10% variation / bass layering).
        public static void PlayPitched(Sfx id, float pitch, float volScale) { if (!Muted && Instance != null) Instance.PlayPitchedInternal(id, pitch, volScale); }
        void PlayPitchedInternal(Sfx id, float pitch, float volScale)
        {
            if (!_clips.TryGetValue(id, out var clip) || clip == null || _pool == null) return;
            float vol = (SfxLibrary.Bus(id) == AudioBus.Ui ? UiVolume : SfxVolume) * Mathf.Clamp(volScale, 0f, 1.5f);
            var src = _pool[_next]; _next = (_next + 1) % _pool.Length;
            src.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            src.PlayOneShot(clip, Mathf.Clamp01(vol));
        }

        // Announcer callout (synth stinger, or a CC0 voice clip if present).
        public static void Announce(Sfx voice) => Play(voice);

        // Layered impact for crit/ult/KO only: hit + bass thump (+ crit/ult) + element signature.
        public static void Impact(bool ult, bool crit, float pitch, string element = "")
        {
            if (Muted || Instance == null) return;
            Instance.PlayPitchedInternal(Sfx.Hit, pitch, 0.9f);
            Instance.PlayPitchedInternal(Sfx.Bass, pitch * 0.98f, ult ? 1f : 0.7f);
            if (ult) Instance.PlayPitchedInternal(Sfx.Ultimate, pitch, 1f);
            else if (crit) Instance.PlayPitchedInternal(Sfx.Crit, pitch, 0.9f);
            if (!string.IsNullOrEmpty(element)) Instance.PlayElementInternal(element, pitch, ult ? 0.95f : 0.7f);   // element identity (P4)
        }

        // Element-signature sound (routed from the element VFX bursts so hits SOUND like their element).
        public static void PlayElement(string element, float pitch, float vol)
        {
            if (Muted || Instance == null) return;
            Instance.PlayElementInternal(element, pitch, vol);
        }
        void PlayElementInternal(string element, float pitch, float vol)
        {
            if (string.IsNullOrEmpty(element) || !_elemClips.TryGetValue(element, out var clip) || clip == null || _pool == null) return;
            var src = _pool[_next]; _next = (_next + 1) % _pool.Length;
            src.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            src.PlayOneShot(clip, Mathf.Clamp01(SfxVolume * vol));
        }

        // ---- Music ----
        public static void PlayMusic(Music m) { if (Instance != null) Instance.PlayMusicInternal(m); }
        public static void StopMusic() { if (Instance != null) { Instance._current = Music.None; } }
        public static void SetBattleIntensity(float x) { if (Instance != null) Instance._intensity = Mathf.Clamp01(x); }
        // Final-finisher music moment: duck the track (so the KO cuts through) then let it
        // swell back, plus a low bass boom. Called on the finishing blow.
        public static void SetFinisher() { if (Instance != null) Instance.FinisherInternal(); }
        void FinisherInternal() { _duckT = Mathf.Max(_duckT, 1.0f); if (!Muted) PlayPitchedInternal(Sfx.Bass, 0.66f, 1.2f); }

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
            float duck = 1f;
            if (_duckT > 0f) { _duckT -= Time.unscaledDeltaTime; duck = Mathf.Lerp(1.1f, 0.4f, Mathf.Clamp01(_duckT)); }   // KO: dip → swell
            float target = Muted ? 0f : MusicVolume * (0.7f + 0.35f * _intensity) * duck;   // wider dynamic range
            if (_activeSrc != null) { _activeSrc.volume = target * _fade; _activeSrc.pitch = 1f + 0.09f * _intensity; }
            if (_oldSrc != null)
            {
                _oldSrc.volume = MusicVolume * (1f - _fade);
                if (_fade >= 1f) { _oldSrc.Stop(); _oldSrc = null; }
            }
        }
    }
}
