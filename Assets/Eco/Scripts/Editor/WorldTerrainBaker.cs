using System.Collections.Generic;
using Eco.Scripts.World;
using UnityEditor;
using UnityEngine;

namespace Eco.Scripts.EditorTools
{
    /// <summary>
    /// Writes a <see cref="WorldData"/> into the scene terrain: heights from each hex's elevation and roughness,
    /// splat textures from each hex's terrain layer, both blended across hex edges, and no grass (the game paints
    /// grass around trees). Land never dips under the water, and keeps a dry rim a little way into neighbouring
    /// water hexes, so every shoreline lies under a water hex's surface.
    /// </summary>
    internal static class WorldTerrainBaker
    {
        // Land stays at least this far above the water; the game's NavMesh starts just below it
        private const float LandAboveWater = 0.25f;

        // The dry rim reaches this far into water hexes, then slopes down to the bed over ShoreSlope.
        // The rim must be wider than the heightmap spacing, or a sample on land could see water next to it.
        private const float ShoreRim = 1.5f;
        private const float ShoreSlope = 2f;

        // Heightmap of at most ~1 m per sample, so the rim above holds
        private const float MaxMetresPerHeightSample = 1.05f;

        private const int MaxHeightmapResolution = 4097;

        public static void Bake(WorldData world, Terrain terrain)
        {
            var data = terrain.terrainData;
            var sampler = new Sampler(world, terrain.GetPosition());

            try
            {
                EditorUtility.DisplayProgressBar("Bake World Terrain", "Layers", 0f);
                var layers = BakeLayers(world, data, sampler);

                EditorUtility.DisplayProgressBar("Bake World Terrain", "Heights", 0.2f);
                EnsureHeightmapResolution(data);
                BakeHeights(data, sampler);

                EditorUtility.DisplayProgressBar("Bake World Terrain", "Textures", 0.6f);
                BakeTextures(data, sampler, layers);

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
        private static Dictionary<TerrainLayer, int> BakeLayers(WorldData world, TerrainData data, Sampler sampler)
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

            var indices = new Dictionary<TerrainLayer, int>();
            for (int i = 0; i < layers.Count; i++)
            {
                indices[layers[i]] = i;
            }

            return indices;
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

        private static void BakeHeights(TerrainData data, Sampler sampler)
        {
            int resolution = data.heightmapResolution;
            var size = data.size;
            var heights = new float[resolution, resolution];
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    var p = new Vector2(x / (float)(resolution - 1) * size.x, z / (float)(resolution - 1) * size.z);
                    heights[z, x] = Mathf.Clamp01((sampler.Height(p) - sampler.TerrainPosition.y) / size.y);
                }
            }

            data.SetHeights(0, 0, heights);
        }

        private static void BakeTextures(TerrainData data, Sampler sampler, Dictionary<TerrainLayer, int> layers)
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
                    sampler.TextureWeights(p, layers, weights);
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

        /// <summary>Terrain height and texture weights at a point, from the hex it's in and the six around it.</summary>
        private class Sampler
        {
            private readonly WorldData _world;
            private readonly float _radius;
            private readonly float _heightBlend;
            private readonly float _textureBlend;
            private readonly float _shoreRim;
            private readonly float _shoreSlope;
            private readonly ChunkDefinition[] _chunks = new ChunkDefinition[7];
            private readonly Vector2[] _centers = new Vector2[7];
            private Vector2Int _cached = new(int.MinValue, int.MinValue);

            public Vector3 TerrainPosition { get; }

            public Sampler(WorldData world, Vector3 terrainPosition)
            {
                _world = world;
                _radius = world.HexRadius;
                TerrainPosition = terrainPosition;

                // Blends wider than the hex would reach past the six neighbours looked at
                _heightBlend = Mathf.Min(world.HeightBlend, _radius * 0.9f);
                _textureBlend = Mathf.Min(world.TextureBlend, _radius * 0.9f);
                _shoreRim = Mathf.Min(ShoreRim, _radius * 0.25f);
                _shoreSlope = Mathf.Min(ShoreSlope, _radius * 0.35f);
            }

            /// <param name="terrainPoint">XZ relative to the terrain's corner.</param>
            public float Height(Vector2 terrainPoint)
            {
                var p = ToWorld(terrainPoint);
                Load(p);

                float noise = Mathf.PerlinNoise(p.x / _world.RoughnessSize + 1000f, p.y / _world.RoughnessSize + 1000f)
                              * 2f - 1f;
                float sum = 0f;
                float weightSum = 0f;
                float landDistance = float.MaxValue;
                for (int i = 0; i < 7; i++)
                {
                    var chunk = _chunks[i];
                    float distance = Hex.SignedDistance(p - _centers[i], _radius);
                    float weight = Weight(distance, _heightBlend);
                    sum += weight * (Elevation(chunk) + (chunk != null ? chunk.Roughness * noise : 0f));
                    weightSum += weight;

                    if (chunk != null && !chunk.IsWater)
                    {
                        landDistance = Mathf.Min(landDistance, distance);
                    }
                }

                float height = sum / weightSum;
                if (landDistance == float.MaxValue)
                {
                    return height;
                }

                // Land and its rim stay dry; past the rim the shore slopes into the water
                float dry = _world.WaterLevel + LandAboveWater;
                float rim = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_shoreRim, _shoreRim + _shoreSlope,
                    landDistance));
                return Mathf.Max(height, Mathf.Lerp(height, dry, rim));
            }

            public void TextureWeights(Vector2 terrainPoint, Dictionary<TerrainLayer, int> layers, float[] weights)
            {
                var p = ToWorld(terrainPoint);
                Load(p);

                System.Array.Clear(weights, 0, weights.Length);
                float weightSum = 0f;
                for (int i = 0; i < 7; i++)
                {
                    var layer = _chunks[i] != null ? _chunks[i].TerrainLayer : null;
                    int index = layer != null && layers.TryGetValue(layer, out int found) ? found : 0;
                    float weight = Weight(Hex.SignedDistance(p - _centers[i], _radius), _textureBlend);
                    weights[index] += weight;
                    weightSum += weight;
                }

                for (int l = 0; l < weights.Length; l++)
                {
                    weights[l] /= weightSum;
                }
            }

            private Vector2 ToWorld(Vector2 terrainPoint)
            {
                return terrainPoint + new Vector2(TerrainPosition.x, TerrainPosition.z);
            }

            // Neighbouring samples are mostly in the same hex; look its chunks up once
            private void Load(Vector2 p)
            {
                var coord = Hex.FromPlane(p, _radius);
                if (coord == _cached)
                {
                    return;
                }

                _cached = coord;
                _chunks[0] = _world.GetChunk(coord);
                _centers[0] = Hex.ToPlane(coord, _radius);
                for (int i = 0; i < 6; i++)
                {
                    var neighbour = coord + Hex.Directions[i];
                    _chunks[i + 1] = _world.GetChunk(neighbour);
                    _centers[i + 1] = Hex.ToPlane(neighbour, _radius);
                }
            }

            private float Elevation(ChunkDefinition chunk)
            {
                // Without an ocean chunk the sea floor sits a little under the water
                return chunk != null ? chunk.Elevation : _world.WaterLevel - 3f;
            }

            // 1 deep inside the hex, ½ on its edge, 0 once 'blend' outside
            private static float Weight(float signedDistance, float blend)
            {
                return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(0.5f - signedDistance / (2f * blend)));
            }
        }
    }
}
