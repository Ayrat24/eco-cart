using System.Collections.Generic;
using Eco.Scripts.Profiling;
using Eco.Scripts.Pooling;
using Eco.Scripts.Scrap;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Random = UnityEngine.Random;

namespace Eco.Scripts.World
{
    public class FieldChunk : Chunk
    {
        [SerializeField] private bool debug;

        private TreePlanter _treePlanter;
        private ScrapRestingPoses _restingPoses;
        private readonly List<ScrapItem> _restingScrap = new();

#if UNITY_EDITOR
        private GUIStyle _style;
#endif


        public void Init(TreePlanter treePlanter, ScrapRestingPoses restingPoses)
        {
            using var marker = EcoProfilerMarkers.FieldChunkInit.Auto();

            _treePlanter = treePlanter;
            _restingPoses = restingPoses;

            bool hasSave = HasSave;
            if (!hasSave)
            {
                CreateScrap();
            }
            else
            {
                LoadSave();
            }

            PaintGrass();

            // A loaded chunk already matches its save
            if (!hasSave)
            {
                SaveTiles();
            }

            if (debug)
            {
                DebugDraw();
            }
        }

        private void CreateScrap()
        {
            int totalTiles = ChunkSize * ChunkSize;
            int target = Mathf.Clamp(ScrapPerChunk, 0, totalTiles);
            if (target == 0) return;

            // Grid dimensions (cols x rows) approximating target cells
            int cols = Mathf.CeilToInt(Mathf.Sqrt(target));
            int rows = Mathf.CeilToInt((float)target / cols);

            float cellW = (float)ChunkSize / cols;
            float cellH = (float)ChunkSize / rows;

            // Build exactly `target` cell centers (left-to-right, top-to-bottom)
            var cells = new List<(float cx, float cy)>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (cells.Count >= target) break;
                    float cx = (c + 0.5f) * cellW;
                    float cy = (r + 0.5f) * cellH;
                    cells.Add((cx, cy));
                }
                if (cells.Count >= target) break;
            }

            // Shuffle cells to randomize placement order
            for (int i = cells.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = cells[i];
                cells[i] = cells[j];
                cells[j] = tmp;
            }

            var selected = new HashSet<int>();

            // helper to find nearest unused tile to a float center, searching outward
            int FindNearestUnused(float cx, float cy)
            {
                int centerX = Mathf.Clamp(Mathf.FloorToInt(cx), 0, ChunkSize - 1);
                int centerY = Mathf.Clamp(Mathf.FloorToInt(cy), 0, ChunkSize - 1);

                // first try a few jittered samples within half-cell to keep randomness
                float halfW = Mathf.Max(0.5f, cellW * 0.5f - 0.001f);
                float halfH = Mathf.Max(0.5f, cellH * 0.5f - 0.001f);
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    float rx = cx + Random.Range(-halfW, halfW);
                    float ry = cy + Random.Range(-halfH, halfH);
                    int tx = Mathf.Clamp(Mathf.FloorToInt(rx), 0, ChunkSize - 1);
                    int ty = Mathf.Clamp(Mathf.FloorToInt(ry), 0, ChunkSize - 1);
                    int idx = ty * ChunkSize + tx;
                    if (!selected.Contains(idx)) return idx;
                }

                // expanding square search out to full chunk if needed
                int maxRadius = Mathf.Max(ChunkSize, ChunkSize);
                for (int r = 0; r <= maxRadius; r++)
                {
                    int x0 = Mathf.Clamp(centerX - r, 0, ChunkSize - 1);
                    int x1 = Mathf.Clamp(centerX + r, 0, ChunkSize - 1);
                    int y0 = Mathf.Clamp(centerY - r, 0, ChunkSize - 1);
                    int y1 = Mathf.Clamp(centerY + r, 0, ChunkSize - 1);

                    var cand = new List<int>();
                    for (int yy = y0; yy <= y1; yy++)
                    {
                        for (int xx = x0; xx <= x1; xx++)
                        {
                            // only perimeter of current square to prioritize nearest
                            if (yy != y0 && yy != y1 && xx != x0 && xx != x1) continue;
                            int idx = yy * ChunkSize + xx;
                            if (!selected.Contains(idx)) cand.Add(idx);
                        }
                    }

                    if (cand.Count > 0)
                    {
                        // pick randomly among perimeter candidates
                        return cand[Random.Range(0, cand.Count)];
                    }
                }

