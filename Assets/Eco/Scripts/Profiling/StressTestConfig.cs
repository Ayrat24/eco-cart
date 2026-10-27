using System.Collections.Generic;
using Eco.Scripts.World;
using UnityEngine;
using Tree = Eco.Scripts.Trees.Tree;

namespace Eco.Scripts.Profiling
{
    /// <summary>
    /// Tuning for the worst-case stress test (see <see cref="StressTest"/>). Must live in a Resources folder
    /// as "StressTestConfig" so dev builds can load it too.
    /// </summary>
    [CreateAssetMenu(menuName = "Eco/Profiling/StressTestConfig", fileName = "StressTestConfig")]
    public class StressTestConfig : ScriptableObject
    {
        [Header("World")]
        [Tooltip("Preset the stress world is cloned from (chunk types, scrap prefabs, difficulty).")]
        [SerializeField] private WorldPreset basePreset;
        [Tooltip("Half-size of the world in chunks. The biggest shipped world uses 14; the terrain is sized for it.")]
        [SerializeField] private int worldSideSize = 14;
        [SerializeField] private int seed = 1;

        [Header("Field content")]
        [Tooltip("Scrap items seeded into every field chunk (shipped worlds use 2-5, max 100).")]
        [SerializeField, Range(0, 100)] private int scrapPerChunk = 10;
        [Tooltip("Distance in tiles between seeded trees. In the real game a tree greens a 12-tile circle and the " +
                 "next tree can only go on non-green ground, so a fully green world has trees ~12 tiles apart.")]
        [SerializeField, Min(1)] private int treeSpacing = 6;
        [Tooltip("Chunks around the spawn point left without trees, so the Plant phase has ground to plant on.")]
        [SerializeField, Min(0)] private int clearingRadiusChunks = 1;
        [Tooltip("Must match the tree prefabs registered on PoolManager in the game scene.")]
        [SerializeField] private List<Tree> treePrefabs;
        [SerializeField] private Settings.DetailQualities detailQuality = Settings.DetailQualities.High;

        [Header("Run")]
        [SerializeField, Min(0)] private float idleSeconds = 5f;
        [SerializeField, Min(0)] private int treesToPlant = 10;
        [SerializeField, Min(0)] private float plantInterval = 0.5f;
        [Tooltip("Collect phase: drive a small loop around the spawn at the player's own speed, swallowing scrap " +
                 "and recycling it whenever the stomach is full.")]
        [SerializeField, Min(0)] private float collectSeconds = 20f;
        [SerializeField, Min(1)] private float collectLoopSize = 8f;
        [Tooltip("Churn phase: teleports between two spots far enough apart that every loaded chunk is " +
                 "despawned and a full new set spawned.")]
        [SerializeField, Min(0)] private int churnJumps = 6;
        [SerializeField, Min(0)] private float churnInterval = 1f;
        [SerializeField, Min(1)] private float moveSpeed = 15f;
        [Tooltip("The player drives a square loop this many chunks from the world centre.")]
        [SerializeField, Min(1)] private int routeRadiusChunks = 10;
        [SerializeField, Min(1)] private int laps = 1;
        [SerializeField, Min(1)] private float maxTraverseSeconds = 180f;
        [Tooltip("Seconds of the Traverse phase to record into a .raw Profiler capture next to the report " +
                 "(0 = off). Editor captures run ~30 MB per second.")]
        [SerializeField, Min(0)] private float profilerCaptureSeconds;
        [SerializeField] private bool quitWhenDone = true;

        public int WorldSideSize => worldSideSize;
        public int Seed => seed;
        public int ScrapPerChunk => scrapPerChunk;
        public int TreeSpacing => treeSpacing;
        public int ClearingRadiusChunks => clearingRadiusChunks;
        public IReadOnlyList<Tree> TreePrefabs => treePrefabs;
        public Settings.DetailQualities DetailQuality => detailQuality;
        public float IdleSeconds => idleSeconds;
        public int TreesToPlant => treesToPlant;
        public float PlantInterval => plantInterval;
        public float CollectSeconds => collectSeconds;
        public float CollectLoopSize => collectLoopSize;
        public int ChurnJumps => churnJumps;
        public float ChurnInterval => churnInterval;
        public float MoveSpeed => moveSpeed;
        public int RouteRadiusChunks => routeRadiusChunks;
        public int Laps => laps;
        public float MaxTraverseSeconds => maxTraverseSeconds;
        public float ProfilerCaptureSeconds => profilerCaptureSeconds;
        public bool QuitWhenDone => quitWhenDone;

        public WorldPreset CreatePreset(string worldId)
        {
            return basePreset.CreateVariant(worldId, worldSideSize, scrapPerChunk);
        }
    }
}
