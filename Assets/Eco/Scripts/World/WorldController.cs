using System.Collections.Generic;
using System.Linq;
using Eco.Scripts.Profiling;
using Eco.Scripts.Pooling;
using Eco.Scripts.Upgrades;
using Unity.AI.Navigation;
using UnityEngine;
using VContainer;

namespace Eco.Scripts.World
{
    public class WorldController : MonoBehaviour
    {
        [SerializeField] List<ChunkPrefab> chunkPrefabs;
        [SerializeField] Transform player;
        [SerializeField] WaterChunk waterPrefab;
        [SerializeField] int renderRadius = 5; // how many chunks in each direction from the player
        [SerializeField] private NavMeshSurface navMeshPlane;

        private WorldPreset _worldPreset;
        private readonly Dictionary<Vector2Int, Chunk> _spawnedChunks = new();
        private Vector2Int _currentPlayerChunkCoord;
        private SaveManager _saveManager;

        private bool _initialized;
        private TreePlanter _treePlanter;
        private PlayerStats _stats;
        private CurrencyManager _currencyManager;

        public const int ChunkSize = 10;
        public int RenderRadius => renderRadius;
        public TreePlanter TreePlanter => _treePlanter;
        public Dictionary<Vector2Int, Chunk> ActiveChunks => _spawnedChunks;

        public int WorldSideSize => _worldPreset.WorldSideSize;
        public int WorldSize => WorldSideSize * 2 + 1;
        
        private readonly Dictionary<ChunkType, ObjectPool<Chunk>> _pools = new();
        private readonly ScrapRestingPoses _scrapRestingPoses = new();

        // Reused by UpdateWorld on every chunk crossing
        private readonly HashSet<Vector2Int> _neededChunks = new();
        private readonly List<Vector2Int> _chunksToRemove = new();
        private readonly List<FieldChunk> _spawnedFields = new();

        // Width of the slope Flatten adds around the plateau
        private const float FlattenFalloff = 50;
        
        [Inject]
        public void Initialize(SaveManager saveManager, PlayerStats stats, CurrencyManager currencyManager)
        {
            _saveManager = saveManager;
            _stats = stats;
            _currencyManager = currencyManager;
        }

        public void SpawnWorld(WorldPreset worldPreset)
        {
            using var marker = EcoProfilerMarkers.SpawnWorld.Auto();

            _worldPreset = worldPreset;
            _worldPreset.GenerateMap();
            var peakChunkCounts = GetPeakChunkCounts();
            foreach (var chunkPrefab in chunkPrefabs)
            {
                // Validate that the chunk prefab type matches its actual class
                if (!chunkPrefab.ValidateType())
                {
                    Debug.LogError($"ChunkPrefab validation failed! Prefab '{chunkPrefab.chunk.name}' " +
                                   $"is of type {chunkPrefab.chunk.GetType().Name} but has Type property set to {chunkPrefab.type}. " +
                                   $"Please fix this in the Unity Inspector on the WorldController component.");
                    continue;
                }
                
                //always create water and beach pools
                if (!_worldPreset.ChunkTypes.Contains(chunkPrefab.type) && 
                    chunkPrefab.type != ChunkType.Water && 
                    chunkPrefab.type != ChunkType.Beach)
                {
                    continue;
                }
                
                _pools[chunkPrefab.type] = chunkPrefab.CreatePool(
                    peakChunkCounts.GetValueOrDefault(chunkPrefab.type), transform);
            }
            

            _treePlanter = new TreePlanter(player, this);

            Flatten();
            RebuildNavMesh();
            UpdateWorld();
            _initialized = true;
         }

