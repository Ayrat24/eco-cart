using Bitgem.VFX.StylisedWater;
using UnityEngine;

namespace Eco.Scripts.World
{
    public class WaterChunk : Chunk
    {
        [SerializeField] private bool enableFloating;
        [SerializeField] private WaterVolumeTransforms water;
        [SerializeField] private WaterVolumeHelper waterVolumeHelper;

        public void Init()
        {
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    Tile t = tiles[y * ChunkSize + x];
                    t.groundType = TileGroundType.Water;
                }
            }
            
            //to show water on the map
            if(!HasSave)
            {
                SaveTiles();
            }
        }
        
        public void UpdateWaterCorners(int worldSize, Vector2Int position)
        {
            WaterVolumeBase.TileFace faces = 0;

            if (Mathf.Abs(position.x * position.y) >= Mathf.Pow(worldSize + 1, 2))
            {
                faces = 0;
            }
            else
            {
                if (position.x == -worldSize - 1)
                {
                    faces = WaterVolumeBase.TileFace.PosX;
                }

                if (position.x == worldSize + 1)
                {
                    faces = WaterVolumeBase.TileFace.NegX;
                }

                if (position.y == worldSize + 1)
                {
                    faces = WaterVolumeBase.TileFace.NegZ;
                }

                if (position.y == -worldSize - 1)
                {
                    faces = WaterVolumeBase.TileFace.PosZ;
                }
            }


            if (water.IncludeFoam != faces)
            {
                water.IncludeFoam = faces;
                water.Rebuild();
            }
        }
    }
}