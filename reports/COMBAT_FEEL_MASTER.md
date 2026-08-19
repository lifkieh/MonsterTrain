# Combat Feel Master — Game-Feel Audit & Fixes

Date: 2026-08-19 · Presentation only · Author: Lifkie Lie

Audited the battle **from a game-feel standpoint, not features**. No new mechanics; no
change to balance, the sim, determinism, or saves. Goal: make fights read like **Naruto
Ultimate Ninja Storm / Budokai Tenkaichi / Tekken** even with single-frame sprites — via
motion, timing, and camera language layered on the transform-driven presentation.

## Stiff points found (audit) → fix applied

| # | Feel dimension | What was stiff | Fix |
|---|---|---|---|
| 1 | **Anticipation** | Only the one spotlight dash had a wind-up; compact / lunge / ranged strikes fired instantly (robotic). | New `UnitView.AttackCurve` drives **every** `PlayAttack`: a brief anticipation crouch (pull-back) before the lunge. Now all strikes telegraph. |
| 2 | **Follow-through** | Attacks returned to stance on a symmetric sine — no overshoot/settle. | Same curve adds a **follow-through overshoot past the base** then a settle (see before/after plot). |
| 3 | **Squash & stretch** | Present but conservative on fast motion. | Velocity stretch gain/cap raised (0.00035→0.0005, 0.16→0.30) so dashes read as a smear. |
| 4 | **Impact frames** | Only ultimates + the finisher got one; **crits felt soft**. | Added a lighter `MicroImpact` (short white victim silhouette + screen pop) on crits (compact/lunge/ranged/heavy-slam) — distinct from the full ult/finisher darken. |
| 5 | **Recoil** | The attacker never reacted to its own hit — it just returned. | Attacker now `Knock(-dir)` **recoils** off every connecting hit (compact, lunge, slam). |
| 6 | **Knockback** | Pure horizontal; victims slid back flatly. | Heavy hits now add a small **up-arc** (`Launch`) so victims pop up-and-back (Storm-style), not just slide. |
| 7 | **Air juggle** | Launcher→air→slam existed. | Kept; recoil + camera push + pop now punctuate the launcher and slam so the juggle reads heavier. |
| 8 | **Motion blur** | None (only discrete afterimages). | Approximated with the stronger directional velocity-stretch smear (#3) + afterimages on more moves (#9). |
| 9 | **After-image** | Only on the spotlight suite. | Afterimages now also spawn on the **compact dash** (element-tinted), so quick strikes leave trails too. |
| 10 | **Camera language** | Only symmetric shake + uniform zoom — the camera never *reacted directionally*. | New `CamPush(dir, amount)`: the camera **lurches in the hit direction** on crits, slams, ranged bigs, and KOs, then eases back. Big hits now shove the frame. |
| 11 | **Hit readability** | Victim only shook horizontally on hit. | Added a **head-snap rotation** (`_extraTilt`) — the victim's sprite tilts away from the blow and settles, so each hit's direction reads. |
| 12 | **Combo readability** | (Already good — one spotlight cinematic at a time, tiered damage numbers, live combo counter from Phase P.) | Left intact; the new recoil/head-snap/camera-push make individual hits inside a chain clearer without adding noise. |

## Before / after (core motion)
`reports/img/combat_feel_attack_curve.png` — the attack motion curve, **before** (grey,
symmetric sine: no anticipation, no follow-through) vs **after** (gold: anticipation crouch
below zero → explosive lunge → overshoot past base → settle). This single change re-times
every strike in the game toward the Storm/Tekken "wind-up + snap + recovery" feel.

## Files touched
- `Assets/Scripts/Battle/UnitView.cs` — `AttackCurve` (anticipation + follow-through),
  hit head-snap tilt, stronger velocity smear.
- `Assets/Scripts/Battle/BattleReplayView.cs` — attacker recoil, crit `MicroImpact`,
  directional `CamPush`, knockback up-arc, compact-dash afterimages.

These compose on top of the existing O–R feel systems (squash/stretch deform layer, spins,
tiered hit-stop, afterimage pool, arena reactions, ceremony) — nothing was removed.

## Validation
- **EditMode 75/75**, 0 compile errors.
- **PlayMode smoke PASS** — full brawl with the new recoil/head-snap/camera-push/afterimages,
  0 runtime exceptions.
- **Android APK builds.**
- **Before/after motion curve** rendered (above) — a deterministic, reproducible visual of
  the core timing change.

### On auto video / screenshots (honest note)
Headless frame capture does not work on this machine — Unity batchmode's `ScreenCapture`/
`WaitForEndOfFrame` yield black/no frames for a Screen-Space-Overlay UI (verified: 0 frames),
and the physical device is currently disconnected from adb. So an **automated gameplay video
was not produced this session**. The reproducible before/after motion curve is included
instead. To capture real footage, run on device (or in the GUI editor where the backbuffer is
real): open a battle and screen-record, or reconnect the phone and I'll grab stills/video via
`adb`.

## Human QA (device) — does it feel like Storm/Tenkaichi/Tekken?
- [ ] Every strike **winds up** before it lands (no instant pokes) and the attacker **snaps
      back** after (follow-through).
- [ ] Crits **freeze-flash** (micro impact frame) and the **camera lurches** toward the hit.
- [ ] Victims' heads **snap** and they **pop up-and-back** on heavy hits, not flat slides.
- [ ] Fast dashes leave **smears/afterimages**; launchers/slams feel weighty; KOs shove the camera.
- [ ] It still reads clearly — one big cinematic at a time; you always know who hit whom.
