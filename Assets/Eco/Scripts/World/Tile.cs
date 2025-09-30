using Eco.Scripts.Pooling;
using UnityEngine;

namespace Eco.Scripts.World
{
    [System.Serializable]
    public class Tile
    {
        public ITileItem item;
        public Vector2Int position;
        public TileStatus status = TileStatus.Empty;

        public Tile(Vector2Int position)
        {
            this.position = position;
        }

        public TileData GetSaveData()
        {
            var data = new TileData
            {
                state = (int)status
            };

            if (item != null)
            {
                data.data = item.GetPrefabId();
            }

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

    public enum TileStatus
    {
        Empty,
        Trash,
        Ground,
        Tree
    }
}