using Eco.Scripts;
using UnityEngine;

public class TreePlanter
{
    public void PlantTree(GameObject prefab, Field.Tile tile, Transform parent)
    {
        Debug.Log("Planting Tree at " + parent + " " + tile.position);
        Object.Instantiate(prefab, parent.position + tile.position, Quaternion.identity, parent);
        tile.status = Field.TileStatus.Tree;
    }
}
