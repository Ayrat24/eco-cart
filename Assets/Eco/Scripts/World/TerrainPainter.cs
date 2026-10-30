using System.Collections;
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
        private static readonly List<GrowCircle> PendingGrowth = new();
        // Detail cells whose ground is growing the grass layer on the GPU, in stamp order. Once grown they go into
        // the alphamap in batches, so the green stays when the terrain shader stops reading the growth map.
        private static readonly Queue<(int x, int z, float grownAt)> GrowingGround = new();
        private static readonly List<Vector2Int> PendingGround = new();
        private static BitArray _groundStamped;
        private static int _groundCommitGeneration;
        private static bool _groundCommitScheduled;
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

        private struct GrowCircle
        {
            public Vector3 WorldPosition;
            public float BladesRadius;
            public float GroundRadius;
            public int GrassDensity;
            public float GrownAt;
            public float GrowDuration;
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
            PendingGrowth.Clear();
            GrowingGround.Clear();
            PendingGround.Clear();
            _groundCommitGeneration++;
            _groundCommitScheduled = false;
            _flushScheduled = false;

            var terrain = Terrain.activeTerrain;
            UseTerrainDataCopy(terrain);
            var data = terrain.terrainData;

            _grassLayer = System.Array.IndexOf(data.terrainLayers, grassLayer);
            if (_grassLayer < 0)
            {
                Debug.LogWarning($"Grass layer '{(grassLayer != null ? grassLayer.name : "none")}' is not on the " +
                                 "terrain; save the world in the World Editor. Grass won't be painted.");
            }

            _groundStamped = new BitArray(data.detailWidth * data.detailHeight);
            GrassGrowthMap.Reset(data, terrain.transform.position, _grassLayer);
        }

        /// <summary>True for the play-mode copy made by <see cref="ResetTerrain"/>, which is safe to change.</summary>
        public static bool IsRuntimeCopy(TerrainData data) => data != null && data == _terrainCopy;

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

            int density = GrassDensity();
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

            ScheduleFlush();
        }

        /// <summary>
        /// Queues grass that grows in: detail cells whose centre is within <paramref name="bladesRadius"/> metres
        /// get grass details (by detail quality), and those within <paramref name="groundRadius"/> the grass layer
        /// in their ground. Both stay hidden until <paramref name="growStart"/> (<see cref="Time.time"/>), then grow
        /// in over <paramref name="growDuration"/> seconds on the GPU (<see cref="GrassGrowthMap"/>); the grown
        /// ground is written into the alphamap in batches afterwards. The cell under the centre always gets both
        /// unless its radius is 0. Cells that already have grass details keep them as they are. Painted with the
        /// next frame's batch; queue it at least a frame before <paramref name="growStart"/>.
        /// </summary>
        public static void GrowGrass(Vector3 worldPosition, float bladesRadius, float groundRadius, float growStart,
            float growDuration)
        {
            if (_grassLayer < 0)
            {
                return;
            }

            int density = GrassDensity();
            PendingGrowth.Add(new GrowCircle
            {
                WorldPosition = worldPosition,
                BladesRadius = density > 0 ? bladesRadius : 0f,
                GroundRadius = groundRadius,
                GrassDensity = density,
                GrownAt = growStart + growDuration,
                GrowDuration = growDuration
            });

            ScheduleFlush();
        }

        private static int GrassDensity() => Settings.DetailQuality switch
        {
            Settings.DetailQualities.Medium => 128,
            Settings.DetailQualities.High => 256,
            _ => 0
        };

        private static void ScheduleFlush()
        {
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

        // One commit at a time, GroundCommitInterval after the oldest growing cell is grown, so it takes along
        // every cell grown by then
        private static void ScheduleGroundCommit()
        {
            if (_groundCommitScheduled || GrowingGround.Count == 0)
            {
                return;
            }

            _groundCommitScheduled = true;
            float commitAt = GrowingGround.Peek().grownAt + GrassGrowthMap.GroundCommitInterval;
            CommitGroundAt(commitAt, _groundCommitGeneration).Forget();
        }

        private static async UniTaskVoid CommitGroundAt(float commitAt, int generation)
        {
            await UniTask.WaitUntil(() => Time.time >= commitAt);

            // ResetTerrain started another game meanwhile
            if (generation != _groundCommitGeneration)
            {
                return;
            }

            _groundCommitScheduled = false;
            float now = Time.time;
            while (GrowingGround.Count > 0 && GrowingGround.Peek().grownAt <= now)
            {
                var cell = GrowingGround.Dequeue();
                PendingGround.Add(new Vector2Int(cell.x, cell.z));
            }

            ScheduleFlush();
            ScheduleGroundCommit();
        }

        private static void PaintPending()
        {
            using var marker = EcoProfilerMarkers.PaintTerrain.Auto();

            // Never the TerrainData asset: a scene reload hands the terrain its asset back until ResetTerrain
            var terrain = Terrain.activeTerrain;
            if (terrain == null || !IsRuntimeCopy(terrain.terrainData) ||
                (PendingCircles.Count == 0 && PendingGrowth.Count == 0 && PendingGround.Count == 0))
            {
                PendingCircles.Clear();
                PendingGrowth.Clear();
                PendingGround.Clear();
                return;
            }

            var data = terrain.terrainData;
            Vector3 terrainPosition = terrain.transform.position;

            if (PendingCircles.Count > 0 || PendingGround.Count > 0)
            {
                PaintAlphamaps(data, terrainPosition);
            }
            if (PendingCircles.Count > 0 || PendingGrowth.Count > 0)
            {
                PaintGrass(data, terrainPosition);
            }

            PendingCircles.Clear();
            PendingGrowth.Clear();
            PendingGround.Clear();
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

            foreach (var cell in PendingGround)
            {
                if (GroundTexels(data, cell, out var texels))
                {
                    Encapsulate(ref bounds, ref any, texels);
                }
            }

            if (!any || !ClampToMap(ref bounds, width, height))
            {
                return;
            }

            // [z, x, layer]
            float[,,] splatmap = data.GetAlphamaps(bounds.x, bounds.y, bounds.width, bounds.height);
            int layers = data.alphamapLayers;

            foreach (var cell in PendingGround)
            {
                if (!GroundTexels(data, cell, out var texels)) continue;

                for (int z = Mathf.Max(texels.yMin, bounds.yMin); z < Mathf.Min(texels.yMax, bounds.yMax); z++)
                {
                    for (int x = Mathf.Max(texels.xMin, bounds.xMin); x < Mathf.Min(texels.xMax, bounds.xMax); x++)
                    {
                        for (int l = 0; l < layers; l++)
                        {
                            splatmap[z - bounds.y, x - bounds.x, l] = l == _grassLayer ? 1f : 0f;
                        }
                    }
                }
            }

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

            // Detail cells per metre
            float cellsX = width / data.size.x;
            float cellsZ = height / data.size.z;
            foreach (var circle in PendingGrowth)
            {
                var center = WorldToMap(circle.WorldPosition, terrainPosition, data.size, width, height);
                float radius = Mathf.Max(circle.BladesRadius, circle.GroundRadius);
                Encapsulate(ref bounds, ref any, center, Mathf.CeilToInt(radius * Mathf.Max(cellsX, cellsZ)));
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

            bool changed = PendingCircles.Count > 0;
            changed |= PaintGrowingGrass(detailLayer, bounds, width, terrainPosition, cellsX, cellsZ);
            GrassGrowthMap.Upload();
            ScheduleGroundCommit();

            if (changed)
            {
                data.SetDetailLayer(bounds.x, bounds.y, detailLayerIndex, detailLayer);
            }
        }

        // Fills the bare cells under each growing circle's blades and ground, stamps when they finish growing and
        // queues the ground for the alphamap. Runs after the plain circles, so grass they paint in the same batch is
        // there at once rather than growing. Returns whether any grass details changed.
        private static bool PaintGrowingGrass(int[,] detailLayer, RectInt bounds, int mapWidth,
            Vector3 terrainPosition, float cellsX, float cellsZ)
        {
            bool changed = false;
            foreach (var circle in PendingGrowth)
            {
                // In cells, relative to the batch's corner
                float cx = (circle.WorldPosition.x - terrainPosition.x) * cellsX - bounds.x;
                float cz = (circle.WorldPosition.z - terrainPosition.z) * cellsZ - bounds.y;
                int centerX = Mathf.FloorToInt(cx);
                int centerZ = Mathf.FloorToInt(cz);
                float reach = Mathf.Max(circle.BladesRadius, circle.GroundRadius);

                int zMin = Mathf.Max(0, Mathf.FloorToInt(cz - reach * cellsZ));
                int zMax = Mathf.Min(bounds.height - 1, Mathf.FloorToInt(cz + reach * cellsZ));
                int xMin = Mathf.Max(0, Mathf.FloorToInt(cx - reach * cellsX));
                int xMax = Mathf.Min(bounds.width - 1, Mathf.FloorToInt(cx + reach * cellsX));
                for (int z = zMin; z <= zMax; z++)
                {
                    // Distance from the centre in metres
                    float dz = (z + 0.5f - cz) / cellsZ;
                    for (int x = xMin; x <= xMax; x++)
                    {
                        float dx = (x + 0.5f - cx) / cellsX;
                        float distanceSqr = dx * dx + dz * dz;
                        bool center = x == centerX && z == centerZ;

                        // Grass details already there would shrink and grow again
                        bool blades = circle.BladesRadius > 0f && detailLayer[z, x] == 0 &&
                                      (center || distanceSqr <= circle.BladesRadius * circle.BladesRadius);
                        if (blades)
                        {
                            detailLayer[z, x] = circle.GrassDensity;
                            GrassGrowthMap.StampBlades(bounds.x + x, bounds.y + z, circle.GrownAt, circle.GrowDuration);
                            changed = true;
                        }

                        int cell = (bounds.y + z) * mapWidth + bounds.x + x;
                        bool ground = circle.GroundRadius > 0f && !_groundStamped[cell] &&
                                      (center || distanceSqr <= circle.GroundRadius * circle.GroundRadius);
                        if (ground)
                        {
                            _groundStamped[cell] = true;
                            GrassGrowthMap.StampGround(bounds.x + x, bounds.y + z, circle.GrownAt, circle.GrowDuration);
                            GrowingGround.Enqueue((bounds.x + x, bounds.y + z, circle.GrownAt));
                        }
                    }
                }
            }

            return changed;
        }

        // The alphamap texels that take their grass from detail cell `cell`, as the terrain shader reads them
        // (GrassGrowth.hlsl): texel j sits j / (alphamap size - 1) across the terrain and belongs to the cell there.
        private static bool GroundTexels(TerrainData data, Vector2Int cell, out RectInt texels)
        {
            int xMin = FirstTexel(cell.x, data.detailWidth, data.alphamapWidth);
            int zMin = FirstTexel(cell.y, data.detailHeight, data.alphamapHeight);
            int xMax = FirstTexel(cell.x + 1, data.detailWidth, data.alphamapWidth);
            int zMax = FirstTexel(cell.y + 1, data.detailHeight, data.alphamapHeight);
            texels = new RectInt(xMin, zMin, xMax - xMin, zMax - zMin);
            return texels.width > 0 && texels.height > 0;
        }

        // First texel j with floor(j * cells / (texels - 1)) >= cell
        private static int FirstTexel(int cell, int cells, int texels) =>
            (cell * (texels - 1) + cells - 1) / cells;

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
            Encapsulate(ref bounds, ref any, circleBounds);
        }

        private static void Encapsulate(ref RectInt bounds, ref bool any, RectInt rect)
        {
            if (!any)
            {
                bounds = rect;
                any = true;
                return;
            }

            int xMin = Mathf.Min(bounds.xMin, rect.xMin);
            int yMin = Mathf.Min(bounds.yMin, rect.yMin);
            int xMax = Mathf.Max(bounds.xMax, rect.xMax);
            int yMax = Mathf.Max(bounds.yMax, rect.yMax);
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
