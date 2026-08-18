# Fight Choreography — Naruto/Tekken Battle Movement

Date: 2026-08-18. The problem was **animation, not sprites**: the monsters didn't
move like fighters. This adds transform-driven fight choreography so battles read
like a duel. **Presentation only** — balance, determinism, outcome, and save are
untouched (EditMode 66/66 green).

## What was added (`BattleReplayView.Combo` rewrite + `UnitView.combatOffset`)
Each melee exchange now plays a real fighting-game sequence:

1. **Dash-in** — the attacker rushes across the arena to point-blank (speed-lines VFX).
2. **Ground combo** — a chain of quick strikes; the target recoils with each hit.
3. **Launcher** (crit / ultimate) — the target is knocked **into the air** with a
   "LAUNCH!" cue; the attacker jumps up after it.
4. **Air combo** — both airborne, the attacker juggles the target with rapid hits
   as it drifts upward.
5. **Slam** — the attacker spikes the target **back down to the ground** ("SLAM!")
   with a big explosion VFX, screen flash, shockwave, and heavy camera shake.
6. **Recovery** — both fighters ease back to their stance.

Plus:
- **Dodge → Counter** — the defender sidesteps (afterimage + MISS + whoosh), then
  **flicks a counter-strike** back at the attacker ("COUNTER").
- **Ranged** attackers use a compact hit sequence (no dash/air — the projectile
  already travels).
- Combo depth scales with the hit tier (light → short ground combo; crit/ult →
  full launcher + air + slam).

## How it works
`UnitView.combatOffset` is a view-driven position offset added on top of the base
stance. The `Combo` coroutine animates the attacker's and target's `combatOffset`
(dash / launch / air / slam / recovery) via an eased `MoveOffset` helper, while
holding the sim clock frozen (hit-stop) so choreography never overlaps or races the
event timeline. Movement is pure transform animation on the real CC0 sprites — no
frame-by-frame art needed, and the deterministic sim/log is never touched.

Speed still respects the 0.5×–4× playback buttons (all timings divide by the
multiplier). The single sim-accurate damage number is shown on the connecting hit
(slam for big combos, the final strike otherwise).

## Validation
- **EditMode: 66/66 pass** — sim/determinism/save untouched.
- **PlayMode UI smoke: PASS** — a full battle runs the whole choreography (dash,
  launcher, air combo, slam, dodge-counter) with **zero runtime errors**.
- APK rebuilt.

## Still open
- Sprites remain single-frame (movement is transform-based, Pokémon/paper-fighter
  style); true frame-by-frame limb animation would need per-monster animation sheets
  (not available free for 21 unique species).
- On-device visual pass recommended (best judged live).
