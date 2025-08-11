using Eco.Scripts;
using Eco.Scripts.Pooling;
using UnityEngine;

public class TreePlanter
{
    public void PlantTree(int prefabId, Field.Tile tile, Field parent)
    {
        Debug.Log("Planting Tree at " + parent + " " + tile.position);

        var tree = PoolManager.Instance.GetTree(prefabId);
        tree.transform.parent = parent.transform;
        tree.transform.position = parent.GetTileWorldPosition(tile);

        tile.item = tree;
        tile.status = Field.TileStatus.Tree;
    }
}
