# Phase D — Audio + Feedback

Date: 2026-08-17. Adds procedural SFX + a mute setting. Presentation only —
deterministic sim, replay tests, and save compatibility preserved.

## Files changed
- **New** `Assets/Scripts/Battle/AudioManager.cs` — `Sfx` enum, `SfxLibrary`
  (procedural clip synth), `AudioManager` (singleton + AudioSource pool + mute).
- **New** `Assets/Scripts/Tests/AudioTests.cs`.
- Edited `App/UIFactory.cs` (button click sound), `Battle/BattleReplayView.cs`
  (hit/crit/skill/ultimate/heal/death/victory sounds), `App/GameBootstrap.cs`
  (audio init + mute toggle), `Meta/SaveData.cs` (`muted` flag), `Tests/MTA.Tests.asmdef`
  (reference MTA.Battle).

## Tasks
1. **Audio manager** — `AudioManager` singleton, `DontDestroyOnLoad`.
2. **SFX pool** — 6 pooled `AudioSource`s, round-robin `PlayOneShot`.
3. **Button click** — every `UIFactory.Button` plays a click.
4. **Battle hit** — on every damaging hit.
5. **Crit** — brighter clip on crits.
6. **Skill** — on active-skill casts.
7. **Ultimate** — on ultimate casts.
8. **Heal** — on heals.
9. **Death** — on unit death.
10. **Victory** — at battle end.
Plus **mute toggle** on the menu, **persisted** in the save (`SaveData.muted`).

All clips are synthesised at runtime (sine/noise + envelopes) — **no audio
assets**, deterministic (`SfxLibrary.Generate(id)`).

## Tests
Full EditMode suite: **30 / 30 pass** (28 prior + 2 new): `Sfx_GeneratesEveryClip`
(all clips non-empty), `Mute_PersistsInSave`. Determinism/replay/save tests still green.

## Known limitations
- Placeholder synthesised SFX (no designed audio yet); mix levels un-tuned.
- No music loop yet (SFX only).
- On-device audio verification needs a human.

## Constraints
Android primary · determinism preserved · save backward-compatible (`muted`
defaults false on old saves) · no functionality removed.
