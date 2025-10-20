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
        [SerializeField] private int worldSize = 52;
        [SerializeField] private NavMeshSurface navMeshPlane;

        private readonly Dictionary<Vector2Int, Chunk> _spawnedChunks = new();
        private Vector2Int _currentPlayerChunkCoord;
        private SaveManager _saveManager;

        private bool _initialized;
        private TreePlanter _treePlanter;
        private UpgradesCollection _upgrades;

        public const int ChunkSize = 10;
        public int RenderRadius => renderRadius;
        public TreePlanter TreePlanter => _treePlanter;
        public Dictionary<Vector2Int, Chunk> ActiveChunks => _spawnedChunks;
        public int WorldSize => worldSize;

        private readonly Dictionary<ChunkType, ObjectPool<Chunk>> _pools = new();
        private readonly Dictionary<Vector2Int, ChunkType> _map = new();
        
        [Inject]
        public void Initialize(SaveManager saveManager, UpgradesCollection upgrades)
        {
            _saveManager = saveManager;
            _upgrades = upgrades;
        }

        public void SpawnWorld()
        {
            foreach (var chunkPrefab in chunkPrefabs)
            {
                _pools[chunkPrefab.type] = chunkPrefab.CreatePool(renderRadius * renderRadius, transform);
            }
            
            Random.InitState(5);
            var allTypes = _pools.Keys.ToArray();
            for (int x = -worldSize; x < worldSize; x++)
            {
                for (int y = -worldSize; y < worldSize; y++)
                {
                    var rand = Random.Range(0, 2);
                    var type = allTypes[0];
                    
                    _map[new Vector2Int(x, y)] = type;
                }
            }

            _treePlanter = new TreePlanter(_upgrades, player, this);
            _treePlanter.Init();

            var planeSize = worldSize * 2 + 1;
            navMeshPlane.transform.localScale = new Vector3(planeSize, 1, planeSize);
            navMeshPlane.collectObjects = CollectObjects.Volume;
            navMeshPlane.size = new Vector3(planeSize * 10, 10, planeSize * 10);
            navMeshPlane.BuildNavMesh();
            
            Flatten();
            UpdateWorld();
            _initialized = true;
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

                        var type = _map.GetValueOrDefault(coord, ChunkType.Water);
                        Chunk chunk = _pools[type].Get();
                        chunk.Setup(coord, _saveManager);
                        
                        if(type == ChunkType.Water)
                        {
                            WaterChunk waterChunk = (WaterChunk)chunk;
                            waterChunk.Init();
                            waterChunk.UpdateWaterCorners(worldSize, coord);
                            pos.y = -1;
                        }
                        else if(type == ChunkType.Field)
                        {
                            FieldChunk fieldChunk = (FieldChunk)chunk;
                            fieldChunk.Init(_treePlanter);
                        } else if (type == ChunkType.Pile)
                        {
                            PileChunk pileChunk = (PileChunk)chunk;
                            pileChunk.Init();
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

                _pools[chunk.Type].ReturnToPool(chunk);
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

            var size = worldSize * 2 * 10;
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
        }
    }
}