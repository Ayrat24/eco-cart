using System.Collections.Generic;
using Eco.Scripts.Pooling;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Eco.Scripts.World
{
    public class Field : MonoBehaviour, IPooledObject
    {
        [SerializeField] private List<GameObject> trashPrefabs;
        [SerializeField] private List<Tile> _tiles = new();

        [SerializeField] private int fieldSize = 20;

        public Terrain terrain;
        private SaveManager _saveManager;
        private Vector2Int _position;
        private TreePlanter _treePlanter;

        public List<Tile> Tiles => _tiles;
        private GUIStyle style;
        private void Awake()
        {
            // terrain = Terrain.activeTerrain;
            // PaintTexture(ConvertWordCor2TerrCor(transform.position), 1);
        }

        public void Init(Vector2Int position, SaveManager saveManager, TreePlanter treePlanter)
        {
            _saveManager = saveManager;
            _position = position;
            _treePlanter = treePlanter;

            style = new GUIStyle
            {
                normal =
                {
                    textColor = Color.magenta
                },
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            
            bool hasSave = saveManager.FieldTiles.ContainsKey(position);

            for (int x = 0; x < fieldSize; x++)
            {
                for (int y = 0; y < fieldSize; y++)
                {
                    Tile tile = new Tile(new Vector2Int(x, y));

                    if (!hasSave)
                    {
                        bool occupied = Random.Range(0, 10) == 0;

                        if (occupied)
                        {
                            SpawnTrashAtTile(tile);
                        }
                    }
                    else
                    {
                        var savedData = saveManager.FieldTiles[position][x * fieldSize + y];
                        var tileStatus = (TileStatus)savedData.state;
                        tile.status = tileStatus;
                        
                        switch (tileStatus)
                        {
                            case TileStatus.Trash:
                                SpawnTrashAtTile(tile, savedData.data);
                                break;
                            case TileStatus.Tree:
                                _treePlanter.PlantTree(savedData.data, tile, this);
                                break;
                        }
                    }

                    _tiles.Add(tile);
                }
            }

            MakeGrass();

            if (!hasSave)
            {
                SaveTiles();
            }
        }

        public void MakeGrass()
        {
            foreach (var tile in _tiles)
            {
                if (tile.status == TileStatus.Ground)
                {
                    TerrainPainter.PaintTerrainTexture(TerrainPainter.TerrainTexture.Grass, GetTileWorldPosition(tile));
                }
            }
        }

        private void SpawnTrashAtTile(Tile tile, int id = -1)
        {
            var tileWorldPosition =
                GetTileWorldPosition(tile);

            var trash = id <= 0 ? PoolManager.Instance.GetRandomTrash() : PoolManager.Instance.GetTrash(id);
            trash.transform.parent = transform;
            trash.transform.position = tileWorldPosition;
            trash.Initialize(tile);
            tile.item = trash;
            tile.status = TileStatus.Trash;
        }

        public Vector3 GetTileWorldPosition(Tile tile)
        {
            var pos = new Vector2Int(Mathf.FloorToInt(transform.position.x),
                Mathf.FloorToInt(transform.position.z)) - new Vector2Int(fieldSize / 2, fieldSize / 2) + tile.position;
            return new Vector3(pos.x, 0, pos.y);
        }

        public void SaveTiles()
        {
            var saveData = new SaveManager.TileData[_tiles.Count];
            for (var i = 0; i < _tiles.Count; i++)
            {
                saveData[i] = _tiles[i].GetSaveData();
            }

            _saveManager.SaveFieldTiles(_position, saveData);
        }

        public Tile GetTile(Vector2Int position)
        {
            var index = position.x * fieldSize + position.y;

            if (index >= _tiles.Count || index < 0)
            {
                return null;
            }
            
            return _tiles[index];
        }

        

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

            public SaveManager.TileData GetSaveData()
            {
                var data = new SaveManager.TileData
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
                if (item != null)
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

        private void OnDrawGizmos()
        {
            if (_tiles == null)
            {
                return;
            }


            
            for (var i = 0; i < _tiles.Count; i++)
            {
                var tile = _tiles[i];
                if (tile.status == TileStatus.Empty)
                {
                    continue;
                }
                
                // Draw a sphere at the object's position
              

                // Draw text in Scene view

                var pos = transform.position - new Vector3Int(fieldSize / 2, 0, fieldSize / 2) +
                          new Vector3(tile.position.x, 0, tile.position.y) +
                          Vector3.up * 0.5f;
                Handles.Label(pos
                    , tile.position + $" ({tile.status})", style);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(pos, 0.1f);
            }
        }
    }
}