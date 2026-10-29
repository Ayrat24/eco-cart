using Eco.Scripts.Pooling;

namespace Eco.Scripts.World
{
    [System.Serializable]
    public class Tile
    {
        public ITileItem item;

        /// <summary>Index in the chunk's <see cref="HexTileLayout"/> and saved tile array.</summary>
        public int index;
        public TileObjectType objectType = TileObjectType.Empty;
        public TileGroundType groundType = TileGroundType.Ground;
        public bool containedScrap;
        public Chunk parentChunk;

        public Tile(int index)
        {
            this.index = index;
        }

        public TileData GetSaveData()
        {
            var data = new TileData
            {
                objectType = (int)objectType,
                containedScrap = containedScrap
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
