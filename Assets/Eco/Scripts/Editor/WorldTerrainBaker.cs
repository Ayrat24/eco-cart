using System.Collections.Generic;
using Eco.Scripts.World;
using UnityEditor;
using UnityEngine;

namespace Eco.Scripts.EditorTools
{
    /// <summary>
    /// Writes a <see cref="WorldData"/> into the scene terrain: heights from each hex's elevation and roughness,
    /// splat textures from each hex's terrain layer, and no grass (the game paints grass around trees).
    /// <para>
    /// Each painted hex is stamped as a round dab a little bigger than the hex, later palette entries over earlier
    /// ones and every painted hex over the unpainted ocean, like painting with a circular brush, and the edges
    /// between them wobble with smooth noise so no shape keeps the grid's symmetry. The result is then blurred
    /// with a round (Gaussian) kernel, so groups of hexes become soft blobs and nothing in the
    /// terrain shows the grid. The water's outline gets its own, wider blur; from that outline the bed falls away
    /// gently with the distance from the waterline (<see cref="WorldData.ShelfWidth"/>) and land rises back to its
    /// own height. The game puts a water surface over every hex where the terrain ends up under water.
    /// </para>
    /// </summary>
    internal static class WorldTerrainBaker
    {
        // Water hexes count as land this far above the water when smoothing land heights, so land near water sits
        // low and the rise from the waterline stays gentle
        private const float ShoreHeight = 0.3f;

        // Share of water around a point (0-1, after blurring) where the waterline is drawn. Half is the painted
        // edge; a little less keeps very small lakes from vanishing.
        private const float ShoreLine = 0.45f;

        // Blur of the distance to the waterline, in metres; rounds off the pixel steps of the distance transform
        private const float ShoreSmoothing = 1.5f;

        // On land, roughness fades out within this many metres of the waterline so bumps don't break up the shore.
        // In the water it fades out over the whole shelf.
        private const float CalmShore = 4f;

        // Slope (rise per metre) of the ground where it crosses the waterline, on both sides. Ground that flattens
        // out at the water level lets the 16-bit terrain heights wobble the shoreline by metres, in blocky steps.
        private const float WaterlineSlope = 0.08f;

        // Heightmap of at most ~1 m per sample, so shores and small lakes keep their shape
        private const float MaxMetresPerHeightSample = 1.05f;

        private const int MaxHeightmapResolution = 4097;

        // The blur reaches about three sigmas; past that the raster only holds ocean
        private const float BlurReach = 3f;

        // Size of the edge wobble's waves as a multiple of the hex radius: a few hexes, so it reshapes groups of
        // hexes instead of fraying single edges. A second, longer and weaker wave bends long straight coasts.
        private const float WobbleWavelength = 4f;
        private const float LongWobbleWavelength = 12f;
        private const float LongWobbleShare = 0.6f;

        // Radius of a hex's dab as a share of the hex radius. Over 1 so the dabs of a painted area overlap with no
        // gaps at the hex corners; under 2 so only a hex's direct neighbours can reach into it.
        private const float DabRadius = 1.15f;

