using System;
using System.Collections.Generic;
using System.Linq;
using Eco.Scripts.Pooling;
using Eco.Scripts.Trees;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Eco.Scripts.World
{
    public class TreePlanter
    {
        public const int TreeGrassRadius = 12;
        
        private readonly Transform _player;
        private readonly UpgradesCollection _upgrades;
        private readonly WorldController _worldController;
        private IDisposable _subscription;

        public Subject<int> OnTreePlanted = new();
        
        public TreePlanter(UpgradesCollection upgrades, Transform player, WorldController worldController)
        {
            _upgrades = upgrades;
            _player = player;
            _worldController = worldController;
        }

        public void Init()
        {
            DisposableBuilder builder = new DisposableBuilder();

            foreach (var tree in _upgrades.GetUpgradeTypes<TreeBuyUpgrade>())
            {
                tree.OnPurchase.Subscribe(PlantTree).AddTo(ref builder);
            }

            _subscription = builder.Build();
        }

        private void PlantTree(int id)
        {
            Vector3 playerPos = _player.position; // Player world position
            Vector2Int playerChunkCoord = _worldController.GetPlayerChunkCoord();
            int chunkSize = 10;

            Field closestField = null;
            Tile closestTile = null;
            float closestDistSqr = float.MaxValue;

            Dictionary<Vector2Int, Field> groundChunks = _worldController.ActiveChunks.Where(x => x.Value is not WaterField)
                .ToDictionary(x => x.Key, x => x.Value);

            // Search in surrounding chunks (adjust search range if needed)
            for (int cx = -_worldController.RenderRadius; cx <= _worldController.RenderRadius; cx++)
            {
                for (int cy = -_worldController.RenderRadius; cy <= _worldController.RenderRadius; cy++)
                {
                    Vector2Int chunkCoord = new Vector2Int(playerChunkCoord.x + cx, playerChunkCoord.y + cy);

                    if (groundChunks.TryGetValue(chunkCoord, out Field field))
                    {
                        foreach (var tile in field.Tiles)
                        {
                            if (tile.objectType == TileObjectType.Empty && tile.groundType == TileGroundType.Ground)
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

            if (closestTile == null)
            {
                return;
            }
            
            PlantTree(id, closestTile, closestField);
            MarkGroundCircle(closestTile, closestField, TreeGrassRadius);
            OnTreePlanted.OnNext(id);
        }

        private void MarkGroundCircle(Tile centerTile, Field centerField, int radius)
        {
            int chunkSize = _worldController.ChunkSize;

            // Center tile's chunk coordinate in chunk grid
            Vector2Int centerChunkCoord = GetChunkCoordFromTile(centerField);
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

                        if (_worldController.ActiveChunks.TryGetValue(targetChunkCoord, out Field targetField))
                        {
                            // Local position inside the target chunk
                            Vector2Int localPos = new Vector2Int(
                                Mod(globalPos.x, chunkSize),
                                Mod(globalPos.y, chunkSize)
                            );

                            var adjTile = targetField.GetTile(localPos);
                            if (adjTile != null)
                            {
                                adjTile.groundType = TileGroundType.Grass;
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

        private Vector2Int GetChunkCoordFromTile(Field field)
        {
            // Reverse lookup: find field's chunk coord in spawnedChunks
            foreach (var kvp in _worldController.ActiveChunks)
            {
                if (kvp.Value == field)
                    return kvp.Key;
            }

            throw new System.Exception("Field not found in spawnedChunks!");
        }
        
        private int Mod(int a, int m) => (a % m + m) % m;

        public void PlantTree(int prefabId, Tile tile, Field parent)
        {
            var tree = PoolManager.Instance.GetTree(prefabId);
            tree.transform.parent = parent.transform;
            tree.transform.position = parent.GetTileWorldPosition(tile);

            tile.item = tree;
            tile.objectType = TileObjectType.Tree;
        }
        
        public void Clear()
        {
            _subscription?.Dispose();
        }
    }
}