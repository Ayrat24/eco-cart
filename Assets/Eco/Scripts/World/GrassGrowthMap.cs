using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Eco.Scripts.World
{
    /// <summary>
    /// When each terrain detail cell's grass is fully grown, for grass that grows in (<see cref="TerrainPainter.GrowGrass"/>)
    /// instead of appearing at once. One float texel per detail cell, read by the terrain grass shader, which scales
    /// each blade by its cell's growth (cells never stamped are grown), and by the terrain shader, which grows the
    /// grass layer into the ground of stamped cells until <see cref="TerrainPainter"/> writes it into the alphamap
    /// (<c>GrassGrowth.hlsl</c>). The map lives only on the GPU: stamps go up through a small staging row and a GPU
    /// copy per cell, so stamping a few cells never uploads the whole map, and nothing runs per frame.
    /// </summary>
    public static class GrassGrowthMap
    {
        /// <summary>
        /// Grown cells have their grass layer written into the alphamap in batches this many seconds apart. The
        /// shaders keep reading the map a second longer than that after the last cell is grown.
        /// </summary>
        public const float GroundCommitInterval = 2f;

        private const int StagingSize = 256;

        private static readonly int MapId = Shader.PropertyToID("_EcoGrassGrowthMap");
        private static readonly int TransformId = Shader.PropertyToID("_EcoGrassGrowthMapTransform");
        private static readonly int GrowthId = Shader.PropertyToID("_EcoGrassGrowth");
        private static readonly int GroundMaskId = Shader.PropertyToID("_EcoGrassGroundMask");

        private static readonly List<(int x, int z, float grownAt)> PendingStamps = new();
        private static Texture2D _map;
        private static Texture2D _staging;
        private static float _inverseDuration;
        private static float _lastGrownAt;
        private static bool _supported;

        /// <summary>
        /// A fresh map for a new game on <paramref name="data"/>: everything grown. The ground grows terrain layer
        /// <paramref name="grassLayer"/> (one of the first four, or none).
        /// </summary>
        public static void Reset(TerrainData data, Vector3 terrainPosition, int grassLayer)
        {
            PendingStamps.Clear();
            _inverseDuration = 0f;
            _lastGrownAt = 0f;

            var groundMask = Vector4.zero;
            if (grassLayer is >= 0 and < 4)
            {
                groundMask[grassLayer] = 1f;
            }
            Shader.SetGlobalVector(GroundMaskId, groundMask);

            _supported = SystemInfo.copyTextureSupport != CopyTextureSupport.None &&
                         SystemInfo.SupportsTextureFormat(TextureFormat.RFloat);
            if (!_supported)
            {
                Debug.LogWarning("Grass can't grow in on this device (no texture copy or float textures); " +
                                 "it appears at once instead.");
                Shader.SetGlobalVector(GrowthId, Vector4.zero);
                return;
            }

            if (_map != null)
            {
                Object.Destroy(_map);
            }

            // Zero is grown. The map is never read back, so the CPU copy goes once it is uploaded.
            _map = new Texture2D(data.detailWidth, data.detailHeight, TextureFormat.RFloat, false, true)
            {
                name = "Grass Growth Map",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = _map.GetPixelData<float>(0);
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = 0f;
            }
            _map.Apply(false, true);

            if (_staging == null)
            {
                _staging = new Texture2D(StagingSize, 1, TextureFormat.RFloat, false, true)
                {
                    name = "Grass Growth Staging",
                    filterMode = FilterMode.Point
                };
            }

            Vector3 size = data.size;
            Shader.SetGlobalTexture(MapId, _map);
            Shader.SetGlobalVector(TransformId, new Vector4(terrainPosition.x, terrainPosition.z,
                data.detailWidth / size.x, data.detailHeight / size.z));
            SetGrowthGlobals();
        }

        /// <summary>
        /// Detail cell (<paramref name="x"/>, <paramref name="z"/>) starts growing <paramref name="growDuration"/>
        /// seconds before <paramref name="grownAt"/> (<see cref="Time.time"/>) and is invisible until then. All
        /// cells share one grow duration, the latest stamped. Sent to the GPU by <see cref="Upload"/>.
        /// </summary>
        public static void Stamp(int x, int z, float grownAt, float growDuration)
        {
            if (!_supported)
            {
                return;
            }

            PendingStamps.Add((x, z, grownAt));
            _inverseDuration = 1f / Mathf.Max(growDuration, 0.01f);
            _lastGrownAt = Mathf.Max(_lastGrownAt, grownAt);
        }

        /// <summary>Writes the stamped cells into the map. Call before their grass is painted.</summary>
        public static void Upload()
        {
            if (!_supported || PendingStamps.Count == 0)
            {
                PendingStamps.Clear();
                return;
            }

            var staging = _staging.GetPixelData<float>(0);
            for (int start = 0; start < PendingStamps.Count; start += StagingSize)
            {
                int count = Mathf.Min(StagingSize, PendingStamps.Count - start);
                for (int i = 0; i < count; i++)
                {
                    staging[i] = PendingStamps[start + i].grownAt;
                }
                _staging.Apply(false);

                for (int i = 0; i < count; i++)
                {
                    var stamp = PendingStamps[start + i];
                    Graphics.CopyTexture(_staging, 0, 0, i, 0, 1, 1, _map, 0, 0, stamp.x, stamp.z);
                }
            }

            PendingStamps.Clear();
            SetGrowthGlobals();
        }

        private static void SetGrowthGlobals()
        {
            float readUntil = _lastGrownAt > 0f ? _lastGrownAt + GroundCommitInterval + 1f : 0f;
            Shader.SetGlobalVector(GrowthId, new Vector4(_inverseDuration, readUntil, _map.width, _map.height));
        }
    }
}
