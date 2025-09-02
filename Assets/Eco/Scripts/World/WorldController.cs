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
        [SerializeField] int chunkSizeX = 10;
        [SerializeField] int chunkSizeY = 10;
        [SerializeField] int renderRadiusX = 2; // how many chunks in each direction from the player
        [SerializeField] int renderRadiusY = 2; // how many chunks in each direction from the player
        [SerializeField] private int worldSize = 52;

        private Vector2Int currentPlayerChunkCoord;
        private readonly Dictionary<Vector2Int, Field> spawnedChunks = new();
        private SaveManager _saveManager;

        private ObjectPool<Field> _fieldPool;
        private ObjectPool<WaterField> _waterPool;
        private bool _initialized;
        private UpgradesCollection _upgrades;
        private IDisposable _subscription;
        private TreePlanter _treePlanter;

        [Inject]
        public void Initialize(SaveManager saveManager, UpgradesCollection upgrades)
        {
            _saveManager = saveManager;
            _upgrades = upgrades;
        }

        public void SpawnWorld()
        {
            _fieldPool = new ObjectPool<Field>(chunkPrefab, renderRadiusX * renderRadiusY, transform);
            _waterPool = new ObjectPool<WaterField>(waterPrefab, renderRadiusX * 2);
            _treePlanter = new TreePlanter();

            UpdateWorld();

            DisposableBuilder builder = new DisposableBuilder();

            foreach (var tree in _upgrades.GetUpgradeTypes<TreeBuyUpgrade>())
            {
                tree.OnPurchase.Subscribe(PlantTree).AddTo(ref builder);
            }

            _subscription = builder.Build();

            _initialized = true;
        }

        void Update()
        {
            if (!_initialized)
            {
                return;
            }

            Vector2Int newChunkCoord = GetPlayerChunkCoord();
            if (newChunkCoord != currentPlayerChunkCoord)
            {
                currentPlayerChunkCoord = newChunkCoord;
                UpdateWorld();
            }
        }

        Vector2Int GetPlayerChunkCoord()
        {
            int x = Mathf.FloorToInt(player.position.x / chunkSizeX);
            int z = Mathf.FloorToInt(player.position.z / chunkSizeY);
            return new Vector2Int(x, z);
        }

        private void UpdateWorld()
        {
            Vector2Int center = GetPlayerChunkCoord();
            HashSet<Vector2Int> neededChunks = new();

            // Spawn chunks in render radius
            for (int x = -renderRadiusX; x <= renderRadiusX; x++)
            {
                for (int z = -renderRadiusY; z <= renderRadiusY; z++)
                {
                    Vector2Int coord = new Vector2Int(center.x + x, center.y + z);
                    neededChunks.Add(coord);

                    if (!spawnedChunks.ContainsKey(coord))
                    {
                        Vector3 pos = new Vector3(coord.x * chunkSizeX, 0, coord.y * chunkSizeY);

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
                        spawnedChunks[coord] = chunk;
                    }
                }
            }

            // Optionally remove far-away chunks
            List<Vector2Int> toRemove = new();
            foreach (var chunkCoord in spawnedChunks.Keys)
            {
                if (!neededChunks.Contains(chunkCoord))
                {
                    toRemove.Add(chunkCoord);
                }
            }

            foreach (var coord in toRemove)
            {
                var chunk = spawnedChunks[coord];
                if (chunk is WaterField waterField)
                {
                    _waterPool.ReturnToPool(waterField);
                }
                else
                {
                    _fieldPool.ReturnToPool(chunk);
                }

                spawnedChunks.Remove(coord);
            }
        }

        public void SaveWorld()
        {
            foreach (var field in spawnedChunks.Values)
            {
                field.SaveTiles();
            }
        }

        private void PlantTree(int id)
        {
            Vector3 playerPos = player.position; // Player world position
            Vector2Int playerChunkCoord = GetPlayerChunkCoord();
            int chunkSize = 10;

            Field closestField = null;
            Field.Tile closestTile = null;
            float closestDistSqr = float.MaxValue;

            Dictionary<Vector2Int, Field> groundChunks = spawnedChunks.Where(x => x.Value is not WaterField)
                .ToDictionary(x => x.Key, x => x.Value);

            // Search in surrounding chunks (adjust search range if needed)
            for (int cx = -renderRadiusX; cx <= renderRadiusX; cx++)
            {
                for (int cy = -renderRadiusY; cy <= renderRadiusY; cy++)
                {
                    Vector2Int chunkCoord = new Vector2Int(playerChunkCoord.x + cx, playerChunkCoord.y + cy);

                    if (groundChunks.TryGetValue(chunkCoord, out Field field))
                    {
                        foreach (var tile in field.Tiles)
                        {
                            if (tile.status == Field.TileStatus.Empty)
                            {
                                // Convert local tile pos to world pos (centered on tile)
                                Vector3 worldPos = new Vector3(
                                    chunkCoord.x * chunkSize + tile.position.x + 0.5f,
                                    0f,
                                    chunkCoord.y * chunkSize + tile.position.y + 0.5f
                                );

                                float distSqr = (worldPos - playerPos).sqrMagnitude;
                                if (distSqr < closestDistSqr)
                                {
                                    closestDistSqr = distSqr;
                                    closestTile = tile;
                                    closestField = field;
                                }
                            }
                        }
                    }
                }
            }

            if (closestTile != null)
            {
                _treePlanter.PlantTree(id, closestTile, closestField);
                MarkGroundCircle(closestTile, closestField, radius: Random.Range(18, 25));
            }
        }

        private void MarkGroundCircle(Field.Tile centerTile, Field centerField, int radius)
        {
            int chunkSize = 10;

            // Center tile's chunk coordinate in chunk grid
            Vector2Int centerChunkCoord = GetChunkCoordFromTile(centerTile.position, centerField);
            HashSet<Field> updatedFields = new();

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        // Convert local tile pos to global coordinates
                        Vector2Int globalPos = new Vector2Int(
                            centerChunkCoord.x * chunkSize + centerTile.position.x + x,
                            centerChunkCoord.y * chunkSize + centerTile.position.y + y
                        );

                        // Figure out which chunk this tile belongs to
                        Vector2Int targetChunkCoord = new Vector2Int(
                            Mathf.FloorToInt((float)globalPos.x / chunkSize),
                            Mathf.FloorToInt((float)globalPos.y / chunkSize)
                        );

                        if (spawnedChunks.TryGetValue(targetChunkCoord, out Field targetField))
                        {
                            // Local position inside the target chunk
                            Vector2Int localPos = new Vector2Int(
                                Mod(globalPos.x, chunkSize),
                                Mod(globalPos.y, chunkSize)
                            );

                            var adjTile = targetField.GetTile(localPos);
                            if (adjTile != null && adjTile.status == Field.TileStatus.Empty)
                            {
                                adjTile.status = Field.TileStatus.Ground;
                                updatedFields.Add(targetField);
                            }
                        }
                    }
                }
            }

            foreach (var f in updatedFields)
            {
                f.MakeGrass();
            }
        }

        private int Mod(int a, int m) => (a % m + m) % m;

        private Vector2Int GetChunkCoordFromTile(Vector2Int tilePos, Field field)
        {
            // Reverse lookup: find field's chunk coord in spawnedChunks
            foreach (var kvp in spawnedChunks)
            {
                if (kvp.Value == field)
                    return kvp.Key;
            }

            throw new System.Exception("Field not found in spawnedChunks!");
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
        }
    }
}