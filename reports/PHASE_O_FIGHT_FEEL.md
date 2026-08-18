# Phase O — Fight Feel Core

Date: 2026-08-19 · Presentation layer only · Author: Lifkie Lie

**Objective.** Make hits feel heavy and motion feel alive with pure code — squash &
stretch, victim reactions, tiered hit-stop, anime impact frames, afterimages. No new
assets. Built on the O-1 brawl + O-2 tawuran staging, applying the "Amendments to Phase O"
from both addenda.

## Files touched (presentation only)
- `Assets/Scripts/Battle/UnitView.cs` — a **deform layer** on the ART child (independent
  of `combatOffset`/choreography): squash & stretch, velocity lean, launcher/slam spin,
  hit-stop vibrate, impact-frame silhouette; slower knockback ease-out; sprite/pos
  exposure for afterimages.
- `Assets/Scripts/Battle/BattleReplayView.cs` — pooled afterimages (×16), impact frames
  (ult + finisher only), tiered/capped hit-stop, anticipation squash, spins, ground
  bounce on slam, victim squash+vibrate, dust puffs.

Gameplay/Core/sim/balance/save/`.asset`, the engagement planner, and the cinematic
director are all untouched. Determinism intact (the deform layer is a pure function of
motion/time — no `UnityEngine.Random` added).

## What was built, by task
1. **Squash & stretch (attacker).** Anticipation crouch `(1.10, 0.85)` for ~0.07 s before
   a dash, then velocity-driven stretch during the dash (auto: `±|vel|·0.00035`, capped
   0.16, perpendicular squash at half), and a `(0.9, 1.12)` landing overshoot. Deform is
   applied to `_artRt` only, so `combatOffset` math is untouched.
2. **Velocity lean & spins.** Sprite leans toward horizontal motion, clamped ±12°
   (mirror-aware). Full spin on launcher rise (720°/s, 0.55 s) and slam descent (900°/s).
3. **Victim selling.** Knockback ease-out slowed to ~0.25 s (impulse decay 7→4.5) so the
   scrum re-closes the gap; impact squash `(1.2, 0.8)` (heavier on slam `(1.35, 0.68)`,
   ranged ult `(1.3, 0.72)`); ground bounce ×2 (~50 % then ~25 % height) with dust puffs
   on the slam; victim vibrate ±2.5–3 px during hit-stop.
4. **Tiered + capped hit-stop.** Light/compact/ground-combo hits **do not freeze**
   (amendment). Global freeze only on heavy/crit/ult/KO: launcher 0.09 s, slam 0.09 s
   (ult 0.15), ranged crit 0.09 / ult 0.15, KO 0.10 (finisher 0.15) — all drawn from the
   per-battle `FREEZE_CAP = 3.5 s` budget (`HitStop` no-ops once spent).
5. **Impact frames (anime).** On **ultimate and finisher only** (amendment — not every
   crit): ~0.05 s of a near-black full-screen darken with the victim silhouette flashed
   white on top. `try/finally` guarantees the overlay + silhouette are always restored, so
   an exception can never leave the screen black.
6. **Afterimages.** Pooled ghost `Image`s (pool 16, pre-warmed at battle start, sized for
   six concurrent units per amendment). During dash / launcher / slam movement a ghost
   spawns every ~0.035 s: a copy of the current sprite tinted the actor's element colour,
   alpha 0.5 → 0 over 0.2 s. Zero per-spawn allocation (fixed pool + array-driven fade);
   no ghost is ever left on screen.
7. **Seeded randomness.** The new deform/juice is deterministic (squash/spin/vibrate/
   afterimage are pure functions of motion & time — no RNG), so determinism is preserved
   with nothing new to seed.

## Amendments applied
- Global hit-stop only for heavy/crit/ult/KO; light hits get flash + knockback + vibrate
  **without** clock freeze (O-1 + O-2 amendment).
- Impact frames only on ultimates and the finisher (O-1 amendment).
- Victim knockback operates within engagements — pushback along the engagement axis, the
  partner re-closes via the O-2 engagement tracking (O-2 amendment).
- Afterimage pool sized ×6 (16 ghosts) (O-1/O-2 amendment).

## Final parameter values
| Effect | Value |
|---|---|
| Anticipation squash / dur | (1.10, 0.85) / 0.07 s |
| Dash land overshoot | (0.9, 1.12) / 0.06 s |
| Velocity stretch gain / cap | 0.00035 per px/s / 0.16 |
| Lean clamp | ±12° |
| Launcher spin / dur | 720°/s / 0.55 s |
| Slam spin / dur | 900°/s / 0.16 s |
| Victim impact squash (ground/air/slam) | (1.2,0.8) / (1.15,0.86) / (1.35,0.68) |
| Vibrate magnitude / freq | 2.5–3 px / ~90 Hz x, ~78 Hz y |
| Knockback ease-out | impulse decay 4.5 (~0.25 s) |
| Ground bounce heights | 50 %, 25 % |
| Hit-stop: light / heavy / crit / ult / KO | 0 / 0.09 / 0.09 / 0.15 / 0.10 (fin 0.15) |
| Freeze cap per battle | 3.5 s |
| Impact frame duration | 0.05 s (ult + finisher only) |
| Afterimage pool / spawn interval / fade | 16 / 0.035 s / 0.2 s |

## Tests / build
- **EditMode: 69 / 69 passed** — determinism, sim, balance, save, progression, planner,
  director all unchanged and green (Phase O is view-only juice).
- **PlayMode UI smoke: PASS** — full battle with the new deform, afterimages, impact
  frames, spins and bounces; 0 runtime errors, 0 stuck ghosts, 0 misplaced buttons.
- **Android APK: Succeeded** — `Build/Android/TrainYourMonster.apk` (~75 MB;
  `MTA: Android build = Succeeded`) — also confirms the Editor-only PlayNow tool never
  enters the player build.

## Human QA checklist (verify on device)
- [ ] Hits feel weighty — pause + shake + knockback; launchers/slams feel acrobatic
      (spin up, spike down, bounce).
- [ ] Impact frames pop on crits… — actually only on **ultimates and the K.O.** (per the
      amendment). If they feel too strong/seizure-y, tell me and I'll dial the darken/hold.
- [ ] No stretched-sprite artifacts, no ghost sprites stuck on screen, stable FPS on
      device with six units + VFX.
