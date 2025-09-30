using System.Collections.Generic;
using Eco.Scripts.Pooling;
using Eco.Scripts.Trash;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Eco.Scripts.World
{
    public class Field : MonoBehaviour, IPooledObject
    {
        [SerializeField] private bool debug;
        [SerializeField] private List<Tile> tiles = new();
        [SerializeField] private int fieldSize = 10;

        private SaveManager _saveManager;
        private Vector2Int _position;
        private TreePlanter _treePlanter;

        public List<Tile> Tiles => tiles;
        
#if UNITY_EDITOR
        private GUIStyle style;
#endif

        public void Init(Vector2Int position, SaveManager saveManager, TreePlanter treePlanter)
        {
            _saveManager = saveManager;
            _position = position;
            _treePlanter = treePlanter;

#if UNITY_EDITOR
            style = new GUIStyle
            {
                normal =
                {
                    textColor = Color.magenta
                },
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
#endif

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

                    tiles.Add(tile);
                }
            }

            MakeGrass();

            if (!hasSave)
            {
                SaveTiles();
            }

            if (debug)
            {
                DebugDraw();
            }
        }

        private void DebugDraw()
        {
            var mesh = GetComponentInChildren<MeshRenderer>();
            mesh.material.color = Random.ColorHSV(0.2f, 0.7f, 0.2f, 1f);
            mesh.gameObject.SetActive(true);
        }

        public virtual void MakeGrass()
        {
            foreach (var tile in tiles)
            {
                if (tile.status == TileStatus.Tree)
                {
                    TerrainPainter.PaintTerrainTexture(TerrainPainter.TerrainTexture.Grass, GetTileWorldPosition(tile),
                        16);
                }
            }
        }

        protected virtual TrashItem SpawnTrashAtTile(Tile tile, int id = -1)
        {
            var tileWorldPosition =
                GetTileWorldPosition(tile);

            var trash = id <= 0 ? PoolManager.Instance.GetRandomTrash() : PoolManager.Instance.GetTrash(id);
            trash.transform.parent = transform;
            trash.transform.position = tileWorldPosition;
            trash.Initialize(tile);
            tile.item = trash;
            tile.status = TileStatus.Trash;
            return trash;
        }

        public Vector3 GetTileWorldPosition(Tile tile)
        {
            var pos = new Vector2Int(Mathf.FloorToInt(transform.position.x),
                Mathf.FloorToInt(transform.position.z)) - new Vector2Int(fieldSize / 2, fieldSize / 2) + tile.position;
            return new Vector3(pos.x, 0, pos.y);
        }

        public void SaveTiles()
        {
            var saveData = new TileData[tiles.Count];
            for (var i = 0; i < tiles.Count; i++)
            {
                saveData[i] = tiles[i].GetSaveData();
            }

            _saveManager.SaveFieldTiles(_position, saveData);
        }

        public Tile GetTile(Vector2Int position)
        {
            var index = position.x * fieldSize + position.y;

            if (index >= tiles.Count || index < 0)
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
            SaveTiles();
            foreach (var t in tiles)
            {
                t.Clear();
            }

            tiles.Clear();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            return;
            if (tiles == null)
            {
                return;
            }


            for (var i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
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
#endif
    }
}