        // The most chunks of each type in view at once from anywhere in the world. Pooling that many during
        // load means walking to the coast never instantiates chunks mid-game.
        private Dictionary<ChunkType, int> GetPeakChunkCounts()
        {
            var peak = new Dictionary<ChunkType, int>();
            var inView = new Dictionary<ChunkType, int>();
            int side = WorldSideSize;

            for (int px = -side; px <= side; px++)
            {
                for (int pz = -side; pz <= side; pz++)
                {
                    inView.Clear();
                    for (int x = -renderRadius; x <= renderRadius; x++)
                    {
                        for (int z = -renderRadius; z <= renderRadius; z++)
                        {
                            var type = _worldPreset.GetChunkType(new Vector2Int(px + x, pz + z));
                            inView[type] = inView.GetValueOrDefault(type) + 1;
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
            }

            return peak;
        }

        private void RebuildNavMesh()
        {
            using var marker = EcoProfilerMarkers.BuildNavMesh.Auto();

            // Bake only the plateau and its slope into the sea, not the whole 1 km terrain
            var terrain = Terrain.activeTerrain;
            var terrainSize = terrain.terrainData.size;
            float side = WorldSize * ChunkSize + FlattenFalloff * 2;
            var worldCenter = terrain.transform.position + terrainSize / 2;

            navMeshPlane.collectObjects = CollectObjects.Volume;
            navMeshPlane.center = navMeshPlane.transform.InverseTransformPoint(worldCenter);
            navMeshPlane.size = new Vector3(side, terrainSize.y, side);
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
                 UpdateWorld();
             }
         }

         public Vector2Int GetPlayerChunkCoord()
         {
             int x = Mathf.FloorToInt(player.position.x / ChunkSize);
             int z = Mathf.FloorToInt(player.position.z / ChunkSize);
             return new Vector2Int(x, z);
         }

         private void UpdateWorld()
         {
             using var marker = EcoProfilerMarkers.UpdateWorld.Auto();

             Vector2Int center = GetPlayerChunkCoord();
             var neededChunks = _neededChunks;
             neededChunks.Clear();
             _spawnedFields.Clear();

             // Spawn chunks in render radius
             for (int x = -renderRadius; x <= renderRadius; x++)
             {
                 for (int z = -renderRadius; z <= renderRadius; z++)
                 {
                     Vector2Int coord = new Vector2Int(center.x + x, center.y + z);
                     neededChunks.Add(coord);

                     if (!_spawnedChunks.ContainsKey(coord))
                     {
                         Vector3 pos = new Vector3(coord.x * ChunkSize, 0, coord.y * ChunkSize);
                         var type = _worldPreset.GetChunkType(coord);
                         
                         if (!_pools.ContainsKey(type))
                         {
                             Debug.LogError($"Pool for chunk type {type} doesn't exist! Skipping chunk at {coord}");
                             continue;
                         }
                         
                         EcoProfilerMarkers.SpawnChunk.Begin();
                         Chunk chunk = _pools[type].Get();

                         // Place the chunk before Init so scrap and trees spawn at their final position
                         if (type == ChunkType.Water)
                         {
                             pos.y = -1;
                         }

                         chunk.transform.parent = transform;
                         chunk.transform.SetPositionAndRotation(pos, Quaternion.identity);
                         chunk.Setup(coord, _saveManager, _worldPreset.ScrapPerChunk);
                         
                         if(type == ChunkType.Water)
                         {
                             WaterChunk waterChunk = chunk as WaterChunk;
                             if (waterChunk != null)
                             {
                                 waterChunk.Init();
                                 waterChunk.UpdateWaterCorners(WorldSideSize, coord);
                             }
                             else
                             {
                                 Debug.LogError($"Chunk at {coord} is type Water but prefab is not WaterChunk!");
                             }
                         }
                         else if(type == ChunkType.Field)
                         {
                             FieldChunk fieldChunk = chunk as FieldChunk;
                             if (fieldChunk != null)
                             {
                                 fieldChunk.Init(_treePlanter, _scrapRestingPoses);
                                 _spawnedFields.Add(fieldChunk);
                             }
                             else
                             {
                                 Debug.LogError($"Chunk at {coord} is type Field but prefab is not FieldChunk!");
                             }
                         } 
                         else if (type == ChunkType.Pile)
                         {
                             PileChunk pileChunk = chunk as PileChunk;
                             if (pileChunk != null)
                             {
                                 pileChunk.Init(_currencyManager, _stats, _worldPreset.Difficulty);
                             }
                             else
                             {
                                 Debug.LogError($"Chunk at {coord} is type Pile but prefab is not PileChunk!");
                             }
                         }
                         else if (type == ChunkType.Beach)
                         {
                             BeachChunk beachChunk = chunk as BeachChunk;
                             if (beachChunk != null)
                             {
                                 beachChunk.Init();
                             }
                             else
                             {
                                 Debug.LogError($"Chunk at {coord} is type Beach but prefab is not BeachChunk!");
                             }
                         }

                         chunk.name = $"{type}_Chunk_{coord.x}_{coord.y}";
                         _spawnedChunks[coord] = chunk;
                         EcoProfilerMarkers.SpawnChunk.End();
                     }
                 }
             }

             // Scrap that rested before goes straight back to sleep. The new transforms have to reach physics
             // first, or the sync on the next step would wake the bodies again.
             if (_spawnedFields.Count > 0)
             {
                 Physics.SyncTransforms();
                 foreach (var field in _spawnedFields)
                 {
                     field.SleepRestingScrap();
                 }

                 _spawnedFields.Clear();
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
                 EcoProfilerMarkers.DespawnChunk.Begin();
                 var chunk = _spawnedChunks[coord];
                 chunk.SaveTiles();
                 if (chunk is FieldChunk field)
                 {
                     field.StoreRestingScrap();
                 }

                 // Validate chunk type matches before returning to pool
                 bool typeMatches = chunk.Type switch
                 {
                     ChunkType.Field => chunk is FieldChunk,
                     ChunkType.Water => chunk is WaterChunk,
                     ChunkType.Pile => chunk is PileChunk,
                     ChunkType.Beach => chunk is BeachChunk,
                     _ => false
                 };
                 
                 if (!typeMatches)
                 {
                     Debug.LogError($"Type mismatch detected! Chunk at {coord} is class {chunk.GetType().Name} " +
                                    $"but has Type property {chunk.Type}. This will cause pool contamination. " +
                                    $"Destroying chunk instead of returning to pool.");
                     Destroy(chunk.gameObject);
                 }
                 else
                 {
                     _pools[chunk.Type].ReturnToPool(chunk);
                 }
                 
                 _spawnedChunks.Remove(coord);
                 EcoProfilerMarkers.DespawnChunk.End();
             }
         }

         public void Flatten()
         {
             using var marker = EcoProfilerMarkers.FlattenTerrain.Auto();

             var terrain = Terrain.activeTerrain;
             TerrainData data = terrain.terrainData;

             int heightmapWidth = data.heightmapResolution;
             int heightmapHeight = data.heightmapResolution;

             float[,] heights = data.GetHeights(0, 0, heightmapWidth, heightmapHeight);

             float terrainWidth = data.size.x;
             float terrainLength = data.size.z;
             float maxHeight = data.size.y;

             var size = WorldSize * 10;
             float centerHalf = size / 2f;
             float centerX = terrainWidth / 2f;
             float centerZ = terrainLength / 2f;

             float normalizedCenterHeight = 10 / maxHeight;
             float normalizedBorderHeight = 0 / maxHeight;

             float falloff = FlattenFalloff;

             for (int y = 0; y < heightmapHeight; y++)
             {
                 for (int x = 0; x < heightmapWidth; x++)
                 {
                     float worldX = (x / (float)(heightmapWidth - 1)) * terrainWidth;
                     float worldZ = (y / (float)(heightmapHeight - 1)) * terrainLength;

                     // Distance from center of terrain
                     float dx = Mathf.Abs(worldX - centerX);
                     float dz = Mathf.Abs(worldZ - centerZ);
                     float distanceFromEdge = Mathf.Max(dx - centerHalf, dz - centerHalf);

                     if (distanceFromEdge <= 0)
                     {
                         // Inside plateau
                         heights[y, x] = normalizedCenterHeight;
                     }
                     else if (distanceFromEdge >= falloff)
                     {
                         // Fully outside plateau
                         heights[y, x] = normalizedBorderHeight;
                     }
                     else
                     {
                         // In falloff zone — blend smoothly
                         float t = Mathf.InverseLerp(0, falloff, distanceFromEdge);
                         heights[y, x] = Mathf.Lerp(normalizedCenterHeight, normalizedBorderHeight, t);
                     }
                 }
             }

             data.SetHeights(0, 0, heights);
         }

         public void SaveWorld()
         {
             foreach (var field in _spawnedChunks.Values)
             {
                 field.SaveTiles();
             }
         }

[System.Serializable]
         private class ChunkPrefab
         {
             public ChunkType type;
             public Chunk chunk;

             public ObjectPool<Chunk> CreatePool(int initialSize, Transform parent)
             {
                 return new ObjectPool<Chunk>(chunk, initialSize, parent);
             }
             
             public bool ValidateType()
             {
                 if (chunk == null)
                 {
                     Debug.LogError("Chunk prefab is null!");
                     return false;
                 }
                 
                 // Check if the actual chunk class matches the expected type
                 bool isValid = type switch
                 {
                     ChunkType.Field => chunk is FieldChunk,
                     ChunkType.Water => chunk is WaterChunk,
                     ChunkType.Pile => chunk is PileChunk,
                     ChunkType.Beach => chunk is BeachChunk,
                     _ => false
                 };
                 
                 return isValid;
             }
         }
     }
 }
