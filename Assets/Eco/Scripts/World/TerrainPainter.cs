using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Profiling;
using UnityEngine;

namespace Eco.Scripts.World
{
    public abstract class TerrainPainter
    {
        // Paint stays in the TerrainData until ClearTerrain, so a circle painted once never needs painting again
        // when its chunk respawns. Keyed by everything that affects the result.
        private static readonly HashSet<(int layer, int x, int z, int radius, int density)> PaintedCircles = new();
        private static readonly List<PaintCircle> PendingCircles = new();
        private static bool _flushScheduled;

        private struct PaintCircle
        {
            public int Layer;
            public Vector3 WorldPosition;
            public int Radius;
            public int GrassDensity;
        }

        public static void ClearTerrain()
        {
            using var marker = EcoProfilerMarkers.ClearTerrain.Auto();

            PaintedCircles.Clear();
            PendingCircles.Clear();
            _flushScheduled = false;

            var terrain = Terrain.activeTerrain;
#if UNITY_EDITOR
            UseTerrainDataCopy(terrain);
#endif
            var data = terrain.terrainData;

            // Every pixel becomes fully layer 0, so there is nothing to read back first
            float[,,] splatmap = new float[data.alphamapHeight, data.alphamapWidth, data.alphamapLayers];
            for (int z = 0; z < data.alphamapHeight; z++)
            {
                for (int x = 0; x < data.alphamapWidth; x++)
                {
                    splatmap[z, x, 0] = 1;
                }
            }

            data.SetAlphamaps(0, 0, splatmap);
            data.SetDetailLayer(0, 0, 0, new int[data.detailHeight, data.detailWidth]);
        }

#if UNITY_EDITOR
        // Painting and flattening edit the TerrainData in place. In the editor that is the New Terrain 1 asset,
        // and a later Save Project would write the painted grass into it, so play mode works on a copy.
        private static void UseTerrainDataCopy(Terrain terrain)
        {
            var data = terrain.terrainData;
            if (!UnityEditor.AssetDatabase.Contains(data))
            {
                return;
            }

            var copy = Object.Instantiate(data);
            copy.name = data.name + " (Play Mode)";
            terrain.terrainData = copy;

            if (terrain.TryGetComponent<TerrainCollider>(out var terrainCollider))
            {
                terrainCollider.terrainData = copy;
            }
        }
#endif

        /// <summary>
        /// Queues a texture circle (plus grass for <see cref="TerrainTexture.Grass"/>). Everything queued in a frame
        /// is painted together on the next frame with one read and one write of the terrain maps.
        /// </summary>
        public static void PaintTerrainTexture(TerrainTexture texture, Vector3 worldPosition, int radius)
        {
            int density = texture != TerrainTexture.Grass
                ? 0
                : Settings.DetailQuality switch
                {
                    Settings.DetailQualities.Medium => 128,
                    Settings.DetailQualities.High => 256,
                    _ => 0
                };

            var key = ((int)texture, Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.z), radius,
                density);
            if (!PaintedCircles.Add(key))
            {
                return;
            }

            PendingCircles.Add(new PaintCircle
            {
                Layer = (int)texture,
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

            // ClearTerrain resets the flag and the queue (new game, or play mode restarted with this still pending)
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

        public enum TerrainTexture
        {
            Sand,
            Grass
        }
    }
}
