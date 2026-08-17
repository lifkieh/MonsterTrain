#if UNITY_EDITOR
using System.IO;
using MTA.App;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MTA.App.EditorTools
{
    // Builds the single first-playable scene (one GameBootstrap object) and the
    // Windows player entirely from script — no manual editor work. Invokable via
    // menu or headless -executeMethod.
    public static class FirstPlayableSceneBuilder
    {
        const string SceneDir = "Assets/Scenes";
        const string ScenePath = SceneDir + "/FirstPlayable.unity";

        [MenuItem("MTA/Build First Playable Scene")]
        public static void BuildScene()
        {
            Directory.CreateDirectory(SceneDir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            new GameObject("GameBootstrap", typeof(GameBootstrap));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("MTA: built + registered " + ScenePath);
        }

        [MenuItem("MTA/Build Windows Player")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath)) BuildScene();
            string outDir = "Build/Windows";
            Directory.CreateDirectory(outDir);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outDir + "/TrainYourMonster.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("MTA: Windows build result = " + report.summary.result +
                      ", size = " + report.summary.totalSize + " bytes");
        }
    }
}
#endif
