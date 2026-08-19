# Phase AA — Dual Battle Modes (implementation)

Presentation only. No progression / monetization / balance / save / determinism change.
Implements the design from `DUAL_BATTLE_MODE_DESIGN.md`.

## What changed (code)

`BattleReplayView.cs`, `GameBootstrap.cs` (+ `StandaloneBuilder.cs`, editor tooling only).

### Mode split
- New `BattleMode { Brawl, Arena }` + `BattleReplayView.mode`.
- Assigned in `GameBootstrap`: **boss / league-finale stages → Arena**, everything else → **Brawl**
  (hangs off the same `bossMusic` flag). Normal PLAY / Daily / Career-normal = Brawl.

### Brawl — fixes the audit's #1 problem (centre pile + idle)
- Reworked `EngageAnchor`: each living unit now fights its opponent **from its own spread lane**
  instead of drifting to centre. Concretely: 50% bias back to its `Formation` home, it holds its
  lane's height, and it **can never cross the centre line** (team 0 clamped to x ≤ −70, team 1 to
  x ≥ +70). The old `Lerp(tx, 0, 0.16)` centre-drift — the direct cause of the pile — is gone.
- Kept the living circle/idle so nobody is a statue; attacks still dash across via `combatOffset`
  during the beat, then units return to their lane.
- **Camera calmed** (readability > spectacle, 6 units active): shake / zoom-punch / camera-push
  scaled ×0.5, and the zoom is capped at **1.12** (vs 1.35 in Arena) so it holds wide.

### Arena — boss / finale showcase
- `UpdateArena`: the **front-living fighter of each team duels near centre**; every other living
  unit is **benched at the flanks** (`SetReserve(true)` → 0.62 scale + dimmed) stacked by slot, and
  snaps back to the bench fast if a beat pulls it out. Camera stays aggressive.
- On a front fighter's death the next slot becomes front → it tags into the duel.

## Verified
- **76/76 EditMode** pass (incl. determinism/logHash).
- **PlayMode UI smoke 1/1** — runs a full battle through the new staging with no crash/null.
- APK rebuilt (80.6 MB, `Android build = Succeeded`).

## NOT verified — visual sign-off is still open (honest)
The brief's final gate — capture Arena/Brawl/1v1/2v2/3v3/Ultimate/KO and answer *"would I play
this 50 games in a row?"* — **was not performed, because I could not see the running game:**
- Desktop capture is impossible in this automation session (no interactive display — a standalone
  builds and launches but creates no visible window to screenshot; same root cause as the earlier
  headless-screenshot failures).
- The Android device is in active personal use (WhatsApp/TikTok), and I won't drive it or grab its
  screen.

So this stops at **compile + tests + build success, not at "looks professional."** I make **no
claim** that the battle now looks professional — that verdict requires eyes on the running build.
The changes are well-reasoned and stay inside the known arena bounds (`ClampArena` x −470..470,
y −230..205), but positioning/pacing always needs a visual tuning loop.

### Known limitation (by design, from the design doc)
Arena is a **presentation showcase, not a mechanically-real tag battle**: the simulator still
fights all six units, so a benched reserve's HP can still drop. This is the leak the design doc
called out; acceptable for a boss-flavour showcase, not a true Pokémon-style tag.

### How to verify on a device (when one is free + hands-off)
1. Install `Build/Android/TrainYourMonster.apk`.
2. **Brawl:** normal PLAY → pick 3 → battle. Check: units spread into left/right lanes, no centre
   pile, no idle statues, calm camera.
3. **Arena:** play **Career to a league-finale stage** (every `PerLeague`-th stage) → that battle
   runs Arena (duel centre + benched reserves at the flanks, punchier camera).
4. Then run the "50 games in a row?" self-audit and keep polishing until it's yes.

Pure 1v1 / 2v2 still aren't pickable from the UI (team select forces 3); observe them from
late-battle survivor states, as before.