        public static void Bake(WorldData world, Terrain terrain)
        {
            var data = terrain.terrainData;

            try
            {
                EditorUtility.DisplayProgressBar("Bake World Terrain", "Layers", 0f);
                var layers = BakeLayers(world, data);

                EnsureHeightmapResolution(data);

                EditorUtility.DisplayProgressBar("Bake World Terrain", "Smoothing", 0.1f);
                var fields = new Fields(world, terrain, layers);

                EditorUtility.DisplayProgressBar("Bake World Terrain", "Heights", 0.5f);
                BakeHeights(data, fields);

                EditorUtility.DisplayProgressBar("Bake World Terrain", "Textures", 0.7f);
                BakeTextures(data, fields);

                EditorUtility.DisplayProgressBar("Bake World Terrain", "Grass", 0.9f);
                ClearDetails(data);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.SetDirty(data);
            terrain.Flush();
        }

        // Every layer the palette uses, plus the grass the game paints. A definition without a layer uses the first.
        private static List<TerrainLayer> BakeLayers(WorldData world, TerrainData data)
        {
            var layers = new List<TerrainLayer>();
            void Add(TerrainLayer layer)
            {
                if (layer != null && !layers.Contains(layer))
                {
                    layers.Add(layer);
                }
            }

            foreach (var definition in world.Palette)
            {
                if (definition != null)
                {
                    Add(definition.TerrainLayer);
                }
            }

            if (world.Ocean != null)
            {
                Add(world.Ocean.TerrainLayer);
            }

            Add(world.GrassLayer);
            if (layers.Count == 0 && data.terrainLayers.Length > 0)
            {
                layers.Add(data.terrainLayers[0]);
            }

            data.terrainLayers = layers.ToArray();
            return layers;
        }

        private static void EnsureHeightmapResolution(TerrainData data)
        {
            var size = data.size;
            int needed = Mathf.CeilToInt(Mathf.Max(size.x, size.z) / MaxMetresPerHeightSample);
            int resolution = Mathf.Min(Mathf.NextPowerOfTwo(Mathf.Max(32, needed)) + 1, MaxHeightmapResolution);
            if (data.heightmapResolution >= resolution)
            {
                return;
            }

            // Changing the resolution rescales the terrain, so put the size back
            data.heightmapResolution = resolution;
            data.size = size;
        }

        private static void BakeHeights(TerrainData data, Fields fields)
        {
            int resolution = data.heightmapResolution;
            var size = data.size;
            var heights = new float[resolution, resolution];
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    var p = new Vector2(x / (float)(resolution - 1) * size.x, z / (float)(resolution - 1) * size.z);
                    heights[z, x] = Mathf.Clamp01((fields.Height(p) - fields.TerrainPosition.y) / size.y);
                }
            }

            data.SetHeights(0, 0, heights);
        }

