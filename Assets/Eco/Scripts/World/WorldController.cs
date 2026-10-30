using System.Collections.Generic;
using Eco.Scripts.Profiling;
using Eco.Scripts.Pooling;
using Eco.Scripts.Upgrades;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

namespace Eco.Scripts.World
{
    /// <summary>
    /// Streams the hex world painted in <see cref="WorldData"/>: every hex within <see cref="RenderRadius"/> of the
    /// player's hex is a spawned <see cref="Chunk"/>, pooled per <see cref="ChunkDefinition"/>. The terrain itself
    /// is baked by the World Editor; this only builds the NavMesh over it.
    /// </summary>
    public class WorldController : MonoBehaviour
    {
        [SerializeField] private WorldData worldData;
        [SerializeField] private Transform player;
        [Tooltip("How many hexes around the player's hex are spawned.")]
        [SerializeField] private int renderRadius = 7;
        [SerializeField] private NavMeshSurface navMeshPlane;

        private readonly Dictionary<Vector2Int, Chunk> _spawnedChunks = new();
        private readonly Dictionary<ChunkDefinition, ObjectPool<Chunk>> _pools = new();
        private readonly ScrapRestingPoses _scrapRestingPoses = new();

        // Reused by UpdateWorld on every chunk crossing
        private readonly HashSet<Vector2Int> _neededChunks = new();
        private readonly List<Vector2Int> _inRange = new();
        private readonly List<Vector2Int> _chunksToRemove = new();
        private readonly List<LandChunk> _spawnedLand = new();

        private Vector2Int _currentPlayerChunkCoord;
        private SaveManager _saveManager;
        private PlayerStats _stats;
        private CurrencyManager _currencyManager;
        private bool _initialized;
        private TreePlanter _treePlanter;
        private ChunkContext _context;
        private HexTileLayout _layout;

        // Walkable ground this far above the water or lower belongs to the shallows
        private const float NavMeshWaterMargin = 0.15f;

        // NavMesh area of the shallows, which only the player walks on
        private const string ShallowsArea = "Shallows";

        // How far from the land anything in the shallows can be, at the deepest and widest
        private const float LandSearchDistance = 20f;

        // How far a saved player position may be from the NavMesh and still be used
        private const float SpawnSearchDistance = 5f;

        public WorldData WorldData => worldData;
        public int RenderRadius => renderRadius;
        public float HexRadius => worldData.HexRadius;
        public TreePlanter TreePlanter => _treePlanter;
        public Dictionary<Vector2Int, Chunk> ActiveChunks => _spawnedChunks;

        /// <summary>NavMesh area mask for everyone but the player: all of it except the shallows.</summary>
        public static int LandAreas => NavMesh.AllAreas & ~(1 << NavMesh.GetAreaFromName(ShallowsArea));

        /// <summary>The nearest point on the land part of the NavMesh, or <paramref name="position"/> if none.</summary>
        public static Vector3 ClosestLand(Vector3 position)
        {
            return NavMesh.SamplePosition(position, out var hit, LandSearchDistance, LandAreas)
                ? hit.position
                : position;
        }

        [Inject]
        public void Initialize(SaveManager saveManager, PlayerStats stats, CurrencyManager currencyManager)
        {
            _saveManager = saveManager;
            _stats = stats;
            _currencyManager = currencyManager;
        }

        /// <summary>Builds the NavMesh and pools, then spawns the chunks around where the player will start.</summary>
        /// <returns>The player's start position (see <see cref="GetSpawnPosition"/>).</returns>
        public Vector3 SpawnWorld(Vector3? savedPlayerPosition)
        {
            using var marker = EcoProfilerMarkers.SpawnWorld.Auto();

            _layout = HexTileLayout.Get(worldData.HexRadius);
            _treePlanter = new TreePlanter(player, this);
            _context = new ChunkContext(worldData, _treePlanter, _scrapRestingPoses, _currencyManager, _stats);

            CreatePools();
            RebuildNavMesh();

            var spawn = GetSpawnPosition(savedPlayerPosition);
            _currentPlayerChunkCoord = Hex.FromWorld(spawn, worldData.HexRadius);
            UpdateWorld(_currentPlayerChunkCoord);
            _initialized = true;
            return spawn;
        }

