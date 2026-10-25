using Eco.Scripts.Pooling;
using UnityEngine;
using UnityEngine.Serialization;

namespace Eco.Scripts.World
{
    [System.Serializable]
    public class Tile
    {
        public ITileItem item;
        public Vector2Int position;
        public TileObjectType objectType = TileObjectType.Empty;
        public TileGroundType groundType = TileGroundType.Ground;
        public bool containedScrap;
        public Chunk parentChunk;
        
        public Tile(Vector2Int position)
        {
            this.position = position;
        }

        public TileData GetSaveData()
        {
            var data = new TileData
            {
                objectType = (int)objectType,
                containedScrap = containedScrap,
                clean = (objectType is TileObjectType.Empty or TileObjectType.Tree) && containedScrap
            };

            if (item != null)
            {
                data.objectId = item.GetPrefabId();
            }
            
            data.ground = (int)groundType;

            return data;
        }

        public void Clear()
        {
            if (item is { CanBeRecycled: true })
            {
                PoolManager.Instance.ReturnItem(item);
            }
        }
    }

    public enum TileObjectType
    {
        Empty,
        Scrap,
        Tree,
        ScrapPile
    }

    public enum TileGroundType
    {
        Ground,
        Grass,
        Water,
        Pile
    }
}