# Audio Design Report

Date: 2026-08-19 · Presentation only · Author: Lifkie Lie

Audited the whole audio system for **clear feedback on every key event** and added a proper
**dynamic music mix**. Engine: `AudioManager` (procedural synth clips with real CC0
overrides where present; 8-source SFX pool; crossfading music; per-play pitch; PlayerPrefs
volumes; mute in save). No gameplay/sim/determinism/save changes.

## Event audio audit
| Event | Feedback | Status |
|---|---|---|
| Menu music | `PlayMusic(Menu)` on every non-battle screen | ✅ |
| Battle music | `PlayMusic(Battle)` on battle start (real CC0 track override) | ✅ |
| **Boss music** | `Music.Boss` existed but was **never triggered** | ⚠️ **fixed** — now plays on each **league-finale** career stage (index %3==2: stages 3/6/9/12/15/18) |
| Victory | `Music.Victory` + `Sfx.Victory` + announcer "VICTORY" | ✅ |
| Defeat | `Music.Defeat` + `Sfx.Defeat` | ✅ |
| Attack (normal hit) | `Sfx.Hit` per connecting hit | ✅ |
| Crit | `Impact()` layer (hit + bass + crit), pitch-varied | ✅ |
| Ultimate | super-flash: `Whoosh` + `Ultimate` + `Bass`; `Impact(ult)` on landing | ✅ |
| **Dodge** | was `Sfx.Hover` (a UI blip — wrong/weak) | ⚠️ **fixed** — now a distinct **`Whoosh` swish** (pitched up) |
| Counter | `Sfx.Hit` + announcer "COUNTER" + splash | ✅ |
| Evolution | `Sfx.Evolution` (arpeggio sweep) | ✅ |
| Level up | `Sfx.LevelUp` (rising triad) on train + result | ✅ |

Every listed event now has clear, distinct feedback. (Filler whiff/block/shove keep the
quiet `Hover` blip on purpose so they never compete with real hits.)

## Dynamic mixing — four states
Music intensity now maps to four explicit battle states (drives volume 0.70→1.05 and pitch
1.0→1.09 on the music bus, plus the finisher duck):

1. **Battle normal** — a clear lead (≥2 alive difference): intensity ≈ 0.35, ebbing with the
   lead. Music sits back.
2. **Battle close** — teams even (equal alive count, >2 total): intensity 0.70. Music lifts +
   pitches up slightly.
3. **Last monster alive** — total alive ≤ 2 (down to the wire): intensity 1.0 (full climax).
4. **Final finisher** — on the finishing blow, `SetFinisher()`: the music **ducks to ~40 %
   then swells back up to ~110 %** over ~1 s (so the K.O., announcer and impact cut through),
   plus a low **bass boom**. Pairs with the existing slow-mo / letterbox.

Implementation:
- `AudioManager`: `SetFinisher()` (duck envelope `_duckT` + bass), widened intensity→volume
  curve, `SetBattleIntensity`.
- `BattleReplayView`: the 4-state intensity calc each frame; `SetFinisher()` on the endsBattle
  death; `bossMusic` flag → `Music.Boss`; dodge → `Whoosh`.
- `GameBootstrap`: sets `_view.bossMusic` for league-finale stages before `Play`.

## Notes / minor (non-blocking)
- Audio volumes persist in PlayerPrefs; mute persists in the save (documented split).
- All music/SFX are deterministic procedural synth unless a real CC0 clip is present in
  `Resources/Audio` (battle theme + creature SFX are already overridden). Announcer voices are
  synth stingers until a CC0 voice pack is dropped in (see `reports/ASSET_SOURCING.md`).

## Validation
- EditMode 75/75, PlayMode smoke PASS, Android APK builds.

## Human QA (device — audio)
- [ ] Menu/battle/victory/defeat music all play; a **league-finale** stage sounds different
      (boss theme).
- [ ] Dodge has a clear **swish** (not a UI click); counter/ult/crit/level-up/evolution each
      read distinctly.
- [ ] Down to the last monster the music **peaks**; on the finishing blow it **ducks then
      swells** with a bass boom under the K.O.
- [ ] Nothing clips/peaks painfully at max SFX + music volume.
