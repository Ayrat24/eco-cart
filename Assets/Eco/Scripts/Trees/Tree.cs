using UnityEngine;

public class Tree : MonoBehaviour, ITileItem
{
    [SerializeField] private int prefabId;

    public int GetPrefabId()
    {
        return prefabId;
    }
}
