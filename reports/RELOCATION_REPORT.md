# RELOCATION REPORT — Train Your Monster

Date: 2026-08-17. Task: relocate the repository off the OneDrive-synced Desktop
(where it had already been auto-moved to the Recycle Bin once) onto a local,
non-synced drive. **Relocation only — no gameplay, balance, architecture, or
design changes.**

## Paths

- **Old path:** `C:\Users\user\OneDrive\Desktop\TrainYourMonster` (OneDrive-synced)
- **New path:** `E:\TrainYourMonster` (local, not synced)

E: had 552 GB free; project size ≈ 1.2 GB (almost entirely the regenerable
Unity `Library/`).

## Method

1. `robocopy` copied the full tree C: → E: (`/E /XJ /COPY:DAT /DCOPY:DAT /MT:16`;
   `/XJ` to skip junction points in `Library/PackageCache`).
2. Verified the E: copy (git integrity + Unity structure + exact file-count
   parity) **before** removing the source.
3. Sent the old C: folder to the **Recycle Bin** (not a permanent delete), so a
   rollback copy exists.

## Files moved

- **16,212 files** across **1,386 directories**, **1.105 GB** — robocopy summary
  reported **0 failed, 0 mismatch, 0 skipped**.
- File-count parity check: **C: 16,212 = E: 16,212**.
- Included: `.git/` (full), `Assets/`, `Packages/`, `ProjectSettings/`,
  `UserSettings/`, `Library/`, `docs/`, `reports/`, `archive/`, `CLAUDE.md`,
  `.gitignore`, and Unity-generated `.sln`/`.csproj`/`.vsconfig`.

## Git verification (at E:)

- `git rev-parse --show-toplevel` → `E:/TrainYourMonster` ✓
- Work tree recognized (`--is-inside-work-tree` = true) ✓
- **Commit `8f1eb22` present** (`cat-file -t` → commit) and is `HEAD` ✓
- Full history intact — `git log --oneline --all`: `8f1eb22 Phase 1 project
  compiles successfully` (the only commit) ✓
- Branch intact: `master` ✓
- `git fsck --full` → clean (no errors/dangling reported) ✓
- `git status` → **tracked tree clean**; sole untracked entry is
  `reports/CHECKPOINT_001.md` (intentionally uncommitted from Checkpoint 001;
  this report adds a second untracked file). No tracked-file modifications from
  the move.

## Unity verification (at E:)

- Present: `Assets/`, `Packages/`, `ProjectSettings/`, `UserSettings/`,
  `Library/`, plus repo `docs/`, `reports/`, `archive/`, `.git/`. ✓
- **23 C# scripts** across `Core`/`Data`/`Editor`/`Tests`. ✓
- Key files: `Packages/manifest.json`, `Assets/StreamingAssets/balance.json`,
  `docs/PROJECT_KNOWLEDGE.md`, `CLAUDE.md`. ✓
- `ProjectSettings/ProjectVersion.txt` → **6000.5.8f1** (unchanged). ✓
- The project is location-independent (Unity uses project-relative paths; git
  uses relative internals), so it opens at the new root with no edits.

## Path references

- **`CLAUDE.md` uses only relative paths** (`docs/…`, "repo root") — it points at
  the correct project root at any location. **No edit required** (per the
  "update path refs only if necessary" constraint).
- No other tracked file hardcodes the old absolute path. Historical reports
  (`P0-1_SETUP.md`, `CHECKPOINT_001.md`) that mention the old C: path are
  point-in-time records and were left unchanged; this report is the authoritative
  record of the new location.

## Issues encountered

1. **Old-source deletion API** — the first Recycle-Bin call used
   `UIOption.DoNothing`, which doesn't exist in this .NET's enum; re-ran with
   `OnlyErrorDialogs`. No impact; source went to the bin on the retry.
2. **Rollback copy** — the old folder is in the Recycle Bin (`orig:
   C:\Users\user\OneDrive\Desktop`, deleted 06:46:28). Recoverable if needed;
   empty the bin once you're satisfied with E:.
3. **Root cause (context, from Checkpoint 001):** the earlier unexpected move to
   the Recycle Bin (05:56) happened because a Unity project with a large,
   churning `Library/` lived inside a OneDrive-synced Desktop folder. Moving to
   `E:\TrainYourMonster` (non-synced) removes that failure mode.

## Result

Repository fully relocated to `E:\TrainYourMonster`, git history and commit
`8f1eb22` intact, Unity project structure intact, working tree clean (only the
two report files untracked). No design/code/doc changes beyond this report.
