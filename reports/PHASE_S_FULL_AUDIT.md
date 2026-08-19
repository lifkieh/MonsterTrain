# Phase S — Full Product Audit

Date: 2026-08-19 · Author: Lifkie Lie · Method: 4 parallel read-only subsystem audits
(UI/flow, save/meta, battle/determinism/tests, Android/store) + synthesis.

## Executive summary
The game is a **feature-complete, deterministic MVP** with a solid pure-C# meta layer and
70 passing tests. The gaps that block "production quality" and a store release are, in order:
1. **No first-time onboarding** (Critical) — new players can't learn auto-battle/elements/evolution.
2. **UI clips on real 1080×2340 devices** (High) — canvas `match=0.5` + wide grids cut ~51px/side.
3. **No release pipeline** (Critical for store) — debug keystore, APK-only (no AAB), Development build.
4. **No privacy policy / Data Safety / content rating / store assets** (Critical for store).
5. **Thin long-tail** — only a 7-day daily + 18-stage career; no quests/achievements/dex screen.
6. A few **raw snake_case leaks** in celebratory popups (unlock/level-up).

None of these require touching balance, the sim, determinism, or breaking saves. Plan maps to
Phases T–Z below.

## Screen inventory (11 phases + 2 overlays)
MainMenu · TeamSelect · Battle · Result · Progress · Collection · Detail · Career · Daily ·
Settings · About; overlays: Popup (modal), Loading (auto-hide). Menu is the hub; most screens
have BACK. **Gaps:** TeamSelect has no BACK; no Android hardware-back anywhere; fresh launch
lands on Daily (no context) instead of Menu.

## Findings (severity · impact · effort · recommendation)

### A. Onboarding & UX (→ Phase T, Y)
| Sev | Issue | Impact | Effort | Fix |
|---|---|---|---|---|
| Critical | No FTUE/tutorial of any kind | New players don't grasp auto-battle, elements, roles, evolution, economy → churn | L | First-run coach flow gated on `SaveData.onboarded` (Phase T) |
| High | Element triangle never surfaced | Fire→Nature→Water advantage decides fights but is never explained | M | Matchup line on detail/team-select + onboarding page (T) |
| High | TeamSelect has no BACK (dead-end) | Player trapped until 3 picked | S | Add BACK (Phase T/Y) |
| High | No Android hardware-back | OS back does nothing / quits | S | `Escape` → phase-appropriate back (Phase Y) |
| Medium | "CONTINUE" duplicates "PLAY" | Misleading | S | Repurpose or remove (Y) |
| Medium | Battle not skippable to result | Long fights unskippable | M | Skip button (Y) |
| Low | Dev placeholder copy ("first playable", "MVP soft-launch") | Unprofessional | S | Shipping copy (Y/Z) |
| Low | Fresh launch shows Daily first, not Menu | No context | S | Menu first, daily as badge (Y) |
| Low | No persistent coin/level header | Economy invisible | M | Currency bar (Y) |
| Low | QUIT no confirm | Accidental exit | S | Confirm popup (Y) |

### B. Layout / readability at 1080×2340 (→ Phase Y)
| Sev | Issue | Impact | Effort | Fix |
|---|---|---|---|---|
| High | Canvas `matchWidthOrHeight = 0.5` | ~51px clipped each side (usable ±489 vs designed ±540) | S | Set `match = 0` (match width) — one line, fixes most clipping |
| High | Grids exceed usable width (team-select ±513, collection ±513, career ±520) | Outer columns clip on device | M | After match=0, verify; tighten pitches if needed |
| Low | Detail art overlaps XP bar | Minor overlap | S | Nudge art |
| Low | ~200px dead vertical space | Floaty layout | M | Use vertical headroom |

### C. Raw snake_case leaks (→ Phase Y, quick)
| Sev | Issue | Fix |
|---|---|---|
| High | "NEW MONSTER!" popup shows raw ids | `Nice()` the unlock ids |
| Medium | Result "Unlocked: …" raw ids | `Nice()` |
| Medium | Result "Leveled up: wolf 3->4" dev notation | Reformat "Wolf reached Lv 4" |
(Battle/result MVP/leaders already humanized — safe.)

