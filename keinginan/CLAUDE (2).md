# Train Your Monster (MTA) — rules for Claude Code

Unity 6000.5.8f1 · Android portrait · one scene (`FirstPlayable.unity`), ALL UI built in
code via `GameBootstrap`/`UIFactory` (no prefab wiring). Full context: `PROJECT_HANDOFF.md`.
Active work: `FIGHT_FEEL_SPEC.md` — execute ONE phase per session, only the phase the user
names, then stop.

## Hard rules (never break)
- Git author on every commit: `Lifkie Lie <llifkie@gmail.com>`.
- NEVER add Claude/OpenAI/AI co-author or attribution trailers.
  Verify: `git log -1 --format=%b` must contain no "Co-Authored".
- Commit + push to `origin/master` after each meaningful unit of work.
- The Android APK must still build after every change.
- Never remove existing functionality or delete/weaken existing tests (66 EditMode + PlayMode smoke).

## Layer boundaries
- **Gameplay layer — DO NOT TOUCH** unless the task is explicitly a balance/gameplay task:
  `Assets/Scripts/Core/**` (BattleSimulator, StatMath, SkillResolver, BalanceConfig, BalanceLab),
  `Meta/SaveData.cs`, `SaveSystem`, `Progression`, `Career`, `DailyRewards`,
  `Assets/Resources/balance.json` + its StreamingAssets mirror,
  stat/growth/skill values inside `MonsterSpecies` `.asset` files,
  and every `speciesId` string (save keys — immutable forever).
- **Presentation layer — free to change:** `Battle/**`, `App/**`, and the Meta choreography
  classes (`BattleCinematicDirector`, `BattleDrama`, `ReplayBuilder`, `BattlePlayback`) —
  timing/visuals only, never outcomes. Meta stays free of UnityEngine scene deps.
- `.asset` sprite references and `displayName` are presentation — editable.
- Determinism: same seed → same `logHash` must stay green. All visual randomness comes from
  the director RNG seeded by `logHash`, never `UnityEngine.Random` in battle code.
- If a balance task is ever approved: `balance.json` is the single tuning source, keep the
  C# `BalanceConfig` defaults in sync, re-run `BalanceAuditRunner.RunValidation`, keep all
  species in the 42–56% band.

## Verification commands (PowerShell)
- EditMode tests:
  `& "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -batchmode -projectPath E:/TrainYourMonster -runTests -testPlatform EditMode -testResults out_edit.xml -logFile out_edit.log`
- PlayMode smoke: same command with `-testPlatform PlayMode`.
- APK build (success line `MTA: Android build = Succeeded`, output `Build/Android/TrainYourMonster.apk`):
  `& "...\Unity.exe" -batchmode -quit -projectPath E:/TrainYourMonster -executeMethod MTA.App.EditorTools.AndroidBuilder.BuildApk -logFile b.log`
- Launch long builds detached (`Start-Process -WindowStyle Hidden`) so they survive session
  teardown. adb on this machine is flaky — device install is optional, never blocking.
- Art import: `-executeMethod MTA.App.EditorTools.ExternalArtImporter.ImportAll`.

## Conventions
- No per-frame allocations in battle code: pool VFX/ghosts/numbers, cache `WaitForSeconds`,
  no LINQ in hot paths. Low-end Android is the target.
- Portrait-first; sanity-check layouts at 1080×2340 and 720×1520.
- UI must always show `displayName` (Title Case), never raw snake_case ids.
- New assets: CC0 preferred; verify the license file in every download; log every pack in
  `reports/ASSET_SOURCING.md` (name, author, license, URL); flag anything unverified.
- Write `reports/PHASE_*.md` after every phase: changes, files touched, chosen parameter
  values, test/build results, and a Human QA checklist for on-device verification.
- Prefer explicit, boring code — one solo dev maintains this months later.
