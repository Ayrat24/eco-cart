using System.Collections.Generic;
using Eco.Scripts.World;
using UnityEngine;

namespace Eco.Scripts.Profiling
{
    /// <summary>
    /// Writes the stress world's save through <see cref="SaveManager"/> (into <see cref="StressTest.SaveDirectory"/>),
    /// so the game loads it through the normal LandChunk load path: every land chunk without a pile gets scrap,
    /// trees on a grid and green ground. Pile and water chunks are left unsaved and generate as usual.
    /// </summary>
    public static class StressTestSeeder
    {
        public static string Seed(StressTestConfig config)
        {
            var world = config.World;
            var saveManager = new SaveManager();
            saveManager.Load();

            var random = new System.Random(config.Seed);
            var scrapIds = new List<int>();
            foreach (var scrap in world.ScrapItems)
            {
                scrapIds.Add(scrap.GetPrefabId());
            }

            var treeIds = new List<int>();
            foreach (var tree in config.TreePrefabs)
            {
                treeIds.Add(tree.GetPrefabId());
            }

            var layout = HexTileLayout.Get(world.HexRadius);
            var spawn = world.GetSpawnCoord();
            int spacing = config.TreeSpacing;
            int landChunks = 0, scrapCount = 0, treeCount = 0;
            var freeTiles = new List<int>(layout.Count);

            foreach (var cell in world.Cells)
            {
                var chunk = cell.chunk;
                if (chunk == null || chunk.IsWater || chunk.Scrap.HasPile)
                {
                    continue;
                }

                bool inClearing = Hex.Distance(cell.coord, spawn) <= config.ClearingRadiusChunks;
                var ground = inClearing ? TileGroundType.Ground : TileGroundType.Grass;
                var center = Hex.ToPlane(cell.coord, world.HexRadius);
                var tiles = new TileData[layout.Count];
                freeTiles.Clear();

                for (int index = 0; index < tiles.Length; index++)
                {
                    tiles[index].ground = (int)ground;

                    var position = center + layout.LocalPosition(index);
                    bool onTreeGrid = Mod(Mathf.FloorToInt(position.x), spacing) == spacing / 2 &&
                                      Mod(Mathf.FloorToInt(position.y), spacing) == spacing / 2;

                    if (!inClearing && onTreeGrid && treeIds.Count > 0)
                    {
                        tiles[index].objectType = (int)TileObjectType.Tree;
                        tiles[index].objectId = treeIds[random.Next(treeIds.Count)];
                        tiles[index].containedScrap = true;
                        treeCount++;
                    }
                    else
                    {
                        freeTiles.Add(index);
                    }
                }

                int scrapInChunk = Mathf.Min(config.ScrapPerChunk, freeTiles.Count);
                for (int i = 0; i < scrapInChunk && scrapIds.Count > 0; i++)
                {
                    // Partial Fisher-Yates: pick a random remaining free tile.
                    int pick = random.Next(i, freeTiles.Count);
                    (freeTiles[i], freeTiles[pick]) = (freeTiles[pick], freeTiles[i]);

                    int index = freeTiles[i];
                    tiles[index].objectType = (int)TileObjectType.Scrap;
                    tiles[index].objectId = scrapIds[random.Next(scrapIds.Count)];
                    scrapCount++;
                }

                saveManager.SaveChunk(cell.coord, new ChunkSaveData { chunk = chunk.Id, tiles = tiles });
                landChunks++;
            }

            saveManager.Save();

            return $"{landChunks} land chunks, {scrapCount} scrap, {treeCount} trees " +
                   $"(world of {world.Cells.Count} painted hexes, map radius {world.MapRadius})";
        }

        private static int Mod(int a, int m) => (a % m + m) % m;
    }
}
