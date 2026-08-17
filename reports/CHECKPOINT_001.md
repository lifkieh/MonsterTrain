# CHECKPOINT 001 — Phase 1 Project Compiles

Date: 2026-08-17 · Commit: `8f1eb22` "Phase 1 project compiles successfully"
(114 files, working tree clean).

## What was achieved

First compiling Unity project for the Phase 1 battle prototype. The Phase 1
scripts drop was imported, made to compile on Unity 6, wired into 4 assembly
definitions, and its EditMode test suite was made discoverable and run headless.
P0-1 (Unity project setup + script import) is complete.

Fixes made to reach zero errors:

- **Test framework setup** — the bare project manifest had no test framework;
  added `com.unity.test-framework` 1.7.0 (bundled version).
- **Assembly definition fix** — `MTA.Tests.asmdef` used the pre-Unity-6
  `optionalUnityReferences: ["TestAssemblies"]` flag (removed in Unity 6);
  rewrote it to reference `UnityEngine.TestRunner` + `UnityEditor.TestRunner`
  with the `nunit.framework.dll` precompiled reference.
- Core / Data / EditorTools compiled clean on the first attempt — only the test
  assembly was broken.

## Unity version

**6000.5.8f1 (Unity 6.5).** Note: the design docs assumed 2021.3 LTS; the
scripts run unmodified on Unity 6.5. The only version-specific change needed was
the test-assembly definition above.

## Scripts compiled

**23 C# scripts, 0 errors, 0 warnings**, across 4 assemblies:

- `MTA.Core` — 17 (pure C#, no engine scene deps)
- `MTA.Data` — 4 (ScriptableObjects; references Core)
- `MTA.EditorTools` — 1 (Editor-only; `MTA → Generate Phase 1 Content` menu)
- `MTA.Tests` — 1 (EditMode; references Core + NUnit)

Verification method: headless `-batchmode -runTests -testPlatform EditMode`.
`MTA` menu registration and `MTA.Tests` discovery both confirmed (the menu
assembly compiled clean; the test assembly was discovered and executed).

## Tests passing (6 / 7)

- `StatMath_MatchesSpecFormulas`
- `Training_RoutesThroughGrowthGrade`
- `Determinism_SameSeedSameHash_100Runs`
- `AllTeamSizes_Terminate_Within_HardResolve`
- `ThirteenthSpecies_FromPureData_ZeroCode`
- `PreparationSignal_TrainedBeatsUntrained`

## Tests failing (1 / 7)

- **`MirrorComps_NoSideBias`** — mirror-comp win rate **73.5% for team A**
  (expected 42–58%). Sweep also shows battles far too fast for the 30–90 s
  target: **P10 12.5 s / P50 15.6 s / P90 19.5 s**, with **87 / 400** battles
  under 15 s and 0% hard-resolves. Two coupled signals: (a) a systematic
  first-actor / side bias in mirror matches, and (b) time-to-kill is way below
  the design window. This is balance + possibly action-timeline tie-break
  fairness — a P1-1 / Stage-3 (balance) concern, **not** a compile defect.

## Known issues

1. **Mirror side-bias + battles too fast** (the failing test, above). Needs
   `balance.json` tuning and an investigation of the timeline tie-break
   (team A ordered before team B) at very low TTK. Deferred to P1-1.
2. **OneDrive deleted the whole project folder to the Recycle Bin** at 05:56:37
   during/after the Unity import, then it was restored intact from the bin
   (`.git`, all 23 scripts, edits, ProjectSettings, Library all present; commit
   verified clean). **Root cause:** a Unity project living inside a OneDrive-
   synced `Desktop` folder — OneDrive fights Unity's large, constantly-changing
   `Library/`. **Strong recommendation:** relocate the project OUT of OneDrive
   (e.g. `C:\Dev\TrainYourMonster`) before further Unity work, or exclude the
   project folder from OneDrive sync. Until then, expect recurrence. (Also note
   the standing "C: chronic low disk" issue — a large `Library/` adds pressure.)
3. **Version drift** — docs say Unity 2021.3 LTS; actual is Unity 6.5. Harmless
   so far; if the docs are treated as build spec, update the target version.
4. `Library/`, `Logs/`, `*.csproj`, `*.sln`, `UserSettings/` are git-ignored
   (regenerable); `ProjectSettings/` and `Packages/*.json` are committed for
   reproducibility.

## Next milestone

**P1-1 — gate tests green.** Get `MirrorComps_NoSideBias` passing by tuning
`balance.json` (slow TTK into the 30–90 s window) and confirming timeline
tie-break fairness in mirror comps, then re-run the full EditMode suite to 7/7.
That unblocks Stage 2 (Balance Sweep window) and Stage 3 (the duration /
fairness / preparation sweep that verifies Phase 1 success criteria 2–4).

**Recommended pre-work before P1-1:** resolve the OneDrive relocation (Known
Issue 2) so the workspace stops disappearing mid-run.
