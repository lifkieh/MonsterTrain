# PROJECT AUDIT — Train Your Monster (Monster Trainer Arena)

Date: 2026-08-17. Audit only — no gameplay, balance, or design changes. Target
remote: `https://github.com/lifkieh/MonsterTrain`.

## 1. Repository integrity

| Item | Value |
|---|---|
| Branch | `master` |
| Working tree | clean |
| Git identity | **Lifkie Lie `<llifkie@gmail.com>`** (global + local; all commits authored by it) |
| Remote origin | none configured yet (added during this audit's push) |
| Remote state | reachable, **empty** (`ls-remote` rc=0, no HEAD) → first push is a clean create |
| Credential helper | `manager` (Git Credential Manager, system) |
| Untracked | none (tree clean) |
| Ignored (on disk) | `Library/`, `Build/`, `.utmp/`, `Logs/` — correctly git-ignored |

### Latest commits (4 total)

```
9baf77d Android APK build successful
d607669 Add first-playable shell + Android build prep
083d2cb Fix deterministic initiative tie bias
8f1eb22 Phase 1 project compiles successfully
```

All authored by Lifkie Lie <llifkie@gmail.com>. No AI attribution present.

## 2. Implementation claims — VERIFIED (all present)

| Claim | File | Status |
|---|---|---|
| FirstPlayable scene | `Assets/Scenes/FirstPlayable.unity` | ✅ |
| AndroidBuilder | `Assets/Scripts/App/Editor/AndroidBuilder.cs` | ✅ |
| APK build pipeline | AndroidBuilder + `FirstPlayableSceneBuilder.cs`; APK built (`Build/Android/…apk`, ~34.8 MB, ignored) | ✅ |
| Battle simulator | `Assets/Scripts/Core/BattleSimulator.cs` | ✅ |
| Replay system | `Battle/BattleReplayView.cs` + `Meta/BattlePlayback.cs` (+ `UnitView.cs`) | ✅ |
| UI shell | `App/GameBootstrap.cs` + `App/UIFactory.cs` (menu/select/battle/result) | ✅ |
| Content generator | `Assets/Scripts/Editor/SpeciesAssetGenerator.cs` | ✅ |

## 3. Asset counts

| Metric | Count |
|---|---|
| C# scripts | **37** (Core 17 · Data 4 · Meta 5 · Battle 2 · App 4 · Editor 1 · Tests 4) |
| Assemblies (asmdef) | **8** |
| Test files / methods | 4 files / **15 test methods** |
| Scenes | **1** (FirstPlayable) |
| ScriptableObjects (`.asset`) | **22** (12 monsters + 10 skills) |
| Reports | **24** (25 including this audit) |

## 4. Checkpoint / report verification (all present)

- Checkpoints: `CHECKPOINT_001` ✅ · `002` ✅ · `003` ✅ · `004` ✅
- Android: `ANDROID_BUILD_INSTRUCTIONS` · `ANDROID_DIAGNOSTIC` · `ANDROID_RELEASE_REPORT` ✅
- Balance: `PHASE1_BALANCE_LOCK` · `P1-1_SIDE_BIAS_ANALYSIS` · `P1-1_BATTLE_DURATION_ANALYSIS` ·
  `P1-1A_SIDE_BIAS_FIX_RECOMMENDATION` · `P1-1B_IMPLEMENTATION_REPORT` · `P1-2_BASELINE_BALANCE_REPORT` ·
  `P1-3_BALANCE_STRATEGY` · `P1-4_BALANCE_EXPERIMENTS` · `P1-5_DURATION_TUNING` · `P1-6_BURST_ANALYSIS` ✅
- Playable: `FIRST_PLAYABLE_PLAN` · `FIRST_PLAYABLE_BUILD` · `BATCH_01_CONTENT_AND_FLOW` ·
  `MOBILE_QA_CHECKLIST` · `DEVICE_VALIDATION` ✅
- Other: `CONSOLIDATION_REPORT` · `RELOCATION_REPORT` · `P0-1_SETUP` ✅

## Confirmed facts

- Deterministic headless battle simulator with a verified fair initiative
  tie-break; **15/15 EditMode tests pass**, 0 compile errors (last run on the
  Android target).
- 12 species + 10 skill ScriptableObjects generated and present.
- A single code-built first-playable scene: Menu → Team-select (pick 3) → Battle
  (event-log replay) → Result → Play Again / Menu.
- Android environment installed (AndroidPlayer + SDK 34/36/37 + NDK r27c + JDK 17);
  `IsBuildTargetSupported(Android)=true`.
- **APK built:** `Build/Android/TrainYourMonster.apk` (~34.8 MB, IL2CPP/ARM64,
  min SDK 24, portrait, debug-signed dev build).
- `balance.json` unchanged since content generation; balance is amber-frozen.
- Project lives at `E:\TrainYourMonster` (relocated off OneDrive to avoid the
  earlier folder-loss issue).

## Assumptions (not fully verified)

- **APK actually runs/looks correct on a device** — unverified (no hardware; the
  agent cannot watch a GUI). Logic is tested; visuals/touch/perf are not.
- **Push will authenticate** via Git Credential Manager — expected to work
  (identity + GCM present); confirmed only when the push runs.
- The remote repo is empty, so the first push creates `master` cleanly.
- Balance numbers are "good enough to ship a playable" — deliberately deferred,
  not validated as fun.

## Missing pieces (vs the full MVP in the GDD)

- **Progression UI:** training (timers/fees), leveling, stat allocation — none.
- **Save/load** (versioned SaveGame) — none; `GameSession` is in-memory only.
- **Capture flow** ("Scouting" pick screen + nickname) — none.
- **Career mode:** 5 leagues × 8 rungs + champions + promotion gates — none.
- **Economy** (Coins, training sink) — none.
- **Production UI shell** (5 real screens) — only the 4-panel placeholder exists.
- **Art / audio / VFX** — placeholder colored quads, no sound.
- **Device validation** + **release signing/AAB** + **Play Store listing** — none.

## Technical debt

- `balance.json` is duplicated (StreamingAssets source + Resources runtime copy) —
  can drift; acceptable while frozen, needs a single-source fix later.
- UI is code-built and **visually unverified** (no play-mode/UI tests; flow is
  tested only through `GameController` logic).
- APK is **debug-signed** (not a release AAB); ARM64-only.
- Skill multipliers live in the generator/`.asset` data, not `balance.json` —
  a lever outside the "balance.json only" boundary (noted in P1-5/P1-6).
- Balance is off (role win-rate spread 2–93%, ~18 s median battles) — amber-frozen,
  owed a post-playable per-species pass with a mixed-comp metric.
- `Spawn` events were added to the sim event log (self-contained replay) — changed
  the determinism hash (self-referential test still passes; no golden to rebase).

## Remaining roadmap (to full MVP / Play Store)

1. **Device validation** — install on a mid-range Android, run `MOBILE_QA_CHECKLIST`,
   fix critical usability only.
2. **Build Phase 2 — progression:** XP-from-battle wiring, save/load, training
   timers/fees, stat-allocation UI.
3. **Build Phase 3 — content:** per-species skills, career (45-battle table +
   gates), leagues, capture flow, nicknames.
4. **Balance pass** (post-playable): per-species tuning to ~40–60% + 30–90 s.
5. **Build Phase 4 — assets:** chibi monster pack, Kenney UI, VFX, audio.
6. **Build Phase 5 — release:** release keystore + **AAB**, Play Console listing,
   privacy policy, content rating, QA, performance.

## Estimated completion

**≈ 30% of the full MVP.**

- Foundation (data layer, deterministic sim, content, tests) — the riskiest,
  make-or-break system — is **done**: ~Phase 1 complete.
- Build/release *infrastructure* (first-playable shell + Android APK pipeline) is
  **done** ahead of schedule.
- Remaining ~70% is progression + save + career/content + economy + production UI
  + art/audio + balance tuning + store release (Build Phases 2–5).
- **First-playable milestone: 100%** (built; on-device confirmation pending).

---

*Audit only. No code, balance, or design modified. Reports and counts reflect the
working tree at commit `9baf77d` prior to this file.*