                // final fallback: any unused tile
                for (int i = 0; i < totalTiles; i++) if (!selected.Contains(i)) return i;
                return -1;
            }

            // pick one tile per cell
            foreach (var (cx, cy) in cells)
            {
                int idx = FindNearestUnused(cx, cy);
                if (idx >= 0)
                {
                    selected.Add(idx);
                    if (selected.Count >= target) break;
                }
            }

            // safety fill
            for (int i = 0; selected.Count < target && i < totalTiles; i++)
            {
                if (!selected.Contains(i)) selected.Add(i);
            }

            foreach (var index in selected)
            {
                SpawnScrapAtTile(Tiles[index], -1, false);
            }
        }

        private void LoadSave()
        {
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    var index = y * ChunkSize + x;
                    Tile tile = Tiles[index];


                    var savedData = SaveManager.FieldTiles[Position][index];
                    tile.groundType = (TileGroundType)savedData.ground;
                    var tileStatus = (TileObjectType)savedData.objectType;
                    tile.objectType = tileStatus;
                    tile.containedScrap = savedData.containedScrap;

                    switch (tileStatus)
                    {
                        case TileObjectType.Scrap:
                            // pass saved contained flag when spawning
                            var scrap = SpawnScrapAtTile(tile, savedData.objectId, savedData.containedScrap);
                            if (_restingPoses.TryTake(Position, index, out var pose))
                            {
                                scrap.PlaceAtRest(pose);
                                _restingScrap.Add(scrap);
                            }
                            break;
                        case TileObjectType.Tree:
                            _treePlanter.PlantTree(savedData.objectId, tile, this);
                            break;
                    }
                }
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
            PaintGrass();
            SaveTiles();
        }

        // TerrainPainter paints on the next frame and skips circles that are already painted
        private void PaintGrass()
        {
            foreach (var tile in tiles)
            {
                if (tile.objectType == TileObjectType.Tree)
                {
                    TerrainPainter.PaintTerrainTexture(TerrainPainter.TerrainTexture.Grass, GetTileWorldPosition(tile),
                        TreePlanter.TreeGrassRadius);
                }
            }
        }

        private ScrapItem SpawnScrapAtTile(Tile tile, int id = -1, bool contained = true)
        {
            var tileWorldPosition =
                GetTileWorldPosition(tile);

            var scrap = id <= 0 ? PoolManager.Instance.GetRandomScrap() : PoolManager.Instance.GetScrap(id);
            scrap.transform.parent = transform;
            scrap.transform.position = tileWorldPosition;
            scrap.Initialize(tile);
            tile.item = scrap;
            tile.objectType = TileObjectType.Scrap;
            tile.containedScrap = contained;
            return scrap;
        }

        /// <summary>
        /// Called by WorldController once the spawned chunks' transforms have reached physics.
        /// </summary>
        public void SleepRestingScrap()
        {
            foreach (var scrap in _restingScrap)
            {
                scrap.Sleep();
            }

            _restingScrap.Clear();
        }

        /// <summary>
        /// Called by WorldController before the chunk goes back to the pool: the pool deactivates it before
        /// OnDespawn, and an inactive rigidbody no longer reports whether it was asleep.
        /// </summary>
        public void StoreRestingScrap()
        {
            for (var i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].item is ScrapItem scrap && scrap.TryGetRestingPose(out var pose))
                {
                    _restingPoses.Store(Position, i, pose);
                }
            }
        }

        public override void OnDespawn()
        {
            _restingScrap.Clear();
            base.OnDespawn();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (tiles == null)
            {
                return;
            }

            _style ??= new GUIStyle
            {
                normal =
                {
                    textColor = Color.magenta
                },
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };


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
                    , tile.position + $" ({tile.objectType}/{tile.groundType})", _style);

                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(pos, 0.1f);
            }
        }
#endif
    }
}
