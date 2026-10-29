using Eco.Scripts.Profiling;
using Eco.Scripts.Pooling;
using R3;
using UnityEngine;

namespace Eco.Scripts.World
{
    /// <summary>Plants trees on free ground near the player and greens the ground around them.</summary>
    public class TreePlanter
    {
        public const int TreeGrassRadius = 12;
        public const int PileGrassRadius = 6;

        private readonly Transform _player;
        private readonly WorldController _worldController;

        public readonly Subject<int> OnTreePlanted = new();

        public TreePlanter(Transform player, WorldController worldController)
        {
            _player = player;
            _worldController = worldController;
        }

        /// <summary>Plants tree prefab <paramref name="id"/> on the free ground tile closest to the player.</summary>
        /// <returns>False if no free tile is in range.</returns>
        public bool PlantTree(int id)
        {
            using var marker = EcoProfilerMarkers.PlantTree.Auto();

            Vector3 playerPos = _player.position;
            LandChunk closestChunk = null;
            Tile closestTile = null;
            float closestDistSqr = float.MaxValue;

            foreach (var chunk in _worldController.ActiveChunks.Values)
            {
                if (chunk is not LandChunk land)
                {
                    continue;
                }

                foreach (var tile in land.Tiles)
                {
                    if (tile.objectType != TileObjectType.Empty || tile.groundType != TileGroundType.Ground)
                    {
                        continue;
                    }

                    var local = land.Layout.LocalPosition(tile.index);
                    var center = land.transform.position;
                    float dx = center.x + local.x - playerPos.x;
                    float dz = center.z + local.y - playerPos.z;
                    float distSqr = dx * dx + dz * dz;
                    if (distSqr < closestDistSqr)
                    {
                        closestDistSqr = distSqr;
                        closestTile = tile;
                        closestChunk = land;
                    }
                }
            }

            if (closestTile == null)
            {
                return false;
            }

            PlantTree(id, closestTile, closestChunk, true);
            MarkGroundCircle(closestChunk.GetTileWorldPosition(closestTile), TreeGrassRadius);
            OnTreePlanted.OnNext(id);
            return true;
        }

        // Every bare land tile within the radius turns to grass, across chunk borders
        private void MarkGroundCircle(Vector3 center, int radius)
        {
            float reach = radius + _worldController.HexRadius;
            foreach (var chunk in _worldController.ActiveChunks.Values)
            {
                if (chunk is not LandChunk land)
                {
                    continue;
                }

                var chunkCenter = land.transform.position;
                float cx = chunkCenter.x - center.x;
                float cz = chunkCenter.z - center.z;
                if (cx * cx + cz * cz > reach * reach)
                {
                    continue;
                }

                bool changed = false;
                foreach (var tile in land.Tiles)
                {
                    var local = land.Layout.LocalPosition(tile.index);
                    float dx = cx + local.x;
                    float dz = cz + local.y;
                    if (dx * dx + dz * dz <= radius * radius && tile.groundType == TileGroundType.Ground)
                    {
                        tile.groundType = TileGroundType.Grass;
                        changed = true;
                    }
                }

                if (changed)
                {
                    land.MakeGrass();
                }
            }
        }

        public void PlantTree(int prefabId, Tile tile, Chunk parent, bool isNewlyPlanted = false)
        {
            var tree = PoolManager.Instance.GetTree(prefabId);
            tree.transform.parent = parent.transform;
            tree.transform.position = parent.GetTileWorldPosition(tile);

            tile.item = tree;
            tile.objectType = TileObjectType.Tree;

            if (isNewlyPlanted)
            {
                tree.ShowPlantAnimation();
            }
        }
    }
}
