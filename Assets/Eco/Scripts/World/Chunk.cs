using Eco.Scripts.Pooling;
using UnityEngine;

namespace Eco.Scripts.World
{
    public class Chunk : MonoBehaviour, IPooledObject
    {
        [SerializeField] private ChunkType type;
        protected Tile[] tiles = new Tile[WorldController.ChunkSize * WorldController.ChunkSize];
        protected int ChunkSize;
    
        protected SaveManager SaveManager;
        protected Vector2Int Position;
        public Tile[] Tiles => tiles;
        public ChunkType Type => type;
        protected bool HasSave => SaveManager.FieldTiles.ContainsKey(Position);
        protected int TrashPerChunk;


        public void Setup(Vector2Int position, SaveManager saveManager, int trashPerChunk)
        {
            Position = position;
            SaveManager = saveManager;
            ChunkSize = WorldController.ChunkSize;
            TrashPerChunk = trashPerChunk;
            
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    Tile t = new Tile(new Vector2Int(x, y));
                    tiles[y * ChunkSize + x] = t;
                }
            }
        }
    
        public Vector3 GetTileWorldPosition(Tile tile)
        {
            var pos = new Vector2Int(Mathf.FloorToInt(transform.position.x),
                Mathf.FloorToInt(transform.position.z)) - new Vector2Int(ChunkSize / 2, ChunkSize / 2) + tile.position;
            return new Vector3(pos.x, 0, pos.y);
        }

        public void SaveTiles()
        {
            var saveData = new TileData[tiles.Length];
            for (var i = 0; i < tiles.Length; i++)
            {
                saveData[i] = tiles[i].GetSaveData();
            }

            SaveManager.SaveFieldTiles(Position, saveData);
        }

        public Tile GetTile(Vector2Int position)
        {
            var index = position.x * ChunkSize + position.y;

            if (index >= tiles.Length || index < 0)
            {
                return null;
            }

            return tiles[index];
        }

    
        public void OnSpawn()
        {
        }

        public virtual void OnDespawn()
        {
            foreach (var t in tiles)
            {
                t.Clear();
            }
        }
    }
}
