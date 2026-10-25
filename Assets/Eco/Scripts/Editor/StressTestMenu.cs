using System.IO;
using Eco.Scripts.Profiling;
using UnityEditor;
using UnityEngine;

namespace Eco.Scripts.EditorTools
{
    /// <summary>
    /// Editor entry point for <see cref="StressTest"/>. The request flag lives in SessionState so it survives the
    /// domain reload on entering play mode, and is cleared as soon as play mode ends.
    /// </summary>
    [InitializeOnLoad]
    internal static class StressTestMenu
    {
        private const string RunMenu = "Eco/Profiling/Run Stress Test";
        private const string ReportsMenu = "Eco/Profiling/Open Stress Test Reports";

        static StressTestMenu()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem(RunMenu)]
        private static void Run()
        {
            SessionState.SetBool(StressTest.EditorSessionKey, true);
            EditorApplication.EnterPlaymode();
        }

        [MenuItem(RunMenu, true)]
        private static bool CanRun() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem(ReportsMenu)]
        private static void OpenReports()
        {
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "StressTest"));
            Directory.CreateDirectory(dir);
            EditorUtility.RevealInFinder(dir);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.EraseBool(StressTest.EditorSessionKey);
            }
        }
    }
}
