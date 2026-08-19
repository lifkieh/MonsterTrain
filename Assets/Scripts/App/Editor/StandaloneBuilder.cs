#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MTA.App.EditorTools
{
    // Windows standalone build — for developer visual verification of the battle
    // presentation without a device (run the .exe windowed, screenshot it). Not shipped.
    // Editor-only tooling; touches no gameplay/sim/save/balance.
    public static class StandaloneBuilder
    {
        const string ScenePath = "Assets/Scenes/FirstPlayable.unity";
        const string ExePath = "Build/Windows/TrainYourMonster.exe";

        [MenuItem("MTA/Build Windows (Dev Verify)")]
        public static void BuildWindows()
        {
            PlayerSettings.productName = "Train Your Monster";
            PlayerSettings.companyName = "TrainYourMonster";
            // Windowed portrait-ish default so a screenshot matches the phone framing.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 1170;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

            Directory.CreateDirectory("Build/Windows");
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ExePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("MTA: Windows build = " + report.summary.result + ", exe=" + ExePath);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
#endif
