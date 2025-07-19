using System;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using Random = UnityEngine.Random;

public class Field : MonoBehaviour
{
    [SerializeField] private List<GameObject> trashPrefabs;
    [SerializeField] private readonly List<Tile> _tiles = new();

    [SerializeField] private int fieldSize = 20;

    private readonly Dictionary<GameObject, LeanGameObjectPool> _pools = new();

    private void Awake()
    {
        foreach (var prefab in trashPrefabs)
        {
            var pool = gameObject.AddComponent<LeanGameObjectPool>();
            pool.Prefab = prefab.gameObject;
            _pools.Add(prefab, pool);
        }
        
        Init();
    }

    public void Init()
    {
        for (int x = 0; x < fieldSize; x++)
        {
            for (int y = 0; y < fieldSize; y++)
            {
                Tile tile = new Tile(new Vector3Int(x, 0, y));

                bool occupied = Random.Range(0, 10) == 0;

                if (occupied)
                {
                    tile.trash = Spawn(trashPrefabs[Random.Range(0, trashPrefabs.Count)], tile.position);
                    tile.occupied = true;
                }
                
                _tiles.Add(tile);
            }
        }
    }

    private void Start()
    {
    }

    private GameObject Spawn(GameObject prefab, Vector3 position)
    {
        position.y = 0.5f;
        var trash = _pools[prefab].Spawn(position, Random.rotation, transform);
        return trash;
    }

    [System.Serializable]
    private class Tile
    {
        public bool occupied;
        public GameObject trash;
        public Vector3Int position;

        public Tile(Vector3Int position)
        {
            this.position = position;
        }
    }
}