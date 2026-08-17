# P0-1 — Unity Project Setup & Script Import (Runbook)

Status after this prep session:

- **Done for you (machine-independent):** scripts imported to repo `Assets/`,
  4 assembly definitions authored, `.gitignore` added, `git init` run (no commit).
- **Blocked on this machine:** Unity is **not installed** (no Unity Hub, no
  Editor, not on PATH). The Editor-side steps below must run on a machine with
  Unity 2021.3 LTS+.

## What is already in the repo

```
TrainYourMonster/            <- open THIS folder as the Unity project
  Assets/
    Scripts/
      Core/  + MTA.Core.asmdef          (pure C#, no engine scene deps)
      Data/  + MTA.Data.asmdef          (refs MTA.Core)
      Editor/+ MTA.EditorTools.asmdef   (Editor-only; refs Core + Data)
      Tests/ + MTA.Tests.asmdef         (EditMode; refs Core + NUnit)
    StreamingAssets/balance.json
    README.md
  .gitignore
```

Assembly graph (verified from the source): `Core` depends on nothing; `Data`
and `Editor` depend on `Core`; `Tests` depends only on `Core` + NUnit. This is
why edit-mode tests can run headless.

## Your steps (need Unity)

1. **Install/confirm Unity 2021.3 LTS+** via Unity Hub, **with the Android Build
   Support module** (needed later at P3-1; harmless now).
2. **Unity Hub → Add → Add project from disk →** select this repo folder
   (`TrainYourMonster`). Open with 2021.3 LTS+. Unity will generate `Packages/`
   and `ProjectSettings/` on first open (expected; they are git-ignored build-
   adjacent except ProjectSettings — see note).
3. **Wait for the first import/compile.** Watch the Console.
   - **Acceptance (P0-1):** **zero compile errors.**
   - The `.cs.meta`/`.asmdef.meta` files Unity creates are normal — keep them.
4. **Verify the test assembly:** Window → General → Test Runner → **EditMode**.
   You should see `MTA.Tests` with 8 tests. *(If it does not appear: the hand-
   authored `MTA.Tests.asmdef` uses the classic `optionalUnityReferences:
   ["TestAssemblies"]` flag — the one part I could not validate without Unity.
   Fix: in Test Runner click "Create EditMode Test Assembly Folder", then either
   copy its generated asmdef settings onto `MTA.Tests.asmdef`, or move
   `Phase1GateTests.cs` under the generated folder.)*
5. **Generate content (P0-2, do right after):** menu **MTA → Generate Phase 1
   Content** → creates 10 skill assets + 12 species assets under
   `Assets/Resources/{Skills,Monsters}`.
6. **Run the gate tests (P1-1):** Test Runner → EditMode → Run All → expect 8/8
   green. Capture the result into `/reports`.

## Notes / gotchas

- **`Core` is pure C#** by design (no `using UnityEngine`). Its asmdef keeps
  default engine references (harmless) but the code has no scene/MonoBehaviour
  deps, so it runs in the edit-mode test assembly.
- **`SpeciesAssetGenerator`** is Editor-only and writes to `Assets/Resources/…`;
  the `Resources/` folder is created on first generate.
- **ProjectSettings/**: once Unity generates it, decide whether to commit it
  (recommended for a reproducible project) — the current `.gitignore` does not
  exclude `ProjectSettings/`, only `Library/`, `Temp/`, `obj/`, builds, IDE
  files. Adjust if you prefer otherwise.
- **git**: initialized, nothing committed. Commit when you want a baseline
  (ask and I will, or run it yourself).

## Definition of done (P0-1)

Unity opens the repo as a project, compiles with **zero errors**, and the
`MTA.Tests` EditMode assembly is discovered in the Test Runner. Then proceed to
P0-2 → P1-1.
