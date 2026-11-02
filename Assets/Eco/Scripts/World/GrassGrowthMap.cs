using UnityEngine;
using UnityEngine.Rendering;

namespace Eco.Scripts.World
{
    /// <summary>
    /// When each terrain detail cell's grass is fully grown, for grass that grows in (<see cref="TerrainPainter.GrowGrass"/>)
    /// instead of appearing at once. Two float maps with one texel per detail cell, so blades and ground can cover
    /// different cells: the blades map is read by the terrain grass shader, which scales each blade by its cell's
    /// growth (0, never stamped, is grown); the ground map by the terrain shader, which grows the grass layer into
    /// the ground of stamped cells until <see cref="TerrainPainter"/> writes it into the alphamap
    /// (<c>GrassGrowth.hlsl</c>). Both are <see cref="CellMap{T}"/>s of times, so nothing runs per frame.
    /// </summary>
    public static class GrassGrowthMap
    {
        /// <summary>
        /// Grown cells have their grass layer written into the alphamap in batches this many seconds apart. The
        /// shaders keep reading the maps a second longer than that after the last cell is grown.
        /// </summary>
        public const float GroundCommitInterval = 2f;

        private static readonly int TransformId = Shader.PropertyToID("_EcoGrassGrowthMapTransform");
        private static readonly int GrowthId = Shader.PropertyToID("_EcoGrassGrowth");
        private static readonly int GroundMaskId = Shader.PropertyToID("_EcoGrassGroundMask");

        private static readonly CellMap<float> Blades =
            new("_EcoGrassGrowthMap", "Grass Growth Map", TextureFormat.RFloat);
        private static readonly CellMap<float> Ground =
            new("_EcoGroundGrowthMap", "Ground Growth Map", TextureFormat.RFloat);
        private static float _inverseDuration;
        private static float _lastGrownAt;
        private static int _width;
        private static int _height;
        private static bool _supported;

        /// <summary>
        /// Fresh maps for a new game on <paramref name="data"/>: everything grown. The ground grows terrain layer
        /// <paramref name="grassLayer"/> (one of the first four, or none).
        /// </summary>
        public static void Reset(TerrainData data, Vector3 terrainPosition, int grassLayer)
        {
            _inverseDuration = 0f;
            _lastGrownAt = 0f;
            _width = data.detailWidth;
            _height = data.detailHeight;

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

            Blades.Reset(_width, _height);
            Ground.Reset(_width, _height);

            Vector3 size = data.size;
            Shader.SetGlobalVector(TransformId, new Vector4(terrainPosition.x, terrainPosition.z,
                _width / size.x, _height / size.z));
            SetGrowthGlobals();
        }

        /// <summary>
        /// The blades in detail cell (<paramref name="x"/>, <paramref name="z"/>) start growing
        /// <paramref name="growDuration"/> seconds before <paramref name="grownAt"/> (<see cref="Time.time"/>) and
        /// are invisible until then. All cells share one grow duration, the latest stamped. Sent to the GPU by
        /// <see cref="Upload"/>.
        /// </summary>
        public static void StampBlades(int x, int z, float grownAt, float growDuration)
        {
            if (Track(grownAt, growDuration))
            {
                Blades.Stamp(x, z, grownAt);
            }
        }

        /// <summary>Like <see cref="StampBlades"/>, for the grass layer in the cell's ground.</summary>
        public static void StampGround(int x, int z, float grownAt, float growDuration)
        {
            if (Track(grownAt, growDuration))
            {
                Ground.Stamp(x, z, grownAt);
            }
        }

        /// <summary>Writes the stamped cells into the maps. Call before their grass is painted.</summary>
        public static void Upload()
        {
            if (!_supported)
            {
                return;
            }

            bool blades = Blades.Upload();
            bool ground = Ground.Upload();
            if (blades || ground)
            {
                SetGrowthGlobals();
            }
        }

        private static bool Track(float grownAt, float growDuration)
        {
            if (!_supported)
            {
                return false;
            }

            _inverseDuration = 1f / Mathf.Max(growDuration, 0.01f);
            _lastGrownAt = Mathf.Max(_lastGrownAt, grownAt);
            return true;
        }

        private static void SetGrowthGlobals()
        {
            float readUntil = _lastGrownAt > 0f ? _lastGrownAt + GroundCommitInterval + 1f : 0f;
            Shader.SetGlobalVector(GrowthId, new Vector4(_inverseDuration, readUntil, _width, _height));
        }
    }
}
