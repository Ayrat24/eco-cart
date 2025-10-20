using Eco.Scripts.Pooling;
using UnityEngine;

namespace Eco.Scripts.World
{
    public class PileChunk : Chunk
    {
        public void Init()
        {
            bool hasSave = SaveManager.FieldTiles.ContainsKey(Position);

            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    var t = tiles[y * ChunkSize + x];
                    t.groundType = TileGroundType.Ground;

                    if (x == ChunkSize / 2 && y == ChunkSize / 2)
                    {
                        SpawnTrashPileAtTile(t);
                    }
                }
            }
            
            if (!hasSave)
            {
                SaveTiles();
            }
        }

        private void SpawnTrashPileAtTile(Tile tile, int size = 5)
        {
            var tileWorldPosition = GetTileWorldPosition(tile);

            var trash = PoolManager.Instance.GetTrashPile();
            trash.transform.parent = transform;
            trash.transform.position = tileWorldPosition;
            trash.Initialize(tile, size);
            tile.item = trash;
            tile.objectType = TileObjectType.TrashPile;
        }
    }
}