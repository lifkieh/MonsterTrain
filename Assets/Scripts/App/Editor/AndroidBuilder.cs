#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace MTA.App.EditorTools
{
    // Configures Android player settings (portrait, package id, SDK, backend) and
    // builds the APK. Requires the Android Build Support module (+ SDK/NDK/JDK)
    // installed via Unity Hub; without it, SwitchActiveBuildTarget/BuildPlayer
    // throw and this reports the blocker.
    public static class AndroidBuilder
    {
        const string ScenePath = "Assets/Scenes/FirstPlayable.unity";
        const string ApkPath = "Build/Android/TrainYourMonster.apk";
        const string AabPath = "Build/Android/TrainYourMonster.aab";

        [MenuItem("MTA/Configure Android Settings")]
        public static void Configure()
        {
            PlayerSettings.productName = "Train Your Monster";
            PlayerSettings.companyName = "TrainYourMonster";

            // Portrait only.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Identity + API levels.
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.trainyourmonster.game");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;   // Android 7.0
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            // IL2CPP + ARM64: runs on all modern devices and is Play-Store-ready.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // Install to internal storage (Play-recommended; avoids SD unmount issues).
            PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;

            // Development APK: no custom keystore -> Unity signs with the Android
            // debug keystore automatically. Fine for sideloading, not for Play Store.
            PlayerSettings.Android.useCustomKeystore = false;

            // Release identity: version name + code.
            PlayerSettings.bundleVersion = ReleaseVersion;
            PlayerSettings.Android.bundleVersionCode = ReleaseVersionCode;

            ApplyBranding();

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("MTA: Android settings configured (portrait, IL2CPP/ARM64, minSdk 24, v" + ReleaseVersion + ").");
        }

        public const string ReleaseVersion = "1.0.0";
        public const int ReleaseVersionCode = 1;

        // App icon + splash background. Wrapped so a branding hiccup never fails the
        // build; the icon is generated procedurally (no art assets required).
        static void ApplyBranding()
        {
            try
            {
                var icon = MakeIcon(512);
                var sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Android, IconKind.Application);
                if (sizes != null && sizes.Length > 0)
                {
                    var icons = new Texture2D[sizes.Length];
                    for (int i = 0; i < icons.Length; i++) icons[i] = icon;
                    PlayerSettings.SetIcons(NamedBuildTarget.Android, icons, IconKind.Application);
                }
            }
            catch (System.Exception e) { Debug.LogWarning("MTA: icon branding skipped — " + e.Message); }

            try
            {
                PlayerSettings.SplashScreen.show = true;
                PlayerSettings.SplashScreen.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
            }
            catch (System.Exception e) { Debug.LogWarning("MTA: splash branding skipped — " + e.Message); }
        }

        // A simple, recognizable monster mark: gradient field + glowing body orb + eyes.
        static Texture2D MakeIcon(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            var top = new Color(0.16f, 0.55f, 0.95f);
            var bot = new Color(0.45f, 0.2f, 0.75f);
            var body = new Color(0.98f, 0.85f, 0.35f);
            float cx = size * 0.5f, cy = size * 0.46f, rBody = size * 0.30f;
            float eyeR = size * 0.05f, eyeDx = size * 0.11f, eyeDy = size * 0.04f;
            for (int y = 0; y < size; y++)
            {
                float t = (float)y / (size - 1);
                var bg = Color.Lerp(bot, top, t);
                for (int x = 0; x < size; x++)
                {
                    var c = bg;
                    float db = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (db < rBody) c = Color.Lerp(body, bg, Mathf.SmoothStep(0.75f, 1f, db / rBody));
                    // eyes
                    float e1 = Mathf.Sqrt((x - (cx - eyeDx)) * (x - (cx - eyeDx)) + (y - (cy + eyeDy)) * (y - (cy + eyeDy)));
                    float e2 = Mathf.Sqrt((x - (cx + eyeDx)) * (x - (cx + eyeDx)) + (y - (cy + eyeDy)) * (y - (cy + eyeDy)));
                    if (e1 < eyeR || e2 < eyeR) c = new Color(0.1f, 0.1f, 0.14f);
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }

        [MenuItem("MTA/Build Android APK")]
        public static void BuildApk()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("MTA: BLOCKER — Android Build Support module is not installed. " +
                    "Install it via Unity Hub (Editor 6000.5.8f1 -> Add Modules -> " +
                    "Android Build Support + Android SDK & NDK Tools + OpenJDK), then re-run.");
                EditorApplication.Exit(2);
                return;
            }

            Configure();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            Directory.CreateDirectory("Build/Android");
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development   // signed dev APK (debug keystore)
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("MTA: Android build = " + report.summary.result +
                      ", size=" + report.summary.totalSize + " bytes, apk=" + ApkPath);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }

        // Release AAB pipeline (Google Play requires an .aab). No Development flag, app-bundle
        // output, internal install, and release signing from env vars if provided. Invoke:
        //   -executeMethod MTA.App.EditorTools.AndroidBuilder.BuildAab
        [MenuItem("MTA/Build Android AAB (Release)")]
        public static void BuildAab()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("MTA: BLOCKER — Android Build Support module is not installed.");
                EditorApplication.Exit(2);
                return;
            }
            ConfigureRelease();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.buildAppBundle = true;   // → .aab

            Directory.CreateDirectory("Build/Android");
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = AabPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None      // RELEASE — no Development/debuggable flag
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("MTA: Android AAB = " + report.summary.result +
                      ", size=" + report.summary.totalSize + " bytes, aab=" + AabPath);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }

        // Release configuration = Configure() + release signing. Falls back to the debug
        // keystore with a loud warning if no release keystore env vars are provided.
        static void ConfigureRelease()
        {
            Configure();
            string ks = System.Environment.GetEnvironmentVariable("MTA_KEYSTORE_PATH");
            if (string.IsNullOrEmpty(ks) || !File.Exists(ks))
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.LogWarning("MTA: NO RELEASE KEYSTORE — set MTA_KEYSTORE_PATH / MTA_KEYSTORE_PASS / " +
                    "MTA_KEY_ALIAS / MTA_KEY_PASS. The AAB will be DEBUG-signed and CANNOT be uploaded to " +
                    "Google Play. Create one with keytool (see reports/PHASE_Z_STORE_READY.md) and keep it OUTSIDE the repo.");
                return;
            }
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = ks;
            PlayerSettings.Android.keystorePass = System.Environment.GetEnvironmentVariable("MTA_KEYSTORE_PASS");
            PlayerSettings.Android.keyaliasName = System.Environment.GetEnvironmentVariable("MTA_KEY_ALIAS");
            PlayerSettings.Android.keyaliasPass = System.Environment.GetEnvironmentVariable("MTA_KEY_PASS");
            Debug.Log("MTA: release keystore applied from environment.");
        }
    }
}
#endif
