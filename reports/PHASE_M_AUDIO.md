# Phase M — Audio System

Date: 2026-08-18. Turns the near-silent prototype into a full procedural audio
framework. **Presentation only** — no gameplay/determinism/save changes. Volumes
persist in **PlayerPrefs** (not the save file), so the save system is untouched.

## Audio Manager (`AudioManager.cs`)
Centralized singleton (`DontDestroyOnLoad`) with:
- **Three buses** — Music, SFX, UI — each with an **independent 0–1 volume**,
  persisted in PlayerPrefs (`vol_music`, `vol_sfx`, `vol_ui`) and exposed via
  **sliders in Settings**. Master **mute** retained (from the profile).
- Pooled `AudioSource`s (8) for SFX/UI, plus **two crossfading music sources**.

## Music (`MusicLibrary.cs`)
**Procedurally generated** looping tracks (no audio assets — chord arpeggio + bass
+ pad, synthesized to `AudioClip`s):
- **Menu** (calm major), **Battle** (driving minor), **Boss** (tense, faster),
  **Victory** (major), **Defeat** (somber minor).
Music crossfades on scene/phase change: Menu on all menu screens, Battle in combat,
Victory/Defeat on the result screen.

## Sound effects (`SfxLibrary`)
Synthesized clips for: **Click, Hover, Attack(Hit), Critical, Skill, Ultimate,
Heal, Death, Victory, Defeat, Level Up, Evolution, Reward Claim**. Categorized to
the UI bus (Click/Hover/Reward) or SFX bus. Wired to the real events: buttons,
combat hits/crits/skills/ults/heals/deaths, level-ups (train + battle), evolution,
daily reward, and defeat.

## Dynamic battle audio
`AudioManager.SetBattleIntensity(0..1)`, driven each frame from the battle:
- **Closeness** (even survivor counts) raises intensity.
- **Down to the last monster** → **climax layer** (intensity → 1: music volume +
  pitch lift).
- **Victory sting** on the finishing blow (Sfx.Victory) + Victory music.

## Files changed
- Rewrote `Battle/AudioManager.cs` (buses, volumes, music, dynamic intensity, new SFX).
- New `MusicLibrary` (in the same file).
- Edited `App/GameBootstrap.cs` (music per phase, SFX hooks, volume sliders),
  `Battle/BattleReplayView.cs` (Battle music + intensity), `App/UIFactory.cs`
  (slider + hover-capable buttons).

## Validation
- **PlayMode smoke PASS** — music generation, crossfade, sliders, and SFX all run
  with zero runtime errors.
- Volumes persist across sessions via PlayerPrefs; save file schema unchanged.
- All audio procedural — no external music/SFX assets required (Android-safe).