        private static void BakeTextures(TerrainData data, Fields fields)
        {
            int width = data.alphamapWidth;
            int height = data.alphamapHeight;
            int count = data.alphamapLayers;
            var size = data.size;
            var alphamaps = new float[height, width, count];
            var weights = new float[count];

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    var p = new Vector2((x + 0.5f) / width * size.x, (z + 0.5f) / height * size.z);
                    fields.TextureWeights(p, weights);
                    for (int l = 0; l < count; l++)
                    {
                        alphamaps[z, x, l] = weights[l];
                    }
                }
            }

            data.SetAlphamaps(0, 0, alphamaps);
        }

        private static void ClearDetails(TerrainData data)
        {
            var empty = new int[data.detailHeight, data.detailWidth];
            for (int layer = 0; layer < data.detailPrototypes.Length; layer++)
            {
                data.SetDetailLayer(0, 0, layer, empty);
            }
        }

        /// <summary>
        /// The painted world as smooth fields over the map area: land height, water share, water bed, roughness and
        /// one weight per terrain layer. Outside the map area everything is ocean.
        /// </summary>
        private class Fields
        {
            private readonly WorldData _world;
            private readonly Raster _land;
            private readonly Raster _water;
            private readonly Raster _bed;
            private readonly Raster _roughness;
            private readonly Raster _shore;
            private readonly Raster[] _layers;
            private readonly int _oceanLayer;
            private readonly float _oceanBed;
            private readonly float _oceanRoughness;
            private readonly float _shoreLand;

            public Vector3 TerrainPosition { get; }

            public Fields(WorldData world, Terrain terrain, List<TerrainLayer> layers)
            {
                _world = world;
                TerrainPosition = terrain.GetPosition();

                var ocean = world.Ocean;
                _oceanBed = Elevation(ocean);
                _oceanRoughness = ocean != null ? ocean.Roughness : 0f;
                _oceanLayer = LayerIndex(ocean, layers);
                _shoreLand = world.WaterLevel + ShoreHeight;

                // Rasterize the map plus enough ocean around it for the widest blur to fade out
                var data = terrain.terrainData;
                float spacing = data.size.x / (data.heightmapResolution - 1);
                float widest = Mathf.Max(world.HeightBlend, Mathf.Max(world.TextureBlend, world.ShoreBlend));
                float extent = (world.MapRadius + 1) * Hex.Spacing(world.HexRadius) + world.EdgeWobble * 2f +
                               widest * BlurReach + spacing;
                var origin = new Vector2(-extent, -extent);
                int cells = Mathf.CeilToInt(extent * 2f / spacing) + 1;

                _land = new Raster(origin, spacing, cells, _shoreLand);
                _water = new Raster(origin, spacing, cells, 1f);
                _bed = new Raster(origin, spacing, cells, _oceanBed);
                _roughness = new Raster(origin, spacing, cells, _oceanRoughness);
                _layers = new Raster[layers.Count];
                for (int l = 0; l < layers.Count; l++)
                {
                    _layers[l] = new Raster(origin, spacing, cells, l == _oceanLayer ? 1f : 0f);
                }

                Rasterize(layers);

                _land.Blur(world.HeightBlend);
                _roughness.Blur(world.HeightBlend);
                _water.Blur(world.ShoreBlend);
                _bed.Blur(world.ShoreBlend);
                foreach (var layer in _layers)
                {
                    layer.Blur(world.TextureBlend);
                }

                _shore = _water.DistanceToEdge(ShoreLine);
                _shore.Blur(ShoreSmoothing);
            }

            private void Rasterize(List<TerrainLayer> layers)
            {
                // Later palette entries paint over earlier ones; unpainted hexes (ocean) are under everything
                var priorities = new Dictionary<ChunkDefinition, int>();
                for (int i = 0; i < _world.Palette.Count; i++)
                {
                    if (_world.Palette[i] != null)
                    {
                        priorities.TryAdd(_world.Palette[i], i);
                    }
                }

                float radius = _world.HexRadius;
                float dab = radius * DabRadius;
                var chunks = new ChunkDefinition[7];
                var centers = new Vector2[7];
                var ranks = new int[7];
                var layerIndices = new int[7];
                var cachedCoord = new Vector2Int(int.MinValue, int.MinValue);

                for (int z = 0; z < _land.Size; z++)
                {
                    for (int x = 0; x < _land.Size; x++)
                    {
                        // Look the dabs up at a wobbled position, so edges between chunks wander off the grid
                        var p = Wobble(_land.Position(x, z));
                        var coord = Hex.FromPlane(p, radius);
                        if (coord != cachedCoord)
                        {
                            // Only the hex itself and its neighbours have dabs that reach this hex
                            cachedCoord = coord;
                            for (int k = 0; k < 7; k++)
                            {
                                var c = k == 0 ? coord : coord + Hex.Directions[k - 1];
                                var painted = _world.GetPainted(c);
                                chunks[k] = painted != null ? painted : _world.Ocean;
                                ranks[k] = painted != null ? priorities.GetValueOrDefault(painted, 0) : -1;
                                centers[k] = Hex.ToPlane(c, radius);
                                layerIndices[k] = LayerIndex(chunks[k], layers);
                            }
                        }

                        int top = 0;
                        int topRank = int.MinValue;
                        for (int k = 0; k < 7; k++)
                        {
                            if (ranks[k] > topRank && (p - centers[k]).sqrMagnitude <= dab * dab)
                            {
                                top = k;
                                topRank = ranks[k];
                            }
                        }

                        var chunk = chunks[top];
                        bool water = chunk == null || chunk.IsWater;
                        int i = _land.Index(x, z);
                        _land.Values[i] = water ? _shoreLand : chunk.Elevation;
                        _water.Values[i] = water ? 1f : 0f;
                        // Bed weighted by the water share; Height divides the blurred share back out
                        _bed.Values[i] = water ? Elevation(chunk) : 0f;
                        _roughness.Values[i] = chunk != null ? chunk.Roughness : 0f;
                        for (int l = 0; l < _layers.Length; l++)
                        {
                            _layers[l].Values[i] = l == layerIndices[top] ? 1f : 0f;
                        }
                    }
                }
            }

            private Vector2 Wobble(Vector2 p)
            {
                var offset = Noise2(p, _world.HexRadius * WobbleWavelength, 0f) +
                             Noise2(p, _world.HexRadius * LongWobbleWavelength, 100f) * LongWobbleShare;
                return p + offset * _world.EdgeWobble;
            }

            // Smooth 2D noise in [-1, 1]² per axis
            private static Vector2 Noise2(Vector2 p, float wavelength, float seed)
            {
                float x = Mathf.PerlinNoise(p.x / wavelength + 37.1f + seed, p.y / wavelength + 11.3f) * 2f - 1f;
                float z = Mathf.PerlinNoise(p.x / wavelength + 91.7f, p.y / wavelength + 53.9f + seed) * 2f - 1f;
                return new Vector2(x, z);
            }

            /// <param name="terrainPoint">XZ relative to the terrain's corner.</param>
            public float Height(Vector2 terrainPoint)
            {
                var p = ToWorld(terrainPoint);
                float noise = Mathf.PerlinNoise(p.x / _world.RoughnessSize + 1000f, p.y / _world.RoughnessSize + 1000f)
                              * 2f - 1f;

                // Metres from the waterline: positive on land, negative in the water
                float shore = _shore.Sample(p);
                float height;
                if (shore >= 0f)
                {
                    // Land rises from the waterline to its own height
                    float land = _land.Sample(p);
                    height = _world.WaterLevel + ShoreProfile(shore, _world.HeightBlend, land - _world.WaterLevel);
                }
                else
                {
                    // The bed falls away gently over the shelf
                    float water = _water.Sample(p);
                    float bed = water > 0.001f ? _bed.Sample(p) / water : _oceanBed;
                    height = _world.WaterLevel - ShoreProfile(-shore, _world.ShelfWidth, _world.WaterLevel - bed);
                }

                // Bumps come back past the shelf, so the shallows stay one smooth slope
                float calmWidth = shore >= 0f ? CalmShore : _world.ShelfWidth;
                float calm = Mathf.SmoothStep(0f, 1f, Mathf.Abs(shore) / calmWidth);
                return height + _roughness.Sample(p) * noise * calm;
            }

            /// <summary>
            /// Height change <paramref name="distance"/> metres away from the waterline on a slope of
            /// <paramref name="width"/> metres that changes by <paramref name="range"/> in total. An S-curve that
            /// eases into its far end, mixed with a straight line just enough to leave the waterline at
            /// <see cref="WaterlineSlope"/>.
            /// </summary>
            private static float ShoreProfile(float distance, float width, float range)
            {
                float t = Mathf.Clamp01(distance / width);
                float straight = range > 0f ? Mathf.Clamp01(WaterlineSlope * width / range) : 1f;
                return range * Mathf.Lerp(t * t * (3f - 2f * t), t, straight);
            }

            public void TextureWeights(Vector2 terrainPoint, float[] weights)
            {
                var p = ToWorld(terrainPoint);
                float sum = 0f;
                for (int l = 0; l < weights.Length; l++)
                {
                    weights[l] = l < _layers.Length ? Mathf.Max(0f, _layers[l].Sample(p)) : 0f;
                    sum += weights[l];
                }

                for (int l = 0; l < weights.Length; l++)
                {
                    weights[l] = sum > 0f ? weights[l] / sum : l == 0 ? 1f : 0f;
                }
            }

            private Vector2 ToWorld(Vector2 terrainPoint)
            {
                return terrainPoint + new Vector2(TerrainPosition.x, TerrainPosition.z);
            }

            private float Elevation(ChunkDefinition chunk)
            {
                // Without an ocean chunk the sea floor sits a little under the water
                return chunk != null ? chunk.Elevation : _world.WaterLevel - 3f;
            }

            private static int LayerIndex(ChunkDefinition chunk, List<TerrainLayer> layers)
            {
                int index = chunk != null && chunk.TerrainLayer != null ? layers.IndexOf(chunk.TerrainLayer) : 0;
                return Mathf.Max(0, index);
            }
        }

        /// <summary>A square grid of values on the XZ plane, blurred in place and sampled bilinearly.</summary>
        private class Raster
        {
            private readonly Vector2 _origin;
            private readonly float _spacing;
            private readonly float _outside;

            public int Size { get; }
            public float[] Values { get; }

            /// <param name="outside">Value everywhere off the grid.</param>
            public Raster(Vector2 origin, float spacing, int size, float outside)
            {
                _origin = origin;
                _spacing = spacing;
                _outside = outside;
                Size = size;
                Values = new float[size * size];
            }

            public int Index(int x, int z) => z * Size + x;

            public Vector2 Position(int x, int z) => _origin + new Vector2(x, z) * _spacing;

            /// <summary>Gaussian blur with the given sigma in metres; round, so it leaves no trace of the grid.</summary>
            public void Blur(float sigma)
            {
                float cellSigma = sigma / _spacing;
                int radius = Mathf.CeilToInt(cellSigma * BlurReach);
                if (radius < 1)
                {
                    return;
                }

                var kernel = new float[radius * 2 + 1];
                float sum = 0f;
                for (int i = -radius; i <= radius; i++)
                {
                    kernel[i + radius] = Mathf.Exp(-i * i / (2f * cellSigma * cellSigma));
                    sum += kernel[i + radius];
                }

                for (int i = 0; i < kernel.Length; i++)
                {
                    kernel[i] /= sum;
                }

                var temp = new float[Values.Length];
                Pass(Values, temp, kernel, radius, true);
                Pass(temp, Values, kernel, radius, false);
            }

            // One direction of the separable blur; off the grid counts as the outside value
            private void Pass(float[] source, float[] target, float[] kernel, int radius, bool horizontal)
            {
                for (int z = 0; z < Size; z++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        float value = 0f;
                        for (int k = -radius; k <= radius; k++)
                        {
                            int sx = horizontal ? x + k : x;
                            int sz = horizontal ? z : z + k;
                            float sample = sx < 0 || sz < 0 || sx >= Size || sz >= Size
                                ? _outside
                                : source[sz * Size + sx];
                            value += kernel[k + radius] * sample;
                        }

                        target[z * Size + x] = value;
                    }
                }
            }

            /// <summary>
            /// Signed distance in metres to the edge of the area where this raster is below
            /// <paramref name="threshold"/>: positive outside that area, negative inside the rest. Exact Euclidean
            /// distance, so the result has no grid directions in it. Off the grid counts as above the threshold.
            /// </summary>
            public Raster DistanceToEdge(float threshold)
            {
                var above = new bool[Values.Length];
                for (int i = 0; i < Values.Length; i++)
                {
                    above[i] = Values[i] >= threshold;
                }

                var toAbove = SquaredDistances(above, true);
                var toBelow = SquaredDistances(above, false);
                var result = new Raster(_origin, _spacing, Size, -1000f);
                for (int i = 0; i < Values.Length; i++)
                {
                    // Half a cell puts the edge between the cells instead of on them
                    result.Values[i] = above[i]
                        ? -((float)System.Math.Sqrt(toBelow[i]) - 0.5f) * _spacing
                        : ((float)System.Math.Sqrt(toAbove[i]) - 0.5f) * _spacing;
                }

                return result;
            }

            // Squared distance in cells from each cell to the nearest cell whose flag equals 'target'
            // (Felzenszwalb & Huttenlocher's distance transform: columns, then rows)
            private double[] SquaredDistances(bool[] flags, bool target)
            {
                const double far = 1e20;
                var grid = new double[flags.Length];
                for (int i = 0; i < flags.Length; i++)
                {
                    grid[i] = flags[i] == target ? 0d : far;
                }

                var f = new double[Size];
                var d = new double[Size];
                var v = new int[Size];
                var z = new double[Size + 1];
                for (int x = 0; x < Size; x++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        f[y] = grid[y * Size + x];
                    }

                    Transform1D(f, d, v, z);
                    for (int y = 0; y < Size; y++)
                    {
                        grid[y * Size + x] = d[y];
                    }
                }

                for (int y = 0; y < Size; y++)
                {
                    System.Array.Copy(grid, y * Size, f, 0, Size);
                    Transform1D(f, d, v, z);
                    System.Array.Copy(d, 0, grid, y * Size, Size);
                }

                return grid;
            }

            // Lower envelope of the parabolas rooted at each sample
            private void Transform1D(double[] f, double[] d, int[] v, double[] z)
            {
                int k = 0;
                v[0] = 0;
                z[0] = double.NegativeInfinity;
                z[1] = double.PositiveInfinity;
                for (int q = 1; q < Size; q++)
                {
                    double s = Intersection(f, v[k], q);
                    while (s <= z[k])
                    {
                        k--;
                        s = Intersection(f, v[k], q);
                    }

                    k++;
                    v[k] = q;
                    z[k] = s;
                    z[k + 1] = double.PositiveInfinity;
                }

                k = 0;
                for (int q = 0; q < Size; q++)
                {
                    while (z[k + 1] < q)
                    {
                        k++;
                    }

                    double offset = q - v[k];
                    d[q] = offset * offset + f[v[k]];
                }
            }

            private static double Intersection(double[] f, int p, int q)
            {
                return (f[q] + (double)q * q - (f[p] + (double)p * p)) / (2d * q - 2d * p);
            }

            public float Sample(Vector2 position)
            {
                var local = (position - _origin) / _spacing;
                int x0 = Mathf.FloorToInt(local.x);
                int z0 = Mathf.FloorToInt(local.y);
                if (x0 < 0 || z0 < 0 || x0 >= Size - 1 || z0 >= Size - 1)
                {
                    return _outside;
                }

                float tx = local.x - x0;
                float tz = local.y - z0;
                int i = z0 * Size + x0;
                float bottom = Mathf.Lerp(Values[i], Values[i + 1], tx);
                float top = Mathf.Lerp(Values[i + Size], Values[i + Size + 1], tx);
                return Mathf.Lerp(bottom, top, tz);
            }
        }
    }
}
