# Phase Q — Super & Ceremony

Date: 2026-08-19 · Presentation layer only · Author: Lifkie Lie

**Objective.** Ceremony & hype: a VS screen, an ultimate super-flash, an upgraded finisher,
a victory ceremony, an announcer, and layered impact audio — kept readable.

## Global guardrail compliance
Balance, progression, save, and battle logic are **untouched**. All changes are in the
presentation layer (`Battle/**`, one dict fed from `App/GameBootstrap`). The super/finisher
freezes reuse the existing sim-clock hit-stop mechanism (they never change outcomes);
determinism is unaffected (no `UnityEngine.Random` added to outcome paths).

## Files touched
- `Assets/Scripts/Battle/BattleReplayView.cs` — VS screen, super flash, finisher darken +
  hold, victory ceremony, announcer + audio-layer wiring.
- `Assets/Scripts/Battle/AudioManager.cs` — announcer voices (synth), bass/whoosh,
  per-play pitch, layered `Impact()`, `Resources/Audio/vo_*`/`impact_*` override hooks.
- `Assets/Scripts/App/GameBootstrap.cs` — feeds a `rarities` dict to the view (VS stars).
- `reports/ASSET_SOURCING.md` — Kenney Impact/Voiceover packs noted (synth fallback + drop-in).

## What was built
1. **VS screen (2.6 s, tap-skip).** Before every battle the fight holds on a VS screen: each
   team's lead — real portrait (mirrored for the player), name, element (colour dot + label),
   and **rarity stars** — slams in from its side; a white flash + camera shake + "VS" pop
   fire at ~30 %; the whole thing fades out and the fight begins (ROUND 1 → FIGHT!). Tap
   anywhere skips to the end (legacy Input, confirmed active).
2. **Super flash (ultimate).** KOF/GG/Storm-style ceremony on every ultimate: the sim clock
   freezes (~1.05 s, not from the hit-stop budget), a ~70 % black dim drops in, the caster's
   **element colour** washes the screen, a **lit caster silhouette** scales in, the **skill-
   name banner** slides across, and a **diagonal portrait cut-in** slides from the corner —
   then the ult executes. `try/finally` guarantees the overlay always clears. ≤ ~1.1 s.
3. **Finisher (last KO only).** The existing slow-mo + dramatic zoom + impact flash + "K.O.!"
   are joined by an arena **darken** (fades in on the finisher, out on victory) and the ~0.5 s
   camera hold from the slow-mo, plus the letterbox bars from Phase P. Announcer "K.O.!".
4. **Victory ceremony.** On the win, the surviving winners **step forward** (staggered) and
   **victory-bounce** in place while the announcer calls "VICTORY"; the stage holds ~1.8 s
   before the result screen (which already shows **MVP / Damage / Kills(survivors) / Healing /
   Combo King**).
5. **Announcer.** FIGHT / COUNTER / K.O. / VICTORY callouts. No CC0 voice pack was available
   to download (network/adb flaky), so these are **procedural synth stingers**; the
   AudioManager has `OverrideSfx` hooks so dropping `vo_fight/vo_counter/vo_ko/vo_victory`
   into `Resources/Audio` swaps in a real voice pack with no code change (see ASSET_SOURCING).
6. **Audio layering.** For crit / ult / KO only, `AudioManager.Impact()` stacks
   hit + bass thump (+ crit/ult), with a **seeded ±10 % pitch** per play (new pitched-play
   API; pitch is reset for normal plays). A whoosh plays on dash wind-up and the super cast.
7. **Polish / readability.** Layering is gated to the big moments only (crit/ult/KO); compact
   and filler hits stay single, quiet cues. One cinematic at a time (Phase O-1 spotlight) and
   one combo counter (Phase P) still hold, so the ceremony adds punch without clutter.

## Ceremony budget
Added presentation time per battle: VS ~2.6 s + supers (~1.05 s each, few per battle) +
finisher hold ~0.5 s ≈ within the ~15 s ceremony budget. Victory hold (~1.8 s) is
post-battle. All skippable/short.

## Tests / build
- **EditMode: 69 / 69 passed** — sim, determinism, balance, save, progression, planner,
  director unchanged and green.
- **PlayMode UI smoke: PASS** — boots the game, builds the VS screen + all ceremony overlays
  and a battle; 0 runtime errors. (The smoke's battle window is consumed by the VS hold, so
  the ult super-flash / KO / victory ceremonies are best judged on device; every ceremony
  path is null-guarded and `try/finally`-restored.)
- **Android APK: Succeeded** — `Build/Android/TrainYourMonster.apk` (~75 MB;
  `MTA: Android build = Succeeded`).

## Human QA checklist (verify on device)
- [ ] Every battle opens on a **VS screen** (portraits, names, element, rarity stars, slam-in
      + flash); tapping skips it; ROUND 1 → FIGHT! follows.
- [ ] **Ultimates** feel like an event — freeze + dim + element wash + name banner + portrait
      cut-in — and you want to screenshot it. Not seizure-y; if too strong, tell me.
- [ ] The **final K.O.** is dramatic: slow-mo, darken, zoom, "K.O.!", brief hold, then the
      winners step forward and bounce before the result screen.
- [ ] Announcer calls **FIGHT / COUNTER / K.O. / VICTORY** (synth voices for now); impact
      audio on crits/ults/KO lands with weight and doesn't clip or peak painfully at max SFX.
- [ ] Battle still reads clearly — the ceremony adds hype without burying the fight.