        // The saved position if it is still on the NavMesh (the world may have been repainted since), else the
        // spawn hex of the world data
        private Vector3 GetSpawnPosition(Vector3? savedPosition)
        {
            if (savedPosition.HasValue &&
                NavMesh.SamplePosition(savedPosition.Value, out var hit, SpawnSearchDistance, NavMesh.AllAreas))
            {
                return hit.position;
            }

            var spawn = WorldTerrain.OnGround(Hex.ToWorld(worldData.GetSpawnCoord(), worldData.HexRadius));
            return NavMesh.SamplePosition(spawn, out hit, worldData.HexRadius * 2f, NavMesh.AllAreas)
                ? hit.position
                : spawn;
        }

        private void CreatePools()
        {
            foreach (var pair in GetPeakChunkCounts())
            {
                GetPool(pair.Key, pair.Value);
            }
        }

        private ObjectPool<Chunk> GetPool(ChunkDefinition definition, int initialSize = 0)
        {
            if (_pools.TryGetValue(definition, out var pool))
            {
                return pool;
            }

            if (definition.Prefab == null)
            {
                Debug.LogError($"Chunk '{definition.name}' has no prefab. Open the World Editor to create it.",
                    definition);
            }
            else if (definition.IsWater != definition.Prefab is WaterChunk)
            {
                Debug.LogError($"Chunk '{definition.name}' is {(definition.IsWater ? "water" : "land")} but its " +
                               $"prefab is a {definition.Prefab.GetType().Name}.", definition);
            }
            else
            {
                pool = new ObjectPool<Chunk>(definition.Prefab, initialSize, transform);
            }

            // A null pool is remembered too, so a broken definition logs once
            _pools[definition] = pool;
            return pool;
        }

        // The most chunks of each definition in view at once from any land hex. Pooling that many during load means
        // walking to the coast never instantiates chunks mid-game.
        private Dictionary<ChunkDefinition, int> GetPeakChunkCounts()
        {
            var peak = new Dictionary<ChunkDefinition, int>();
            var inView = new Dictionary<ChunkDefinition, int>();
            var viewpoints = new List<Vector2Int>();
            foreach (var cell in worldData.Cells)
            {
                if (cell.chunk != null && !cell.chunk.IsWater)
                {
                    viewpoints.Add(cell.coord);
                }
            }

            if (viewpoints.Count == 0)
            {
                viewpoints.Add(Vector2Int.zero);
            }

            foreach (var viewpoint in viewpoints)
            {
                inView.Clear();
                Hex.Range(viewpoint, renderRadius, _inRange);
                foreach (var coord in _inRange)
                {
                    var definition = worldData.GetChunk(coord);
                    if (definition != null)
                    {
                        inView[definition] = inView.GetValueOrDefault(definition) + 1;
                    }
                }

                foreach (var pair in inView)
                {
                    if (pair.Value > peak.GetValueOrDefault(pair.Key))
                    {
                        peak[pair.Key] = pair.Value;
                    }
                }
            }

            return peak;
        }

        private void RebuildNavMesh()
        {
            using var marker = EcoProfilerMarkers.BuildNavMesh.Auto();

            if (!worldData.TryGetLandBounds(out var land))
            {
                Debug.LogWarning("The world has no land to walk on; paint some in the World Editor.");
                return;
            }

            // Bake the land and the shallows around it, as far out as the shelf can reach, from the deepest water
            // the player can wade in up: deeper lakes and sea are cut out by height, so the walkable area ends where
            // the shelf the World Editor baked into the terrain gets too deep.
            var terrain = Terrain.activeTerrain;
            float bottom = worldData.WaterLevel - worldData.WadeDepth;
            float top = terrain.GetPosition().y + terrain.terrainData.size.y;
            var worldCenter = new Vector3(land.center.x, (bottom + top) / 2f, land.center.y);
            float width = land.width + worldData.ShelfWidth * 2f;
            float length = land.height + worldData.ShelfWidth * 2f;

            navMeshPlane.collectObjects = CollectObjects.Volume;
            navMeshPlane.center = navMeshPlane.transform.InverseTransformPoint(worldCenter);
            navMeshPlane.size = new Vector3(width, top - bottom, length);

            // The same ground as before the water was walkable stays in the default area, for the pets
            float shore = worldData.WaterLevel + NavMeshWaterMargin;
            var shallows = navMeshPlane.gameObject.AddComponent<NavMeshModifierVolume>();
            shallows.area = NavMesh.GetAreaFromName(ShallowsArea);
            shallows.center = navMeshPlane.transform.InverseTransformPoint(
                new Vector3(land.center.x, (bottom + shore) / 2f, land.center.y));
            shallows.size = new Vector3(width, shore - bottom, length);

            navMeshPlane.BuildNavMesh();
        }

