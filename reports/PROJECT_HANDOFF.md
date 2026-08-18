# Train Your Monster — Full Project Handoff (context for a new AI/dev)

> Catatan (ID): Dokumen ini rangkuman lengkap seluruh proyek + aturan wajib, biar
> AI/dev baru langsung paham tanpa ikut chat sebelumnya. Baca **STANDING RULES**
> dulu — itu tidak boleh dilanggar.

---

## 1. What the game is
**Train Your Monster** (internal: **MTA / Monster Trainer Arena**) — a Unity
Android monster-raising **auto-battler**: collect monsters, build a team, watch a
**deterministic** battle play out as an animated fighting-game replay, earn XP/coins,
level/train/evolve monsters, climb a career ladder, claim daily rewards.

- Engine: **Unity 6000.5.8f1** (Unity 6.5). Target: **Android**, IL2CPP/ARM64,
  minSdk 24, portrait, package `com.trainyourmonster.game`, version `0.1.0`.
- Repo root: `E:\TrainYourMonster`. Remote: `https://github.com/lifkieh/MonsterTrain`,
  branch `master`. HEAD at handoff: `27c52b5`.
- All UI is **built in code** (one scene `FirstPlayable.unity`, `GameBootstrap`
  MonoBehaviour builds every screen at runtime — no prefab wiring).

## 2. STANDING RULES (never break)
- **Commit author must be `Lifkie Lie <llifkie@gmail.com>`** on every commit.
- **NEVER add Claude/OpenAI/AI co-author or attribution trailers.** Verify with
  `git log -1 --format=%b | grep -i co-authored` → must be empty.
- Commit + push to `origin/master` after each meaningful unit of work.
- **Android APK must keep building** after every change.
- Two hard boundaries:
  - **Gameplay layer** (sim, balance, determinism, outcome, progression, evolution,
    save) — only touch when the task is explicitly a balance/gameplay task, and then
    re-validate. Determinism tests are **self-referential** (same seed → same
    `logHash`), so formula changes stay green but must be intentional.
  - **Presentation layer** (Battle view, UI, audio, VFX, sprites) — free to change.
    Most phases are presentation-only and must NOT touch the sim/save.
