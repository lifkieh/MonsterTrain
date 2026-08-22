# AUDIO DIRECTION AUDIT (Phase 8)

Presentation only. Evidence: recorded mix `reports/audio/showcase_audio.wav` (176 s) analysed with
`numpy` (envelope + spectra); code paths in `AudioManager`.

## Escalation (normal → heavy → crit → ultimate → KO)
Verified structurally + from the envelope:
- **Layered tiers** (`AudioManager.Impact`): a normal hit = one `Hit` clip; a crit adds `Bass` + `Crit`;
  an ultimate adds `Bass` + `Ultimate`; a KO uses the ultimate tier **plus** `SetFinisher()` (music duck
  + bass boom). Each step adds a layer → louder + fuller. Ultimate dominates via `Ultimate` + `Bass`.
- **From the WAV:** rms 0.126 with peaks to 1.0 → **wide dynamic range**; the envelope sparkline shows
  loud `*`/`@` spikes (ultimates / KOs) standing clearly above the `.:-` baseline of normal hits →
  escalation is audible, not flat.

## Element identity (verified last cycle, still shipped)
Fire 9623 Hz (sizzle) / Nature 4491 Hz (woody) / Water 2613 Hz (bloop) — three separated spectral bands,
routed into every crit/ult/launcher/slam/KO. Each element sounds like a different world.

## Ducking / priority
- **Ducking:** the finisher ducks the music (`_duckT`) then swells, so the KO cuts through.
- **Dynamic music:** intensity ramps with how few units remain; boss track separate.
- **Priority (honest gap):** SFX use an 8-source round-robin pool — a very dense burst could steal a
  source from a still-ringing ultimate. Not observed as masking in the capture (ult/KO peaks read
  clearly), but a dedicated priority bus would be the next refinement if pushed further.

## Masking check
No sound was found masking important information in the capture — damage/combo call-outs are visual;
the audio layer reinforces without burying. Nothing removed.

## Verdict
Impact escalates correctly, ultimate dominates, elements are distinct, the finisher ducks + swells.
Subjective mix *polish* (EQ/reverb/mastering) is not judged here — only that the direction reads.
