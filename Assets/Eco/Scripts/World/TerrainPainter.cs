using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Profiling;
using UnityEngine;

namespace Eco.Scripts.World
{
    public abstract class TerrainPainter
    {
        // Paint stays in the TerrainData until ResetTerrain, so a circle painted once never needs painting again
        // when its chunk respawns. Keyed by everything that affects the result.
        private static readonly HashSet<(int layer, int x, int z, int radius, int density)> PaintedCircles = new();
        private static readonly List<PaintCircle> PendingCircles = new();
        private static bool _flushScheduled;
        private static int _grassLayer = -1;
        private static TerrainData _terrainCopy;

        private struct PaintCircle
        {
            public int Layer;
            public Vector3 WorldPosition;
            public int Radius;
            public int GrassDensity;
        }

        /// <summary>
        /// Starts a game on a fresh copy of the scene terrain as the World Editor baked it, so grass painted while
        /// playing never reaches the TerrainData asset (and a restarted game starts from the baked look again).
        /// </summary>
        public static void ResetTerrain(TerrainLayer grassLayer)
        {
            using var marker = EcoProfilerMarkers.ResetTerrain.Auto();

            PaintedCircles.Clear();
            PendingCircles.Clear();
            _flushScheduled = false;

            var terrain = Terrain.activeTerrain;
            UseTerrainDataCopy(terrain);

            _grassLayer = System.Array.IndexOf(terrain.terrainData.terrainLayers, grassLayer);
            if (_grassLayer < 0)
            {
                Debug.LogWarning($"Grass layer '{(grassLayer != null ? grassLayer.name : "none")}' is not on the " +
                                 "terrain; save the world in the World Editor. Grass won't be painted.");
            }
        }

        private static void UseTerrainDataCopy(Terrain terrain)
        {
            // Scene loads hand the terrain its asset again; the previous game's copy is no longer used
            if (_terrainCopy != null && terrain.terrainData != _terrainCopy)
            {
                Object.Destroy(_terrainCopy);
            }

            var data = terrain.terrainData;
            if (data == _terrainCopy)
            {
                return;
            }

            _terrainCopy = Object.Instantiate(data);
            _terrainCopy.name = data.name + " (Runtime)";
            terrain.terrainData = _terrainCopy;

            if (terrain.TryGetComponent<TerrainCollider>(out var terrainCollider))
            {
                terrainCollider.terrainData = _terrainCopy;
            }
        }

        /// <summary>
        /// Queues a circle of the grass layer plus grass details (by detail quality), <paramref name="radius"/> in
        /// terrain map pixels (about a metre each). Everything queued in a frame is painted together on the next
        /// frame with one read and one write of the terrain maps.
        /// </summary>
        public static void PaintGrass(Vector3 worldPosition, int radius)
        {
            if (_grassLayer < 0)
            {
                return;
            }

            int density = Settings.DetailQuality switch
            {
                Settings.DetailQualities.Medium => 128,
                Settings.DetailQualities.High => 256,
                _ => 0
            };

            var key = (_grassLayer, Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.z), radius,
                density);
            if (!PaintedCircles.Add(key))
            {
                return;
            }

            PendingCircles.Add(new PaintCircle
            {
                Layer = _grassLayer,
                WorldPosition = worldPosition,
                Radius = radius,
                GrassDensity = density
            });

            if (!_flushScheduled)
            {
                _flushScheduled = true;
                PaintNextFrame().Forget();
            }
        }

        private static async UniTaskVoid PaintNextFrame()
        {
            await UniTask.NextFrame();

            // ResetTerrain resets the flag and the queue (new game, or play mode restarted with this still pending)
            if (!_flushScheduled)
            {
                return;
            }

            _flushScheduled = false;
            PaintPending();
        }

        private static void PaintPending()
        {
            using var marker = EcoProfilerMarkers.PaintTerrain.Auto();

            var terrain = Terrain.activeTerrain;
            if (terrain == null || PendingCircles.Count == 0)
            {
                PendingCircles.Clear();
                return;
            }

            var data = terrain.terrainData;
            Vector3 terrainPosition = terrain.transform.position;

            PaintAlphamaps(data, terrainPosition);
            PaintGrass(data, terrainPosition);

            PendingCircles.Clear();
        }