- `balance.json` is the single tuning source; keep the C# `BalanceConfig` defaults
  in sync with it (tests use the C# defaults, the game uses the JSON).
- Do not remove existing functionality. Build APK + run tests + write a
  `reports/PHASE_*.md` per phase.

## 3. Environment & CLI workflow (headless, Windows)
- Unity editor: `C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe`
- adb (Unity-bundled): `C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`
- **Run EditMode tests**: `Unity.exe -batchmode -projectPath E:/TrainYourMonster -runTests -testPlatform EditMode -testResults out.xml -logFile out.log`
- **Run PlayMode smoke** (boots real game, walks every screen + a battle, asserts no
  runtime errors): same but `-testPlatform PlayMode`.
- **Build APK**: `Unity.exe -batchmode -quit -projectPath E:/TrainYourMonster -executeMethod MTA.App.EditorTools.AndroidBuilder.BuildApk -logFile b.log` → `Build/Android/TrainYourMonster.apk` (~75 MB). Success line: `MTA: Android build = Succeeded`.
- **Balance tooling** (`-executeMethod`): `BalanceAuditRunner.RunAudit` (K1 audit),
  `.RunSpeciesTune` (per-species round-robin, edit the `TuneTable`), `.RunValidation`
  (20k-battle validation → `reports/BALANCE_VALIDATION.md`).
- **Import downloaded art**: `ExternalArtImporter.ImportAll` (+ an AssetPostprocessor
  auto-sets import settings for `Resources/MonSprites|Vfx|Arena|Ui`).
- Long builds are launched **detached** (PowerShell `Start-Process -WindowStyle Hidden`)
  so they survive session teardown. adb link on this setup is flaky — it drops often.

## 4. Architecture (9 assemblies)
- **MTA.Core** — pure headless C# sim + math. `BattleSimulator.Run(a,b,seed,cfg,reg)`
  → `BattleResult{winnerTeam,duration,events,logHash}`. `StatMath` (all stat/damage/
  APS/dodge/element math), `SkillResolver` (damage/heal/buff/debuff), `BalanceConfig`
  (every tunable), `BalanceSweep` + `BalanceLab` (analysis: EHP/DPS/TTK/Power, duels,
  round-robin, presence-winrate, power-curve). RNG contract documented in
  `BattleSimulator` (growth → per-unit timing jitter → per-hit crit/dodge/variance →
  hard-resolve flip).
- **MTA.Data** — ScriptableObject `MonsterSpecies` (.asset per monster) + `SpeciesDatabase`
  (loads Resources → Core registry). Fields incl. `element`, `evolvesTo`,
  `evolveLevel`, `evolutionOnly`.
- **MTA.Meta** — game brain (no UnityEngine scene deps): `GameController`/`GameFlow`
  (phase state machine), `GameSession`, `MatchRunner`, `SaveData`+`SaveSystem`
  (JSON to `persistentDataPath/save.json`, atomic, backward-compatible),
  `Progression` (XP/level/train/evolve/unlock rules), `Career`, `DailyRewards`,
  `BattlePlayback`, `ReplayBuilder`+`BattleCinematicDirector` (deterministic
  choreography seeded by `logHash`), `BattleDrama`, `MonsterMeta`, `SpeciesIdentity`.
- **MTA.Battle** — the animated battle view + presentation: `BattleReplayView`
  (the big one), `UnitView` (per-monster: real sprite + flash + HP + `combatOffset`
  choreography), `MonsterVisual` (loads CC0 sprites), `MonsterArt`/`ProceduralArt`
  (procedural fallback shapes), `BattleArena` (element-themed + real backdrop),
  `Vfx` (pooled grid-spritesheet player), `AudioManager`+`SfxLibrary`+`MusicLibrary`.
- **MTA.App** — `GameBootstrap` (builds all screens), `UIFactory` (buttons/panels/
  sliders/badges + Kenney 9-slice sprites), `ButtonPunch`. **MTA.App.Editor** /
  **MTA.EditorTools** — `AndroidBuilder`, `BalanceAuditRunner`, `ExternalArtImporter`.
- **MTA.Tests** (EditMode, 66 tests) + **MTA.PlayTests** (PlayMode UI smoke).

## 5. Combat & balance (the "K" rework — this is the important design)
Original combat: SPD dominated (a +5 SPD edge won ~100% of duels), power curve was a
deterministic cliff, tanks/supports non-viable. Fixed by the parity framework:
- **Linear-through-origin APS** so ATK-heavy ≈ SPD-heavy equal budgets (equal DPS).
- **Controlled variance**: ±30% damage variance, **LUCK-based dodge**, ±45% initiative
  jitter — kills the deterministic cliff, rehabilitates LUCK.
- **DEF/HP EHP-parity** via `k=60`; global `damageScale=0.62` for pacing.
- **Value-balanced species** (each ~equal effective Power, distinct role shape);
  tanks/supports got real damage actives (kit reassignment).
- **Symmetric elemental triangle** Fire→Nature→Water→Fire (`elementAdvantage=0.04`).
Current `balance.json`: `k=60, damageScale=0.62, damageVariance=0.30, dodgeBase=0.06,
dodgePerLuck=0.004, dodgeCap=0.28, timingJitter=0.45, elementAdvantage=0.04,
apsPerSpd 0.015 (no kink), apsCap=1.15, crit 0.005/LUCK cap 0.30 ×1.5`.
**Validated (20k battles, level 5)**: all species **42.4–55.7%**, team-A **~50%**,
duration P50 **~30 s**, element aggregate ~50% each, power-diff <10% → 45–55%,
no slight edge → 90%+. See `reports/BALANCE_VALIDATION.md`, `ATTRIBUTE_VALUE_ANALYSIS.md`,
`ELEMENT_SYSTEM.md`, `PHASE_K_FINAL.md`. **Do not touch balance unless the task is a
balance task**; if you do, re-run `RunValidation` and keep the band.

## 6. Content
- **21 species** = 18 obtainable base + 3 evolution-only (`dire_wolf`, `inferno_drake`,
  `blade_mantis`). Roles: Tank/Bruiser/Assassin/Mage/Support. Elements: Fire/Water/Nature
  (4-ish each). Kits from a shared skill pool (ATK line strike/power_strike/savage_rend,
  INT line zap/spark_burst/mind_blast, utility mend/rally/war_cry/slow_hex).
- **Evolution**: `Progression.Evolve` transforms an owned monster in place at
  `evolveLevel` (keeps level/xp); evolution-only forms excluded from wild/unlock/career
  pools (and from balance validation).
- **Career**: 18 stages / 6 leagues (Bronze…Legend), difficulty scales enemy level
  5→22, first-clear rewards, frontier-gated. **Daily**: 7-day streak, anti-cheat
  (clock-rollback blocked). **Collection/dex**, **training** (coin sink), **leveling**.

## 7. Presentation (current state)
- **Real monster sprites** (CC0) — Pokémon-style front(enemy)/back(player); evolutions
  use the base's alt-color palette. Procedural `MonsterArt` is only a missing-sprite
  fallback. Used in battle, collection, detail, result MVP, team-select.
- **Fight choreography** (latest, `27c52b5`) — melee exchanges play **dash-in →
  ground combo → launcher (into air) → air combo → slam (to ground) → recovery**,
  plus **dodge → counter**. Driven by `UnitView.combatOffset` transform animation +
  `MoveOffset` coroutine, sim clock frozen (hit-stop) so it never races the timeline.
  Ranged attackers use a compact hit sequence. 1v1 framing: only the active fighter
  visible, reserves off-screen, next runs in on death.
- **Real VFX** (CC0, CodeManu) — hit sparks / big-hit / explosion / speed-lines / fire
  / electric / puff, played via pooled grid-spritesheet `VfxPlayer` on crit/ult/death/
  dodge/element.
- **Real audio** (CC0) — battle music (CleytonRX) + creature SFX (rubberduck) override
  the synthesized fallback; procedural music/SFX synth for the rest; Music/SFX/UI
  volume buses persisted in **PlayerPrefs** (NOT save.json); dynamic battle intensity.
- **Arena** — CC0 forest panorama backdrop (MatiasVME), element-tinted, + procedural
  particles/ground.
- **UI** — Kenney CC0 9-slice buttons + rarity frames; page/panel transitions,
  button-press punch, popup scale-in, element badges, round-pip battle HUD, result
  reveal (MVP portrait, Combo King, rewards).
- Camera: attack/combo/crit shake + zoom-punch, ultimate cinematic zoom, finisher
  slow-mo, victory zoom, screen flash + shockwave.

## 8. External assets (all CC0 unless noted) — `Assets/ExternalArt/`
| Use | Pack | Author | License |
|-----|------|--------|---------|
| Monsters | 50+ Monsters Pack 2D (`Resources/MonSprites`) | isaiah658 | CC0 |
| VFX | Free VFX Asset Pack (`Resources/Vfx`) | CodeManu | CC0 |
| Battle music | Battle RPG Theme (`Resources/Audio/music_battle`) | CleytonRX/Kauffman | CC0 |
| Creature SFX | 80 CC0 creature SFX (`Resources/Audio/sfx_*`) | rubberduck | CC0 |
| Arena bg | Parallax Forest (`Resources/Arena/forest`) | MatiasVME | CC0 |
| UI | Kenney UI Pack 2.0 (`Resources/Ui`) | Kenney | CC0 |
| (unused) | Animated Fox | IDoTheDrawing | CC-BY (dropped) |
Full sourcing + URLs in `reports/ASSET_SOURCING.md`.

## 9. Verification snapshot
- **EditMode: 66/66 pass** (incl. determinism self-ref, mirror ~50%, HP↔DEF parity,
  element triangle, dodge, evolution, career, daily, choreography director).
- **PlayMode UI smoke: PASS** (boots + all screens + a battle, 0 runtime errors).
- APK builds (~75 MB). Installs + launches on Samsung SM-S731B (Android 16/arm64),
  cold start ~0.5 s, no crash (only benign AssetPackManager ClassNotFound noise).

## 10. Known gaps / possible next steps
- Sprites are **single-frame**; movement is transform-based (paper-fighter), not
  frame-by-frame limb animation (no free CC0 anime sheets for 21 unique species).
- Arena is one forest panorama tinted per element (not 3 distinct hand-made arenas).
- UI is functional-but-generic (Kenney kit); a bespoke art pass would lift fidelity.
- Store-readiness: currently **debug-signed dev APK** — needs a real upload keystore +
  AAB for Play Store; procedural app icon (replace with store art).
- On-device visual QA is the persistent human-only gap (headless can't verify pixels;
  device drops off adb frequently).

## 11. Key files
`Core/BattleSimulator.cs`, `Core/StatMath.cs`, `Core/SkillResolver.cs`,
`Core/BalanceConfig.cs`, `Core/BalanceLab.cs`; `Meta/SaveData.cs` (Progression+Evolve),
`Meta/Career.cs`, `Meta/DailyRewards.cs`, `Meta/BattleCinematicDirector.cs`;
`Battle/BattleReplayView.cs` (choreography), `Battle/UnitView.cs`, `Battle/Vfx.cs`,
`Battle/AudioManager.cs`, `Battle/BattleArena.cs`, `Battle/MonsterVisual.cs`;
`App/GameBootstrap.cs`, `App/UIFactory.cs`; `App/Editor/AndroidBuilder.cs`,
`App/Editor/BalanceAuditRunner.cs`, `App/Editor/ExternalArtImporter.cs`.
Balance: `Assets/Resources/balance.json` (+ mirror in `StreamingAssets`).

## 12. Reports index (in `reports/`)
Phase docs: D_AUDIO, E_COLLECTION, F_LEVELING, G_CAREER, H_RETENTION, I_RELEASE,
J_CINEMATIC_REPLAY, K_FINAL, L_VISUALS, M_AUDIO, N_COMBAT_FEEL, N_FIGHT_CHOREOGRAPHY.
Balance: ATTRIBUTE_VALUE_ANALYSIS, BALANCE_VALIDATION, ELEMENT_SYSTEM, CONTENT_EXPANSION.
Assets/UI: ASSET_SOURCING, UI_REDESIGN, UI_SMOKE_VALIDATION, PRODUCTION_PRESENTATION_AUDIT.
Status: MVP_STATUS, FINAL_MVP_AUDIT, DEVICE_VALIDATION.

## 13. Commit history (recent, newest first)
27c52b5 fight choreography (dash/launcher/air/slam) · 0dc4d51 Kenney UI sprites ·
cf74bd1 real VFX + arena backdrop · cb5d2df real monster sprites + audio ·
a8559b6 production visuals + audio · efe777b UI smoke + grid fixes ·
0381063 species + career + evolution · ff34b9f finalize Phase K ·
af8dd11 rebalance parity + elements · 7934a59 cinematic replay · … through the MVP
(Phases D–I) and Phase-1 balance work.
