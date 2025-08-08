using System.Collections.Generic;
using Eco.Scripts.Pooling;
using Eco.Scripts.Trash;
using UnityEngine;
using Random = UnityEngine.Random;

public class Field : MonoBehaviour, IPooledObject
{
    [SerializeField] private List<GameObject> trashPrefabs;
    [SerializeField] private readonly List<Tile> _tiles = new();

    [SerializeField] private int fieldSize = 20;

    public Terrain terrain;
    private SaveManager _saveManager;
    private Vector2Int _position;

    private void Awake()
    {
        // terrain = Terrain.activeTerrain;
        // PaintTexture(ConvertWordCor2TerrCor(transform.position), 1);
    }

    public void Init(Vector2Int position, SaveManager saveManager)
    {
        _saveManager = saveManager;
        _position = position;
        
        bool hasSave = saveManager.FieldTiles.ContainsKey(position);
        
        for (int x = 0; x < fieldSize; x++)
        {
            for (int y = 0; y < fieldSize; y++)
            {
                Tile tile = new Tile(new Vector3Int(x, 0, y));

                if(!hasSave)
                {
                    bool occupied = Random.Range(0, 10) == 0;

                    if (occupied)
                    {
                        SpawnTrashAtTile(tile);
                    }
                }
                else
                {
                    var savedData = saveManager.FieldTiles[position][y * fieldSize + x];
                    var tileStatus = (TileStatus)savedData;

                    switch (tileStatus)
                    {
                        case TileStatus.Trash: 
                            SpawnTrashAtTile(tile);
                            break;
                    }
                }

                _tiles.Add(tile);
            }
        }
        
        if(!hasSave)
        {
            SaveTiles();
        }
    }

    private void SpawnTrashAtTile(Tile tile)
    {
        var localPos =
            new Vector3Int(Mathf.FloorToInt(transform.position.x), 0,
                Mathf.FloorToInt(transform.position.z)) - new Vector3Int(5, 0, 5) + tile.position;

        var trash = PoolManager.Instance.GetTrash();
        trash.transform.parent = transform;
        trash.transform.position = localPos;
        trash.Initialize(tile);
        tile.trash = trash;
        tile.status = TileStatus.Trash;
    }

    public void SaveTiles()
    {
        var saveData = new int[_tiles.Count];
        for (var i = 0; i < _tiles.Count; i++)
        {
            saveData[i] = (int)_tiles[i].status;
        }
        
        _saveManager.SaveFieldTiles(_position, saveData);
    }
    
    private Vector3Int ConvertWordCor2TerrCor(Vector3 wordCor)
    {
        Vector3Int vecRet = new();
        Terrain ter = Terrain.activeTerrain;
        Vector3 terPosition = ter.transform.position;
        vecRet.x = (int)(((wordCor.x - terPosition.x) / ter.terrainData.size.x) * ter.terrainData.alphamapWidth);
        vecRet.z = (int)(((wordCor.z - terPosition.z) / ter.terrainData.size.z) * ter.terrainData.alphamapHeight);
        return vecRet;
    }
    
    public void PaintTexture(Vector3Int pos, int layerIndex)
    {
        TerrainData data = terrain.terrainData;

        int sqr = 2;
        float[,,] splatmap = data.GetAlphamaps(pos.x, pos.z, sqr, sqr);

        // Clear all layers
        // for (int i = 0; i < data.alphamapLayers; i++)
        //     splatmap[0, 0, i] = 0;
        for (int i = 0; i < data.alphamapLayers; i++)
        {
            for (int x = 0; x < sqr; x++)
            {
                for (int z = 0; z < sqr; z++)
                {
                    splatmap[x, z, layerIndex] = 0;
                }
            }
        }

        //Set desired layer
        for (int x = 0; x < sqr; x++)
        {
            for (int z = 0; z < sqr; z++)
            {
                splatmap[x, z, layerIndex] = 1;
            }
        }

        data.SetAlphamaps(pos.x, pos.z, splatmap);
    }

    [System.Serializable]
    public class Tile
    {
        public bool occupied;
        public TrashItem trash;
        public Vector3Int position;
        public TileStatus status = TileStatus.Empty;

        public Tile(Vector3Int position)
        {
            this.position = position;
        }

        public void Clear()
        {
            if (trash != null)
            {
                PoolManager.Instance.ReturnTrash(trash);
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

    public void OnSpawn()
    {
    }

    public void OnDespawn()
    {
        SaveTiles();
        foreach (var t in _tiles)
        {
            t.Clear();
        }
        
        _tiles.Clear();
    }
}