using System.Collections.Generic;
using UnityEngine;

namespace Eco.Scripts.World
{
    public abstract class TerrainPainter
    {
        public static void ClearTerrain()
        {
            var data = Terrain.activeTerrain.terrainData;
            float[,,] splatmap = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);

            for (int x = 0; x < data.alphamapWidth; x++)
            {
                for (int z = 0; z < data.alphamapHeight; z++)
                {
                    splatmap[x, z, 1] = 0;
                    splatmap[x, z, 0] = 1;
                }
            }

            data.SetAlphamaps(0, 0, splatmap);

            int[,] detailLayer = data.GetDetailLayer(
                0,
                0,
                data.detailWidth,
                data.detailHeight,
                0
            );

            for (int x = 0; x < data.detailWidth; x++)
            {
                for (int z = 0; z < data.detailHeight; z++)
                {
                    detailLayer[x, z] = 0;
                }
            }

            data.SetDetailLayer(0, 0, 0, detailLayer);
        }

        public static void PaintTerrainTexture(TerrainTexture texture, Vector3 worldPosition, int radius)
        {
            Terrain terrain = Terrain.activeTerrain;
            TerrainData data = terrain.terrainData;
            int layerIndex = (int)texture;

            // Convert to splatmap coords and find bounds
            var initialPoint = ConvertWorldCor2TerrCor(worldPosition - Vector3.one * radius);


            // Get only the affected region
            float[,,] splatmap = data.GetAlphamaps(initialPoint.x, initialPoint.z, radius * 2 + 2, radius * 2 + 2);

            splatmap[0, 0, layerIndex] = 1;

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        for (int l = 0; l < data.alphamapLayers; l++)
                        {
                            splatmap[x + radius, y + radius, l] = (l == layerIndex) ? 1f : 0f;
                        }
                    }
                }
            }

            // Apply back
            data.SetAlphamaps(initialPoint.x, initialPoint.z, splatmap);

            switch (Settings.DetailQuality)
            {
                case Settings.DetailQualities.Low:
                    break;
                case Settings.DetailQualities.Medium:
                    AddGrass(worldPosition, 0, radius - 2, 128);
                    break;
                case Settings.DetailQualities.High:
                    AddGrass(worldPosition, 0, radius - 2, 256);
                    break;
            }
        }

        private static void AddGrass(Vector3 worldPos, int layer, int radius, int density)
        {
            TerrainData tData = Terrain.activeTerrain.terrainData;

            // Convert to detail map position
            Vector3 terrainPos = worldPos - Terrain.activeTerrain.transform.position - Vector3.one * radius / 2;
            int mapX = Mathf.RoundToInt((terrainPos.x / tData.size.x) * tData.detailWidth);
            int mapZ = Mathf.RoundToInt((terrainPos.z / tData.size.z) * tData.detailHeight);

            // Get current detail layer
            int[,] detailLayer = tData.GetDetailLayer(
                mapX - radius / 2,
                mapZ - radius / 2,
                radius * 2 + 2,
                radius * 2 + 2,
                layer
            );

            // Fill with grass
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        detailLayer[y + radius, x + radius] = density; // Higher = more grass
                    }
                }
            }

            // Apply
            tData.SetDetailLayer(
                mapX - radius / 2,
                mapZ - radius / 2,
                layer,
                detailLayer
            );
        }

        private static Vector3Int ConvertWorldCor2TerrCor(Vector3 worldCor)
        {
            Terrain ter = Terrain.activeTerrain;
            Vector3 terPosition = ter.transform.position;

            return new Vector3Int(
                (int)(((worldCor.x - terPosition.x) / ter.terrainData.size.x) * ter.terrainData.alphamapWidth),
                0,
                (int)(((worldCor.z - terPosition.z) / ter.terrainData.size.z) * ter.terrainData.alphamapHeight)
            );
        }

        public enum TerrainTexture
        {
            Sand,
            Grass
        }
    }
}