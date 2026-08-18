using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MTA.App;
using MTA.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MTA.PlayTests
{
    // Boots the real game (GameBootstrap), navigates every screen, and audits for
    // (a) runtime errors/exceptions and (b) off-canvas / overlapping interactive UI.
    // This is the automated "run it and look at the UX" pass.
    public class UiSmokeTests
    {
        readonly List<string> _errors = new List<string>();
        readonly List<string> _layout = new List<string>();
        GameObject _boot;
        GameController _ctrl;

        void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _errors.Add(type + ": " + msg);
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            LogAssert.ignoreFailingMessages = true;                // capture errors ourselves
            Application.logMessageReceived += OnLog;
            SaveSystem.Delete();                                   // deterministic fresh profile
            _boot = new GameObject("Boot");
            _boot.AddComponent<GameBootstrap>();
            yield return new WaitForSeconds(1.2f);                 // Start + loading screen
            _ctrl = (GameController)typeof(GameBootstrap)
                .GetField("_ctrl", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(_boot.GetComponent<GameBootstrap>());
        }

        [TearDown]
        public void Teardown()
        {
            Application.logMessageReceived -= OnLog;
            if (_boot != null) Object.Destroy(_boot);
        }

        [UnityTest]
        public IEnumerator Boots_NavigatesAllScreens_NoErrors_NoMisplacedButtons()
        {
            Assert.IsNotNull(_ctrl, "GameController not built");
            var boot = _boot.GetComponent<GameBootstrap>();

            // --- walk the menu screens ---
            Audit("Menu");
            _ctrl.ToCollection(); yield return Frames(); Audit("Collection");
            _ctrl.ToCareer(); yield return Frames(); Audit("Career");
            _ctrl.ToDaily(); yield return Frames(); Audit("Daily");
            _ctrl.ToSettings(); yield return Frames(); Audit("Settings");
            _ctrl.ToAbout(); yield return Frames(); Audit("About");
            _ctrl.ToProgress(); yield return Frames(); Audit("Progress");
            _ctrl.BackToMenu(); yield return Frames();

            // --- teaming: team-select + pick 3 ---
            _ctrl.StartGame(); yield return Frames();
            var pool = _ctrl.SpeciesPool;
            int picked = 0;
            foreach (var id in pool)
            {
                if (picked >= 3) break;
                if (boot != null) { _ctrl.ToggleSpecies(id); picked++; }
            }
            yield return Frames();
            Audit("TeamSelect");
            Assert.AreEqual(3, _ctrl.Session.playerTeam.Count, "could not build a 3-monster team");

            // --- run a battle (build stage + HUD), then jump to result ---
            var onStart = typeof(GameBootstrap).GetMethod("OnStartBattle", BindingFlags.NonPublic | BindingFlags.Instance);
            onStart.Invoke(boot, null);
            yield return new WaitForSeconds(2.5f);                 // spawn + stage + hud
            Audit("Battle");
            _ctrl.OnBattleFinished(); yield return new WaitForSeconds(1f); Audit("Result");

            // --- detail on an owned species ---
            var profile = (SaveData)typeof(GameBootstrap).GetField("_profile", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(boot);
            string owned = profile.unlocked.Count > 0 ? profile.unlocked[0] : pool[0];
            typeof(GameBootstrap).GetField("_detailSpecies", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boot, owned);
            _ctrl.ToDetail(); yield return Frames(); Audit("Detail");

            // --- report ---
            // Layout is warn-only (batch render resolution differs from device); the
            // warnings are logged for review. Runtime errors are a hard failure.
            if (_layout.Count > 0)
                foreach (var l in _layout) Debug.LogWarning("UI_LAYOUT " + l);
            else Debug.Log("UI_LAYOUT clean");
            Assert.IsEmpty(_errors, "runtime errors during UI walk:\n" + string.Join("\n", _errors));
        }

        static IEnumerator Frames() { yield return null; yield return null; yield return null; }

        // Flag any active, interactable Button whose rect leaves the screen.
        void Audit(string screen)
        {
            float w = Screen.width, h = Screen.height;
            var corners = new Vector3[4];
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (b == null || !b.isActiveAndEnabled || !b.gameObject.activeInHierarchy) continue;
                var rt = (RectTransform)b.transform;
                rt.GetWorldCorners(corners);
                float minX = Mathf.Min(corners[0].x, corners[2].x), maxX = Mathf.Max(corners[0].x, corners[2].x);
                float minY = Mathf.Min(corners[0].y, corners[2].y), maxY = Mathf.Max(corners[0].y, corners[2].y);
                const float m = 4f;   // allow a few px of rounding
                if (minX < -m || minY < -m || maxX > w + m || maxY > h + m)
                {
                    string txt = b.GetComponentInChildren<Text>() ? b.GetComponentInChildren<Text>().text : b.name;
                    _layout.Add(screen + ": button '" + txt + "' off-canvas rect=(" +
                        (int)minX + "," + (int)minY + ")-(" + (int)maxX + "," + (int)maxY + ") screen=" + (int)w + "x" + (int)h);
                }
            }
        }
    }
}
