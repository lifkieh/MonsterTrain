# Phase Z — Store Readiness

Date: 2026-08-19 · Author: Lifkie Lie

Prepares Train Your Monster for a Google Play release. Code changes are in
`AndroidBuilder.cs`; the rest are drafts/checklists under `docs/`.

## Release configuration (done in code)
- **Version:** `bundleVersion 1.0.0`, `versionCode 1` (`AndroidBuilder.ReleaseVersion`).
- **Install location:** internal (`preferredInstallLocation = Auto`) — was `preferExternal`.
- **IL2CPP + ARM64** (64-bit compliant), portrait, `com.trainyourmonster.game` — unchanged.

## AAB pipeline (done in code)
New `MTA/Build Android AAB (Release)` menu item + `AndroidBuilder.BuildAab`:
- Sets `EditorUserBuildSettings.buildAppBundle = true` → outputs
  `Build/Android/TrainYourMonster.aab` (Google Play requires `.aab`).
- Builds with **`BuildOptions.None`** (no `Development`/`debuggable` flag) — a real release build.
- Applies a **release keystore from environment variables** (see below); if none is set it
  loudly warns and falls back to debug signing (which Play will reject) so the pipeline
  still runs for testing.
- Invoke headless:
  `Unity.exe -batchmode -quit -projectPath . -executeMethod MTA.App.EditorTools.AndroidBuilder.BuildAab -logFile aab.log`

### Release signing (do once, keep the keystore OUTSIDE the repo)
```
keytool -genkeypair -v -keystore tym-release.jks -alias tym \
  -keyalg RSA -keysize 2048 -validity 10000
# then set before building the AAB:
setx MTA_KEYSTORE_PATH  C:\keys\tym-release.jks
setx MTA_KEYSTORE_PASS  <store-pass>
setx MTA_KEY_ALIAS      tym
setx MTA_KEY_PASS       <key-pass>
```
Enroll in **Play App Signing** (Google holds the app-signing key; you keep the upload key).

## Store deliverables (drafts written)
- `docs/PRIVACY_POLICY.md` — privacy policy template (host it, paste URL in Console).
- `docs/DATA_SAFETY.md` — Data Safety answers. **Recommendation:** the code never calls
  Unity Analytics or IAP, so removing `com.unity.purchasing` + `com.unity.analytics` from
  `Packages/manifest.json` lets you declare **"No data collected"** and drops the BILLING
  permission. (Left as a decision — not removed automatically to keep the build stable.)
- `docs/CONTENT_RATING.md` — IARC questionnaire draft → expected **Everyone / PEGI 3**.
- `docs/STORE_LISTING.md` — app name, short/full description, category/tags, and
  **screenshot + feature-graphic + hi-res-icon checklists**.
- `reports/RELEASE_NOTES.md` — v1.0.0 notes + how to generate future ones.

## Remaining release blockers (owner action, outside code)
1. **Create the release keystore** and enroll in Play App Signing (commands above).
2. **Host the privacy policy** and add the URL to the listing.
3. **Complete Data Safety + content rating** in Play Console (drafts provided).
4. **Produce store assets:** hi-res 512×512 icon (replace the procedural placeholder),
   1024×500 feature graphic, ≥2 phone screenshots (checklist provided).
5. **Decide on Analytics/IAP** packages (recommend removing for a clean "no data" release).
6. Optional: turn off the Unity splash logo (Unity 6 allows it for free).

## Verified
- `AndroidBuilder` compiles; the dev APK path (`BuildApk`) is unchanged and still succeeds
  (see final validation). The AAB path is new code, ready to run once a keystore is set.
