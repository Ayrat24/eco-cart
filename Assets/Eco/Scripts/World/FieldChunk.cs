using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Pooling;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Eco.Scripts.World
{
    public class FieldChunk : Chunk
    {
        [SerializeField] private bool debug;

        private TreePlanter _treePlanter;
        private CancellationTokenSource _cancellationTokenSource;

#if UNITY_EDITOR
        private GUIStyle style;
#endif


        public void Init(TreePlanter treePlanter)
        {
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

            bool hasSave = HasSave;

            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    var index = y * ChunkSize + x;
                    Tile tile = Tiles[index];

                    if (!hasSave)
                    {
                        bool hasTrash = Random.Range(0, 100) < 5;

                        if (hasTrash)
                        {
                            SpawnTrashAtTile(tile);
                        }
                    }
                    else
                    {
                        var savedData = SaveManager.FieldTiles[Position][index];
                        tile.groundType = (TileGroundType)savedData.ground;
                        var tileStatus = (TileObjectType)savedData.objectType;
                        tile.objectType = tileStatus;

                        switch (tileStatus)
                        {
                            case TileObjectType.Trash:
                                SpawnTrashAtTile(tile, savedData.objectId);
                                break;
                            case TileObjectType.Tree:
                                _treePlanter.PlantTree(savedData.objectId, tile, this);
                                break;
                        }
                    }
                }
            }

            _cancellationTokenSource = new CancellationTokenSource();
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
            var mesh = GetComponentInChildren<MeshRenderer>(true);
            mesh.material.color = Random.ColorHSV(0.2f, 0.7f, 0.2f, 1f);
            mesh.gameObject.SetActive(true);
        }

        public void MakeGrass()
        {
            MakeGrass(_cancellationTokenSource.Token).Forget();
            SaveTiles();
        }

        private async UniTask MakeGrass(CancellationToken cancellationToken)
        {
            await UniTask.NextFrame(cancellationToken);
            foreach (var tile in tiles)
            {
                if (tile.objectType == TileObjectType.Tree)
                {
                    TerrainPainter.PaintTerrainTexture(TerrainPainter.TerrainTexture.Grass, GetTileWorldPosition(tile),
                        TreePlanter.TreeGrassRadius);
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
            tile.objectType = TileObjectType.Trash;
        }

        public override void OnDespawn()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            
            base.OnDespawn();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (tiles == null)
            {
                return;
            }


            for (var i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];
                if (tile.objectType == TileObjectType.Empty)
                {
                    continue;
                }

                // Draw a sphere at the object's position


                // Draw text in Scene view

                var pos = transform.position - new Vector3Int(ChunkSize / 2, 0, ChunkSize / 2) +
                          new Vector3(tile.position.x, 0, tile.position.y) +
                          Vector3.up * 0.5f;
                Handles.Label(pos
                    , tile.position + $" ({tile.objectType}/{tile.groundType})", style);

                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(pos, 0.1f);
            }
        }
#endif
    }
}