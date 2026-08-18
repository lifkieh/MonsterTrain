# Polish 003 — Battle Round-Pip HUD

Date: 2026-08-18. Presentation-only. Adds a fighting-game round-pip HUD to the
battle screen so the fight has a clear "who's winning" read over the cinematic
arena. No new systems, no gameplay change; deterministic sim + hash untouched.

## Problem
On device the battle screen had no team-strength indicator, and the "BATTLE"
title was overdrawn by the arena. The cinematic looked good but you couldn't tell
the standings at a glance.

## Change (`BattleReplayView.cs`, view layer only)
- **Round pips** — one pip per monster, player team left, enemy right, with a
  center "VS". Pips are screen-fixed (parented to the battle root, **not** the
  shaken/zoomed stage) so they stay readable through camera work. Each frame they
  fill from `BattlePlayback.AliveCount(team)` — a pip goes dark as that side loses
  a fighter. Built above the arena (`SetAsLastSibling`), so it's never occluded.
- Uses an inline UI label helper (the `MTA.App` `UIFactory` isn't referenceable
  from `MTA.Battle` — App depends on Battle, not the reverse).

## Verification
- Full EditMode suite: **58 / 58 pass** (HUD reads playback state only; no pure-C#
  logic changed).
- Android APK: built.
- `BattleCinematicDirector` and all gameplay/data untouched.

## Constraints honored
Presentation only · no combat formula / `balance.json` / simulator changes ·
same seed ⇒ identical winner + battle hash · no functionality removed.
