using System.Collections.Generic;
using Eco.Scripts.Profiling;
using Eco.Scripts.Pooling;
using Eco.Scripts.Trees;
using R3;
using UnityEngine;
using Tree = Eco.Scripts.Trees.Tree;

namespace Eco.Scripts.World
{
    /// <summary>Plants on existing grass. Planting and drag previews share placement rules.</summary>
    public class TreePlanter
    {
        public const int PileGrassRadius = 6;
        private readonly Transform _player;
        private readonly WorldController _world;
        private readonly FruitGrowth _fruitGrowth;
        private readonly Dictionary<int, TreeDefinition> _definitions = new();
        private readonly HashSet<(Vector2Int chunk, int tile)> _reserved = new();
        public PlacementSpace Placement { get; } = new();
        public readonly Subject<int> OnTreePlanted = new();

        public TreePlanter(Transform player, WorldController world, SaveManager saves, FruitGrowth fruitGrowth)
        {
            _player = player;
            _world = world;
            _fruitGrowth = fruitGrowth;
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
            return TryFindNearest(id, _player.position, out var tile, out _) && PlantNew(id, tile);
        }

        /// <summary>Find the nearest free grass cell to a drop without moving the preview to that cell.</summary>
        public bool TryGetSpot(int id, Vector3 point, out Tile tile, out Vector3 position)
        {
            tile = null;
            position = point;
            // A drop must still be over dry, grown grass; never redirect a drop from water or bare soil.
            var coord = Hex.FromWorld(point, _world.HexRadius);
            if (!_world.ActiveChunks.TryGetValue(coord, out var chunk) || chunk is not LandChunk ||
                !TerrainPainter.IsGrownGrass(point)) return false;
            var local = point - chunk.transform.position;
            int index = chunk.Layout.IndexAt(new Vector2(local.x, local.z));
            if (index < 0 || chunk.Tiles[index].groundType == TileGroundType.Water ||
                chunk.Tiles[index].groundType == TileGroundType.Pile) return false;
            return TryFindNearest(id, point, out tile, out position);
        }

        private bool TryFindNearest(int id, Vector3 point, out Tile tile, out Vector3 position)
        {
            tile = null;
            position = point;
            if (!_definitions.TryGetValue(id, out var definition)) return false;
            float best = float.MaxValue;
            // Seed the distance bound locally so the rest of the world can usually be skipped.
            _world.ActiveChunks.TryGetValue(Hex.FromWorld(point, _world.HexRadius), out var localChunk);
            if (localChunk is LandChunk local) Search(local, definition, point, ref tile, ref best);
            foreach (var chunk in _world.ActiveChunks.Values)
            {
                if (chunk == localChunk || chunk is not LandChunk land) continue;
                var delta = chunk.transform.position - point;
                float nearest = Mathf.Max(0f, Mathf.Sqrt(delta.x * delta.x + delta.z * delta.z) - _world.HexRadius);
                if (nearest * nearest > best) continue;
                Search(land, definition, point, ref tile, ref best);
            }
            if (tile == null) return false;
            position = tile.parentChunk.GetTileWorldPosition(tile);
            return true;
        }

        private void Search(LandChunk chunk, TreeDefinition definition, Vector3 point, ref Tile closest, ref float best)
        {
            foreach (var candidate in chunk.Tiles)
            {
                if (candidate.objectType != TileObjectType.Empty || candidate.groundType == TileGroundType.Water) continue;
                var local = chunk.Layout.LocalPosition(candidate.index);
                var position = chunk.transform.position + new Vector3(local.x, 0, local.y);
                float dx = position.x - point.x;
                float dz = position.z - point.z;
                float distance = dx * dx + dz * dz;
                if (distance >= best || !CanPlant(definition, candidate, position)) continue;
                closest = candidate;
                best = distance;
            }
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
            var tree = PlaceTree(id, tile, tile.parentChunk);
            if (_definitions.TryGetValue(id, out var definition))
            {
                tree.ShowPlantAnimation(definition.GrowthSettings);
                // Up to half a fruit's head start, so trees planted together don't bear in step
                if (tree.Bearer != null)
                    tree.Bearer.Begin(definition, _fruitGrowth, 0, _fruitGrowth.GetClock(definition) - Random.value * 0.5f);
            }
            tile.parentChunk.SaveTiles();
            OnTreePlanted.OnNext(id);
            return true;
        }

        // Save restoration bypasses grass/unlock/cost checks; old trees remain in place.
        public void PlantTree(int prefabId, Tile tile, Chunk parent, TileData saved)
        {
            var tree = PlaceTree(prefabId, tile, parent);
            if (tree.Bearer != null && _definitions.TryGetValue(prefabId, out var definition))
                tree.Bearer.Begin(definition, _fruitGrowth, saved.fruits, saved.fruitCycle);
        }

        private Tree PlaceTree(int prefabId, Tile tile, Chunk parent)
        {
            var tree = PoolManager.Instance.GetTree(prefabId);
            tree.transform.parent = parent.transform;
            tree.transform.position = parent.GetTileWorldPosition(tile);
            tile.item = tree;
            tile.objectType = TileObjectType.Tree;
            Reserve(Hex.FromWorld(parent.transform.position, _world.HexRadius), tile.index,
                tree.transform.position, prefabId);
            return tree;
        }

        private void Reserve(Vector2Int chunk, int tile, Vector3 position, int id)
        {
            if (_reserved.Add((chunk, tile)) && _definitions.TryGetValue(id, out var definition))
                Placement.Reserve(position, definition.PlacementRadius);
        }
    }
}
