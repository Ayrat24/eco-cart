using System.Collections.Generic;
using Eco.Scripts.Profiling;
using Eco.Scripts.Pooling;
using Eco.Scripts.Trees;
using R3;
using UnityEngine;

namespace Eco.Scripts.World
{
    /// <summary>Plants on existing grass. Planting and drag previews share placement rules.</summary>
    public class TreePlanter
    {
        public const int PileGrassRadius = 6;
        private readonly Transform _player;
        private readonly WorldController _world;
        private readonly Dictionary<int, TreeDefinition> _definitions = new();
        private readonly HashSet<(Vector2Int chunk, int tile)> _reserved = new();
        public PlacementSpace Placement { get; } = new();
        public readonly Subject<int> OnTreePlanted = new();

        public TreePlanter(Transform player, WorldController world, SaveManager saves)
        {
            _player = player;
            _world = world;
            foreach (var definition in world.Trees)
            {
                if (definition != null) _definitions[definition.PrefabId] = definition;
            }
            // Include unloaded neighbours so streaming never permits overlapping reservations.
            var layout = HexTileLayout.Get(world.HexRadius);
            foreach (var pair in saves.Chunks)
            {
                var definition = world.WorldData.GetChunk(pair.Key);
                if (definition == null || definition.Id != pair.Value.chunk ||
                    pair.Value.tiles == null || pair.Value.tiles.Length != layout.Count) continue;
                for (int i = 0; i < layout.Count; i++)
                {
                    var tile = pair.Value.tiles[i];
                    if (tile.objectType != (int)TileObjectType.Tree) continue;
                    var local = layout.LocalPosition(i);
                    var position = Hex.ToWorld(pair.Key, world.HexRadius) + new Vector3(local.x, 0, local.y);
                    Reserve(pair.Key, i, position, tile.objectId);
                }
            }
        }

        public bool PlantTree(int id)
        {
            using var marker = EcoProfilerMarkers.PlantTree.Auto();
            if (!_definitions.TryGetValue(id, out var definition)) return false;
            Tile closest = null;
            float best = float.MaxValue;
            foreach (var chunk in _world.ActiveChunks.Values)
            {
                if (chunk is not LandChunk) continue;
                foreach (var tile in chunk.Tiles)
                {
                    if (tile.objectType != TileObjectType.Empty || tile.groundType == TileGroundType.Water) continue;
                    var local = chunk.Layout.LocalPosition(tile.index);
                    var position = chunk.transform.position + new Vector3(local.x, 0, local.y);
                    float dx = position.x - _player.position.x;
                    float dz = position.z - _player.position.z;
                    float distance = dx * dx + dz * dz;
                    if (distance >= best || !CanPlant(definition, tile, position)) continue;
                    closest = tile;
                    best = distance;
                }
            }
            return closest != null && PlantNew(id, closest);
        }

        public bool TryGetSpot(int id, Vector3 point, out Tile tile, out Vector3 position)
        {
            tile = null;
            position = point;
            if (!_definitions.TryGetValue(id, out var definition)) return false;
            var coord = Hex.FromWorld(point, _world.HexRadius);
            if (!_world.ActiveChunks.TryGetValue(coord, out var chunk) || chunk is not LandChunk) return false;
            var local = point - chunk.transform.position;
            int index = chunk.Layout.IndexAt(new Vector2(local.x, local.z));
            if (index < 0) return false;
            tile = chunk.Tiles[index];
            position = chunk.GetTileWorldPosition(tile);
            return CanPlant(definition, tile, position);
        }

        public bool PlantTreeAt(int id, Vector3 point)
        {
            using var marker = EcoProfilerMarkers.PlantTree.Auto();
            return TryGetSpot(id, point, out var tile, out _) && PlantNew(id, tile);
        }

        private bool CanPlant(TreeDefinition definition, Tile tile, Vector3 position)
        {
            return tile.objectType == TileObjectType.Empty && tile.groundType != TileGroundType.Water &&
                   tile.groundType != TileGroundType.Pile && TerrainPainter.IsGrownGrass(position) &&
                   Placement.CanPlace(position, definition.PlacementRadius);
        }

        private bool PlantNew(int id, Tile tile)
        {
            PlantTree(id, tile, tile.parentChunk, true);
            tile.parentChunk.SaveTiles();
            OnTreePlanted.OnNext(id);
            return true;
        }

        // Save restoration bypasses grass/unlock/cost checks; old trees remain in place.
        public void PlantTree(int prefabId, Tile tile, Chunk parent, bool isNewlyPlanted = false)
        {
            var tree = PoolManager.Instance.GetTree(prefabId);
            tree.transform.parent = parent.transform;
            tree.transform.position = parent.GetTileWorldPosition(tile);
            tile.item = tree;
            tile.objectType = TileObjectType.Tree;
            Reserve(Hex.FromWorld(parent.transform.position, _world.HexRadius), tile.index,
                tree.transform.position, prefabId);
            if (isNewlyPlanted) tree.ShowPlantAnimation();
        }

        private void Reserve(Vector2Int chunk, int tile, Vector3 position, int id)
        {
            if (_reserved.Add((chunk, tile)) && _definitions.TryGetValue(id, out var definition))
                Placement.Reserve(position, definition.PlacementRadius);
        }
    }
}
