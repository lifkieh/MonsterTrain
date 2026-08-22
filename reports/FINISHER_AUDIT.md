# FINISHER AUDIT (Phase 7)

Presentation only. Evidence: `showcase_v11/3_arena_3v3_18` (KO), `showcase_v11/6_victory_*` (victory).

## Audit: does the final KO feel different from a normal hit?
**Yes — already strongly directed** (built across prior cycles, re-verified this pass). The finishing
blow chain is distinct from a normal hit:
- **Hit-stop** — `HitStop(0.15)` (heavier than a normal 0.09) freezes the frame on the kill.
- **Slow-mo** — `ChoreoCam.SlowMoFinisher` (`_slowmo 0.22` for 1.4 s).
- **Camera emphasis** — bigger zoom punch + shake + directional push + a cinematic pan to the clash (P2).
- **Impact frame** — near-black darken + white victim silhouette.
- **Letterbox + finisher darken** in.
- **Callout** — "K.O.!" + a finisher word (TOTAL DOMINATION / COMEBACK / CLUTCH FINISH by `BattleDrama`).
- **Audio** — `SetFinisher()` ducks the music then swells + a low bass boom; the KO impact layers the
  victim's **element** sound (P4).
- **Now: species death motion** (P4/Char pass) — the loser collapses / tumbles / dissolves per species.
- **Hero focus + victory transition** — winners step forward + pose, then the **victory hero screen**
  ("TOTAL DOMINATION" + win-tier stars) over the painted arena (`6_victory_12`).

## Verdict
The finisher is memorable and clearly separated from a normal exchange. This pass added only the
**species-specific death motion** on top (so *who* died reads in the finish). No new work required
beyond that; no regression.
