using System.Collections.Generic;
using Eco.Scripts.World;
using UnityEngine;

namespace Eco.Scripts.Profiling
{
    /// <summary>
    /// Writes the stress world's save through <see cref="SaveManager"/> (into <see cref="StressTest.SaveDirectory"/>),
    /// so the game loads it through the normal FieldChunk.LoadSave path: every field chunk gets scrap, trees on a grid
    /// and green ground. Pile, beach and water chunks are left unsaved and generate as usual.
    /// </summary>
    public static class StressTestSeeder
    {
        public static string Seed(StressTestConfig config, WorldPreset preset)
        {
            var saveManager = new SaveManager();
            saveManager.Load(preset.WorldId);

            preset.GenerateMap();

            var random = new System.Random(config.Seed);
            var scrapIds = new List<int>();
            foreach (var scrap in preset.AllowedScrapItems)
            {
                scrapIds.Add(scrap.GetPrefabId());
            }

            var treeIds = new List<int>();
            foreach (var tree in config.TreePrefabs)
            {
                treeIds.Add(tree.GetPrefabId());
            }

            const int chunkSize = WorldController.ChunkSize;
            int side = preset.WorldSideSize;
            int spacing = config.TreeSpacing;
            int fieldChunks = 0, scrapCount = 0, treeCount = 0;
            var freeTiles = new List<int>(chunkSize * chunkSize);

            for (int cx = -side; cx <= side; cx++)
            {
                for (int cy = -side; cy <= side; cy++)
                {
                    var coord = new Vector2Int(cx, cy);
                    if (preset.GetChunkType(coord) != ChunkType.Field)
                    {
                        continue;
                    }

                    bool inClearing = Mathf.Abs(cx) <= config.ClearingRadiusChunks &&
                                      Mathf.Abs(cy) <= config.ClearingRadiusChunks;
                    var ground = inClearing ? TileGroundType.Ground : TileGroundType.Grass;
                    var tiles = new TileData[chunkSize * chunkSize];
                    freeTiles.Clear();

                    for (int y = 0; y < chunkSize; y++)
                    {
                        for (int x = 0; x < chunkSize; x++)
                        {
                            int index = y * chunkSize + x;
                            tiles[index].ground = (int)ground;

                            bool onTreeGrid = Mod(cx * chunkSize + x, spacing) == spacing / 2 &&
                                              Mod(cy * chunkSize + y, spacing) == spacing / 2;

                            if (!inClearing && onTreeGrid && treeIds.Count > 0)
                            {
                                tiles[index].objectType = (int)TileObjectType.Tree;
                                tiles[index].objectId = treeIds[random.Next(treeIds.Count)];
                                tiles[index].containedScrap = true;
                                tiles[index].clean = true;
                                treeCount++;
                            }
                            else
                            {
                                freeTiles.Add(index);
                            }
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

                    saveManager.SaveFieldTiles(coord, tiles);
                    fieldChunks++;
                }
            }

            saveManager.Save();

            return $"{fieldChunks} field chunks, {scrapCount} scrap, {treeCount} trees " +
                   $"(world {preset.WorldSideSize * 2 + 1}x{preset.WorldSideSize * 2 + 1} chunks)";
        }

        private static int Mod(int a, int m) => (a % m + m) % m;
    }
}
