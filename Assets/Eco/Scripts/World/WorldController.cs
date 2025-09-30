using System;
using System.Collections.Generic;
using System.Linq;
using Eco.Scripts.Pooling;
using Eco.Scripts.Trees;
using Eco.Scripts.Upgrades;
using R3;
using Unity.Mathematics;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace Eco.Scripts.World
{
    public class WorldController : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] Field chunkPrefab;
        [SerializeField] WaterField waterPrefab;
        [SerializeField] int chunkSize = 10;
        [SerializeField] int renderRadius = 5; // how many chunks in each direction from the player
        [SerializeField] private int worldSize = 52;

        private readonly Dictionary<Vector2Int, Field> _spawnedChunks = new();
        private Vector2Int _currentPlayerChunkCoord;
        private SaveManager _saveManager;

        private ObjectPool<Field> _fieldPool;
        private ObjectPool<WaterField> _waterPool;
        private bool _initialized;
        private TreePlanter _treePlanter;
        private UpgradesCollection _upgrades;

        public int ChunkSize => chunkSize;
        public int RenderRadius => renderRadius;
        public Dictionary<Vector2Int, Field> ActiveChunks => _spawnedChunks;
        
        [Inject]
        public void Initialize(SaveManager saveManager, UpgradesCollection upgrades)
        {
            _saveManager = saveManager;
            _upgrades = upgrades;
        }

        public void SpawnWorld()
        {
            _fieldPool = new ObjectPool<Field>(chunkPrefab, renderRadius * renderRadius, transform);
            _waterPool = new ObjectPool<WaterField>(waterPrefab, renderRadius * 2);

            _treePlanter = new TreePlanter(_upgrades, player, this);
            _treePlanter.Init();
            
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
            int x = Mathf.FloorToInt(player.position.x / chunkSize);
            int z = Mathf.FloorToInt(player.position.z / chunkSize);
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
                        Vector3 pos = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);

                        Field chunk;
                        if (coord.x > worldSize || coord.x < -worldSize || coord.y > worldSize || coord.y < -worldSize)
                        {
                            WaterField waterChunk = _waterPool.Get();
                            waterChunk.UpdateWaterCorners(worldSize, coord);

                            chunk = waterChunk;
                        }
                        else
                        {
                            chunk = _fieldPool.Get();
                        }

                        chunk.transform.parent = transform;
                        chunk.transform.position = pos;
                        chunk.transform.rotation = quaternion.identity;

                        chunk.Init(coord, _saveManager, _treePlanter);

                        chunk.name = $"Chunk_{coord.x}_{coord.y}";
                        _spawnedChunks[coord] = chunk;
                    }
                }
            }

            // Optionally remove far-away chunks
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
                if (chunk is WaterField waterField)
                {
                    _waterPool.ReturnToPool(waterField);
                }
                else
                {
                    _fieldPool.ReturnToPool(chunk);
                }

                _spawnedChunks.Remove(coord);
            }
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
    }
}