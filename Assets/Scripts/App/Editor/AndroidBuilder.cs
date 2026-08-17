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

            // Development APK: no custom keystore -> Unity signs with the Android
            // debug keystore automatically. Fine for sideloading, not for Play Store.
            PlayerSettings.Android.useCustomKeystore = false;

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("MTA: Android settings configured (portrait, IL2CPP/ARM64, minSdk 24).");
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
    }
}
#endif