        void Update()
        {
            if (!_initialized)
            {
                return;
            }

            Vector2Int newChunkCoord = GetPlayerChunkCoord();
            if (newChunkCoord != _currentPlayerChunkCoord)
            {
                _currentPlayerChunkCoord = newChunkCoord;
                UpdateWorld(newChunkCoord);
            }
        }

        public Vector2Int GetPlayerChunkCoord()
        {
            return Hex.FromWorld(player.position, worldData.HexRadius);
        }

        private void UpdateWorld(Vector2Int center)
        {
            using var marker = EcoProfilerMarkers.UpdateWorld.Auto();

            var neededChunks = _neededChunks;
            neededChunks.Clear();
            _spawnedLand.Clear();

            Hex.Range(center, renderRadius, _inRange);
            foreach (var coord in _inRange)
            {
                neededChunks.Add(coord);
                if (!_spawnedChunks.ContainsKey(coord))
                {
                    SpawnChunk(coord);
                }
            }

            // Scrap that rested before goes straight back to sleep. The new transforms have to reach physics
            // first, or the sync on the next step would wake the bodies again.
            if (_spawnedLand.Count > 0)
            {
                Physics.SyncTransforms();
                foreach (var land in _spawnedLand)
                {
                    land.SleepRestingScrap();
                }

                _spawnedLand.Clear();
            }

            //remove far-away chunks
            var toRemove = _chunksToRemove;
            toRemove.Clear();
            foreach (var chunkCoord in _spawnedChunks.Keys)
            {
                if (!neededChunks.Contains(chunkCoord))
                {
                    toRemove.Add(chunkCoord);
                }
            }

            foreach (var coord in toRemove)
            {
                DespawnChunk(coord);
            }
        }

        private void SpawnChunk(Vector2Int coord)
        {
            var definition = worldData.GetChunk(coord);
            if (definition == null)
            {
                return;
            }

            var pool = GetPool(definition);
            if (pool == null)
            {
                return;
            }

            EcoProfilerMarkers.SpawnChunk.Begin();
            Chunk chunk = pool.Get();

            // Place the chunk before Init so scrap and trees spawn at their final position. Land chunks stand on the
            // baked ground at their centre (smoothing moves it off the definition's elevation), so objects in their
            // prefab sit on it.
            var position = Hex.ToWorld(coord, worldData.HexRadius);
            position.y = definition.IsWater ? worldData.WaterLevel : WorldTerrain.GroundHeight(position);
            chunk.transform.parent = transform;
            chunk.transform.SetPositionAndRotation(position, Quaternion.identity);
            chunk.Setup(coord, definition, _saveManager, _layout);
            chunk.Init(_context);

            if (chunk is LandChunk land)
            {
                _spawnedLand.Add(land);
            }

            chunk.name = $"{definition.name}_{coord.x}_{coord.y}";
            _spawnedChunks[coord] = chunk;
            EcoProfilerMarkers.SpawnChunk.End();
        }

        private void DespawnChunk(Vector2Int coord)
        {
            EcoProfilerMarkers.DespawnChunk.Begin();
            var chunk = _spawnedChunks[coord];
            chunk.SaveTiles();
            if (chunk is LandChunk land)
            {
                land.StoreRestingScrap();
            }

            _pools[chunk.Definition].ReturnToPool(chunk);
            _spawnedChunks.Remove(coord);
            EcoProfilerMarkers.DespawnChunk.End();
        }

        public void SaveWorld()
        {
            foreach (var chunk in _spawnedChunks.Values)
            {
                chunk.SaveTiles();
            }
        }
    }
}
