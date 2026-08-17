# Battle Presentation Upgrade

Date: 2026-08-17. Turned the static/log battle view into a readable auto-battler
replay. **Presentation only** — no mechanics, no simulator outcomes, no
`balance.json`, no determinism change. Same seed → identical result + identical
log hash (asserted by tests).

## Design principle

The simulator already emits a complete event log (`Spawn`, `Action` with
`skillId`+`crit`+heal-by-team, `Modifier`, `Died`, `End`). The upgrade **does not
add or change simulator events** (that would change the hash). Instead a
presentation layer **classifies** the existing log into typed replay events and
animates them. Simulator stays the single source of truth.

## Files changed

**New:**
- `Assets/Scripts/Meta/ReplayEvent.cs` — `ReplayEvent` + `ReplayBuilder`
  (classifies the sim log → Spawn/Attack/Skill/Ultimate/Heal/Death/Victory; pure C#).
- `Assets/Scripts/Meta/BattleDrama.cs` — post-sim stats + win-tier classification (pure C#).
- `Assets/Scripts/Battle/FloatingCombatText.cs` — pooled rise/fade combat text.
- `Assets/Scripts/Tests/ReplayPresentationTests.cs` — 4 tests (Phase J).

**Rewritten (presentation):**
- `Assets/Scripts/Battle/UnitView.cs` — procedural animation.
- `Assets/Scripts/Battle/BattleReplayView.cs` — staging, pacing, event-driven
  animation, camera shake/zoom, floating text.

**Edited:**
- `Assets/Scripts/App/GameBootstrap.cs` — builds the replay + slot map, speed
  buttons, upgraded result screen (drama banner + leaders).

**Untouched (guaranteed):** all of `Core/` (simulator, determinism, event log)
and `Assets/StreamingAssets/balance.json`.

## New systems (by phase)

- **A — Event model:** `ReplayBuilder` maps the sim log to typed `ReplayEvent`s.
  Crit/damage/heal are carried as fields on the cast event (info preserved). Skill
  slot resolved from the loaded species (`skillId → SkillSlot`), no hardcoding.
- **B — Unit visuals:** idle float + breathe · dash attack (bigger on ultimate) ·
  hit shake + white flash (stronger on crit) · heal green pulse + bounce · death
  fade + sink. All procedural (no sprites).
- **C — Staging:** Team A left, Team B right, 3 lanes (rows 380 apart) + per-slot
  depth so front/mid/back read; portrait 1080-ref; units never overlap.
- **D — Attack viz:** attacker dashes, target flashes + shakes, damage text; skills
  show "SKILL", ultimates "ULTIMATE" + camera punch, crits "CRIT!" + bigger shake;
  heals show green "+N".
- **E — Pacing:** the sim plays across a **15–60 s** window scaled from sim
  duration; **speed multiplier 0.5× / 1× / 2× / 4×** buttons. The simulator is
  never slowed — only playback timing.
- **F — Drama:** after sim, computes total damage, healing, survivors, lead
  changes → tier **DOMINANT / ADVANTAGE / CLOSE / CLUTCH** with banner ("Total
  Domination", "Decisive Victory", "Hard-Fought Win", "Clutch Victory").
- **G — Camera:** stage shake on impacts, bigger punch on ultimates, victory zoom.
- **H — Floating text:** pooled; rise + fade + recycle; white damage, green heal,
  yellow crit, orange skill/ultimate.
- **I — Result screen:** Winner · Battle Duration · Damage/Kills/Healing leaders ·
  drama banner · **Play Again** / **Back To Menu**.

## Tests (Phase J)

Full EditMode suite: **19 / 19 pass** (15 prior + 4 new). New:
- `Replay_EventsAreTimeOrdered` — replay stream is time-ordered.
- `Replay_ShowsAllActionKinds` — Spawn/Attack/Death/Victory + at least one
  Skill/Ultimate present (so attacks/skills/deaths are visible in the data).
- `Replay_DoesNotChangeOutcomeOrHash` — building the replay + drama does not mutate
  the result; **same seed → same winner and same `logHash`** (determinism intact).
- `Drama_ClassifiesAndReportsLeaders` — valid tier, banner, duration, leaders.

## Performance notes

- Animations are transform/color updates in `Update()` — no per-frame allocation
  in the hot path; floating text is **pooled** (reused, not destroyed).
- No sprites, no particles, no post-processing → minimal GPU. ≤ 6 units on screen.
- Canvas is `ScaleWithScreenSize` at 1080×1920 portrait reference; camera "shake"
  is a cheap RectTransform offset. 60 fps target is comfortable on the S24 FE.

## Known limitations

- **Visual correctness not verified by the agent** (headless build) — layout,
  readability, and animation feel need on-device human QA (`MOBILE_QA_CHECKLIST`).
- Placeholder colored quads (no monster sprites yet — Build Phase 4).
- Camera-shake jitter uses `UnityEngine.Random` — **visual only, not part of
  determinism** (never touches the simulator).
- Crit/Damage/Heal are fields on the cast event, not separate event objects — all
  information is preserved and animated; kept simple for the view.
- Buff/debuff casts (war_cry/slow_hex/rally) animate as skill/ultimate casts
  without a dedicated buff icon.
- "Lead changes" is a HP-fraction-leader heuristic for the drama tier.

## Constraints honored

No simulator/mechanics/balance/determinism change. `balance.json` untouched.
Same-seed outcome + log hash unchanged (test-proven). Simulator remains the source
of truth; the replay view consumes its events.
