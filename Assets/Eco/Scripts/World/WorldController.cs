using System.Collections.Generic;
using System.Linq;
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
        private UpgradesCollection _upgrades;
        private CurrencyManager _currencyManager;
        private PileScoreUpgrade _pileScoreUpgrade;
        private DigPowerUpgrade _digPowerUpgrade;

        public const int ChunkSize = 10;
        public int RenderRadius => renderRadius;
        public TreePlanter TreePlanter => _treePlanter;
        public Dictionary<Vector2Int, Chunk> ActiveChunks => _spawnedChunks;

        public int WorldSideSize => _worldPreset.WorldSideSize;
        public int WorldSize => WorldSideSize * 2 + 1;
        
        private readonly Dictionary<ChunkType, ObjectPool<Chunk>> _pools = new();
        
        [Inject]
        public void Initialize(SaveManager saveManager, UpgradesCollection upgrades, CurrencyManager currencyManager)
        {
            _saveManager = saveManager;
            _upgrades = upgrades;
            _currencyManager = currencyManager;
        }

        public void SpawnWorld(WorldPreset worldPreset)
        {
            _worldPreset = worldPreset;
            _worldPreset.GenerateMap();
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
                
                _pools[chunkPrefab.type] = chunkPrefab.CreatePool(renderRadius * renderRadius, transform);
            }
            

            _treePlanter = new TreePlanter(_upgrades, player, this);
            _treePlanter.Init();
            
            // Get the PileScoreUpgrade from upgrades collection
            _pileScoreUpgrade = _upgrades.GetUpgradeType<PileScoreUpgrade>();
            _digPowerUpgrade = _upgrades.GetUpgradeType<DigPowerUpgrade>();

            Flatten();
            RebuildNavMesh();
            UpdateWorld();
            _initialized = true;
         }

        private void RebuildNavMesh()
        {
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
             Vector2Int center = GetPlayerChunkCoord();
             HashSet<Vector2Int> neededChunks = new();

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
                         
                         Chunk chunk = _pools[type].Get();
                         chunk.Setup(coord, _saveManager, _worldPreset.TrashPerChunk);
                         
                         if(type == ChunkType.Water)
                         {
                             WaterChunk waterChunk = chunk as WaterChunk;
                             if (waterChunk != null)
                             {
                                 waterChunk.Init();
                                 waterChunk.UpdateWaterCorners(WorldSideSize, coord);
                                 pos.y = -1;
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
                                 fieldChunk.Init(_treePlanter);
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
                                 pileChunk.Init(_currencyManager, _pileScoreUpgrade, _digPowerUpgrade, _worldPreset.Difficulty);
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

                         chunk.transform.parent = transform;
                         chunk.transform.position = pos;
                         chunk.transform.rotation = Quaternion.identity;

                         chunk.name = $"{type}_Chunk_{coord.x}_{coord.y}";
                         _spawnedChunks[coord] = chunk;
                     }
                 }
             }

             //remove far-away chunks
             List<Vector2Int> toRemove = new();
             foreach (var chunkCoord in _spawnedChunks.Keys)
             {
                 if (!neededChunks.Contains(chunkCoord))
                 {
                     toRemove.Add(chunkCoord);
                 }
             }

             foreach (var coord in toRemove)
             {
                 var chunk = _spawnedChunks[coord];
                 chunk.SaveTiles();

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
             }
         }

         public void Flatten()
         {
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

             float falloff = 50;

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

         private void OnDestroy()
         {
             _treePlanter?.Clear();
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