### D. Save / meta (→ Phases T–X foundation)
| Sev | Issue | Impact | Effort | Fix |
|---|---|---|---|---|
| **Med (action item)** | `SaveSystem.Load` null-guards only 4 lists | A new List field NPEs from old saves | S | **Null-guard every new list in Load** (done this phase) |
| High | `Evolve()` leaves base id in `unlocked` | Owned-count double-counts base+evo | M | Semantics choice; Dex uses discovered = unlocked∪seen (both forms count as discovered — acceptable/attainable) |
| High | Dex completion mixes rosters (21 full vs 18 obtainable) | 100% needs the 3 evolutions | S | Compute over full roster; document evolutions required |
| Info | No quests/achievements exist | Thin retention | L | Phases U/V (greenfield, additive) |
| Low | Double Save per battle | Wasteful flash writes | S | Coalesce (optional) |
| Low | Daily uses local time / forward-clock exploit | Offline-game norm | S/M | Accept as offline-by-design; document |

Additive save fields are **backward-compatible** (JsonUtility default-fills). New fields added
this phase: `onboarded, winStreak, bestWinStreak, evolutionsDone, trainingsDone, bestCombo,
leaguesCompleted, questDay, dailyWins/Battles/Trains, quests[], achievements[], seenNews[]`.

### E. Battle / determinism / tests (→ keep green)
| Verdict | Detail |
|---|---|
| Determinism | **PASS same-device** — one `System.Random(seed)`, FNV-1a64 `logHash` proven stable over 100 runs. Cross-CPU float parity not guaranteed (only matters for server replay/PvP — not shipped). |
| Tests | **70 tests / 20 files.** Strong: determinism, balance parity, save round-trip + backward-compat, career, daily, evolution, progression. Gaps: SaveSystem file IO, migration transform, HardResolve stalemate, AudioManager runtime, quests/achievements (adding tests in U/V). |
| Minor | `Sfx.Victory` via default case (harmless); `StallTick` under-emits on big gaps (cosmetic). |

### F. Android / store readiness (→ Phase Z)
Current: pkg `com.trainyourmonster.game`, version `0.1.0` code `1`, IL2CPP + **ARM64** (64-bit ✓),
portrait, legacy Input. Packages include `com.unity.purchasing` + `com.unity.analytics`
(→ BILLING permission + data collection ⇒ privacy/Data-Safety implications).

| Sev | Blocker | Fix |
|---|---|---|
| Critical | Debug keystore (no release signing) | Release keystore + Play App Signing (documented in Z; keystore kept out of repo) |
| Critical | APK-only, no AAB | Add `BuildAab()` (`buildAppBundle=true`) — Phase Z |
| Critical | `BuildOptions.Development` (debuggable) | Release build path without Development — Phase Z |
| Critical | No privacy policy / Data Safety / content rating | Draft docs — Phase Z |
| High | `targetSdkVersion = Auto` | Pin explicitly — Phase Z |
| High | No store assets (icon/feature graphic/screenshots) | Checklists + real icon — Phase Z |
| Medium | `installLocation = preferExternal` | Set internal — Phase Z |
| Low | Unity splash logo on | Optional off — Phase Z |

## Prioritized plan → phases
- **T Onboarding:** first-run coach flow (auto-battle, elements, roles, leveling, evolution, rewards), `onboarded` flag, matchup text, TeamSelect BACK.
- **U Quests:** daily/progress/milestone quests, rewards, persistence, UI, menu badge.
- **V Achievements:** 11 achievements, save support, unlock toasts, achievement screen.
- **W Monster Dex:** full encyclopedia (portrait/element/rarity/stats/evolution chain/discovered), silhouettes for unknown.
- **X Retention:** win streaks, completion %, collection %, goal tracker.
- **Y Polish:** canvas `match=0`, grid widths, hardware-back, snake_case leaks, dead copy, overlaps, timing/spacing.
- **Z Store:** AAB + release build path, versioning, release notes, privacy/Data-Safety/content-rating/description drafts, screenshot + feature-graphic checklists.

No architecture rewrite, no system removal, determinism + save-compat preserved throughout.
