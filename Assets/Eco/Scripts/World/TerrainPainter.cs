using UnityEngine;

public class TerrainPainter
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
    }

    public static void PaintTerrainTexture(TerrainTexture texture, Vector3 position)
    {
        PaintTexture(ConvertWordCor2TerrCor(position), (int)texture);
    }

    private static Vector3Int ConvertWordCor2TerrCor(Vector3 wordCor)
    {
        Vector3Int vecRet = new();
        Terrain ter = Terrain.activeTerrain;
        Vector3 terPosition = ter.transform.position;
        vecRet.x = (int)(((wordCor.x - terPosition.x) / ter.terrainData.size.x) * ter.terrainData.alphamapWidth);
        vecRet.z = (int)(((wordCor.z - terPosition.z) / ter.terrainData.size.z) * ter.terrainData.alphamapHeight);
        return vecRet;
    }

    private static void PaintTexture(Vector3Int pos, int layerIndex)
    {
        TerrainData data = Terrain.activeTerrain.terrainData;

        int sqr = 1;
        float[,,] splatmap = data.GetAlphamaps(pos.x, pos.z, sqr, sqr);

        // Clear all layers
        // for (int i = 0; i < data.alphamapLayers; i++)
        //     splatmap[0, 0, i] = 0;
        for (int i = 0; i < data.alphamapLayers; i++)
        {
            for (int x = 0; x < sqr; x++)
            {
                for (int z = 0; z < sqr; z++)
                {
                    splatmap[x, z, i] = 0;
                }
            }
        }

        //Set desired layer
        for (int x = 0; x < sqr; x++)
        {
            for (int z = 0; z < sqr; z++)
            {
                splatmap[x, z, layerIndex] = 1;
            }
        }

        data.SetAlphamaps(pos.x, pos.z, splatmap);
    }

    public enum TerrainTexture
    {
        Sand,
        Grass
    }
}