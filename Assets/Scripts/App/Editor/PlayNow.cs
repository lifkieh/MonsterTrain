#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MTA.App.EditorTools
{
    // Opens the playable scene, brings the Game view forward, and enters Play mode.
    // Invoke on editor launch (GUI, no -batchmode / no -quit):
    //   Unity.exe -projectPath E:/TrainYourMonster -executeMethod MTA.App.EditorTools.PlayNow.Go
    public static class PlayNow
    {
        public static void Go()
        {
            EditorApplication.delayCall += () =>
            {
                var scenes = EditorBuildSettings.scenes;
                if (scenes != null && scenes.Length > 0)
                    EditorSceneManager.OpenScene(scenes[0].path);

                // Bring the Game view to the front and maximize it on play.
                var gv = Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gv != null)
                {
                    var win = EditorWindow.GetWindow(gv, false, "Game", true);
                    if (win != null) { win.Show(); win.Focus(); win.maximized = true; }
                }
                EditorApplication.isPlaying = true;   // enter Play mode
                Debug.Log("MTA: PlayNow entered Play mode.");
            };
        }
    }
}
#endif
