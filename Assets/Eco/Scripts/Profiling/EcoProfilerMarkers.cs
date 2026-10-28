using System.Collections.Generic;
using Unity.Profiling;

namespace Eco.Scripts.Profiling
{
    /// <summary>
    /// Profiler markers around the game's known heavy paths. They show up in the Profiler window under these
    /// names and the stress test reports their per-phase cost. Cheap enough to leave in release builds.
    /// </summary>
    public static class EcoProfilerMarkers
    {
        // Must be declared before the markers: static fields initialize in textual order.
        private static readonly List<string> NameList = new();

        public static readonly ProfilerMarker LoadGame = Create("Eco.SaveManager.Load");
        public static readonly ProfilerMarker InitPools = Create("Eco.PoolManager.InitializePools");
        public static readonly ProfilerMarker ClearTerrain = Create("Eco.TerrainPainter.ClearTerrain");
        public static readonly ProfilerMarker SpawnWorld = Create("Eco.WorldController.SpawnWorld");
        public static readonly ProfilerMarker FlattenTerrain = Create("Eco.WorldController.Flatten");
        public static readonly ProfilerMarker BuildNavMesh = Create("Eco.WorldController.RebuildNavMesh");
        public static readonly ProfilerMarker UpdateWorld = Create("Eco.WorldController.UpdateWorld");
        public static readonly ProfilerMarker SpawnChunk = Create("Eco.WorldController.SpawnChunk");
        public static readonly ProfilerMarker DespawnChunk = Create("Eco.WorldController.DespawnChunk");
        public static readonly ProfilerMarker FieldChunkInit = Create("Eco.FieldChunk.Init");
        public static readonly ProfilerMarker PileChunkInit = Create("Eco.PileChunk.Init");
        public static readonly ProfilerMarker SaveTiles = Create("Eco.Chunk.SaveTiles");
        public static readonly ProfilerMarker ProgressUpdate = Create("Eco.ProgressTracker.OnChunkUpdated");
        public static readonly ProfilerMarker ProgressWrite = Create("Eco.WorldProgressData.SaveForWorld");
        public static readonly ProfilerMarker PaintTerrain = Create("Eco.TerrainPainter.PaintPending");
        public static readonly ProfilerMarker PlantTree = Create("Eco.TreePlanter.PlantTree");
        public static readonly ProfilerMarker ScanForItems = Create("Eco.ItemCollector.ScanForItems");
        public static readonly ProfilerMarker ReserveInStomach = Create("Eco.Stomach.Reserve");
        public static readonly ProfilerMarker StomachFloat = Create("Eco.Stomach.Float");
        public static readonly ProfilerMarker JellyBody = Create("Eco.JellyBody.Update");
        public static readonly ProfilerMarker JellyTentacle = Create("Eco.JellyTentacle.Build");
        public static readonly ProfilerMarker CurvedBounds = Create("Eco.CurvedWorld.UpdateBounds");
        public static readonly ProfilerMarker Atmosphere = Create("Eco.CurvedWorld.RecordAtmosphere");
        public static readonly ProfilerMarker RecycleScrap = Create("Eco.ScrapItem.Recycle");
        public static readonly ProfilerMarker SaveGame = Create("Eco.SaveManager.Save");

        public static IReadOnlyList<string> Names => NameList;

        private static ProfilerMarker Create(string name)
        {
            NameList.Add(name);
            return new ProfilerMarker(ProfilerCategory.Scripts, name);
        }
    }
}
