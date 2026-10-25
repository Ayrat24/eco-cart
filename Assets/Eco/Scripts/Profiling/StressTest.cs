using System;
using System.IO;
using Eco.Scripts.World;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eco.Scripts.Profiling
{
    /// <summary>
    /// Worst-case profiling run. When requested (editor menu Eco/Profiling/Run Stress Test, or the
    /// <see cref="CommandLineFlag"/> on a development build) it seeds a sandboxed save full of scrap, trees and
    /// grass, makes WorldSelector load that world and spawns a <see cref="StressTestDriver"/> that runs the
    /// scripted phases and writes a report. Saves go to <see cref="SaveDirectory"/>, never the player's own.
    /// </summary>
    public static class StressTest
    {
        public const string CommandLineFlag = "-ecoStressTest";
        public const string EditorSessionKey = "Eco.StressTest.Requested";
        public const string WorldId = "stress";
        private const string ConfigResourcePath = "StressTestConfig";

        public static bool IsActive { get; private set; }
        public static StressTestConfig Config { get; private set; }
        public static WorldPreset Preset { get; private set; }
        public static string SaveDirectory { get; private set; }
        public static string ReportDirectory { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            IsActive = false;

            if (!IsRequested())
            {
                return;
            }

            Config = Resources.Load<StressTestConfig>(ConfigResourcePath);
            if (Config == null)
            {
                Debug.LogError($"[StressTest] No StressTestConfig found at Resources/{ConfigResourcePath}.");
                return;
            }

            SaveDirectory = Path.Combine(Application.persistentDataPath, "StressTest");
            ReportDirectory = Application.isEditor
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "StressTest"))
                : Path.Combine(Application.persistentDataPath, "StressTestReports");

            // Fresh sandbox every run so results don't depend on what the previous run collected or planted.
            if (Directory.Exists(SaveDirectory))
            {
                Directory.Delete(SaveDirectory, true);
            }

            Directory.CreateDirectory(SaveDirectory);
            Directory.CreateDirectory(ReportDirectory);

            Preset = Config.CreatePreset(WorldId);
            IsActive = true;

            var seedStats = StressTestSeeder.Seed(Config, Preset);
            Debug.Log($"[StressTest] Seeded '{WorldId}' world in {SaveDirectory}: {seedStats}");

            var driver = new GameObject(nameof(StressTestDriver)).AddComponent<StressTestDriver>();
            Object.DontDestroyOnLoad(driver.gameObject);
            driver.SetSeedStats(seedStats);
        }

        private static bool IsRequested()
        {
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool(EditorSessionKey, false))
            {
                return true;
            }
#endif
            return Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), CommandLineFlag) >= 0;
        }
    }
}