        private static void PaintAlphamaps(TerrainData data, Vector3 terrainPosition)
        {
            int width = data.alphamapWidth;
            int height = data.alphamapHeight;

            var bounds = new RectInt();
            bool any = false;
            foreach (var circle in PendingCircles)
            {
                var center = WorldToMap(circle.WorldPosition, terrainPosition, data.size, width, height);
                Encapsulate(ref bounds, ref any, center, circle.Radius);
            }

            if (!ClampToMap(ref bounds, width, height))
            {
                return;
            }

            // [z, x, layer]
            float[,,] splatmap = data.GetAlphamaps(bounds.x, bounds.y, bounds.width, bounds.height);
            int layers = data.alphamapLayers;

            foreach (var circle in PendingCircles)
            {
                var center = WorldToMap(circle.WorldPosition, terrainPosition, data.size, width, height);
                int radius = circle.Radius;

                for (int dz = -radius; dz <= radius; dz++)
                {
                    int z = center.y + dz - bounds.y;
                    if (z < 0 || z >= bounds.height) continue;

                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int x = center.x + dx - bounds.x;
                        if (x < 0 || x >= bounds.width || dx * dx + dz * dz > radius * radius) continue;

                        for (int l = 0; l < layers; l++)
                        {
                            splatmap[z, x, l] = l == circle.Layer ? 1f : 0f;
                        }
                    }
                }
            }

            data.SetAlphamaps(bounds.x, bounds.y, splatmap);
        }

        private static void PaintGrass(TerrainData data, Vector3 terrainPosition)
        {
            const int detailLayerIndex = 0;
            int width = data.detailWidth;
            int height = data.detailHeight;

            var bounds = new RectInt();
            bool any = false;
            foreach (var circle in PendingCircles)
            {
                if (circle.GrassDensity <= 0) continue;

                var center = WorldToMap(circle.WorldPosition, terrainPosition, data.size, width, height);
                Encapsulate(ref bounds, ref any, center, GrassRadius(circle));
            }

            if (!any || !ClampToMap(ref bounds, width, height))
            {
                return;
            }

            // SetDetailLayer takes [z, x]. GetDetailLayer returns the same z-major data but, for a non-square
            // region, in an array declared [width, height], so copy the raw buffer into the shape Set expects.
            int[,] read = data.GetDetailLayer(bounds.x, bounds.y, bounds.width, bounds.height, detailLayerIndex);
            var detailLayer = new int[bounds.height, bounds.width];
            System.Buffer.BlockCopy(read, 0, detailLayer, 0, read.Length * sizeof(int));

            foreach (var circle in PendingCircles)
            {
                if (circle.GrassDensity <= 0) continue;

                var center = WorldToMap(circle.WorldPosition, terrainPosition, data.size, width, height);
                int radius = GrassRadius(circle);

                for (int dz = -radius; dz <= radius; dz++)
                {
                    int z = center.y + dz - bounds.y;
                    if (z < 0 || z >= bounds.height) continue;

                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int x = center.x + dx - bounds.x;
                        if (x < 0 || x >= bounds.width || dx * dx + dz * dz > radius * radius) continue;

                        detailLayer[z, x] = circle.GrassDensity; // Higher = more grass
                    }
                }
            }

            data.SetDetailLayer(bounds.x, bounds.y, detailLayerIndex, detailLayer);
        }

        // Grass stops short of the texture edge
        private static int GrassRadius(PaintCircle circle) => Mathf.Max(0, circle.Radius - 2);

        private static Vector2Int WorldToMap(Vector3 worldPosition, Vector3 terrainPosition, Vector3 terrainSize,
            int mapWidth, int mapHeight)
        {
            return new Vector2Int(
                (int)((worldPosition.x - terrainPosition.x) / terrainSize.x * mapWidth),
                (int)((worldPosition.z - terrainPosition.z) / terrainSize.z * mapHeight));
        }

        private static void Encapsulate(ref RectInt bounds, ref bool any, Vector2Int center, int radius)
        {
            var circleBounds = new RectInt(center.x - radius, center.y - radius, radius * 2 + 1, radius * 2 + 1);
            if (!any)
            {
                bounds = circleBounds;
                any = true;
                return;
            }

            int xMin = Mathf.Min(bounds.xMin, circleBounds.xMin);
            int yMin = Mathf.Min(bounds.yMin, circleBounds.yMin);
            int xMax = Mathf.Max(bounds.xMax, circleBounds.xMax);
            int yMax = Mathf.Max(bounds.yMax, circleBounds.yMax);
            bounds = new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private static bool ClampToMap(ref RectInt bounds, int mapWidth, int mapHeight)
        {
            int xMin = Mathf.Max(bounds.xMin, 0);
            int yMin = Mathf.Max(bounds.yMin, 0);
            int xMax = Mathf.Min(bounds.xMax, mapWidth);
            int yMax = Mathf.Min(bounds.yMax, mapHeight);
            bounds = new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
            return bounds.width > 0 && bounds.height > 0;
        }
    }
}
