using Cysharp.Threading.Tasks;
using Eco.Scripts.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace Eco.Scripts.World
{
    /// <summary>
    /// The dead ground under loose scrap. Two <see cref="CellMap{T}"/>s tell the terrain shader which detail cells are
    /// stained and in what tint (<c>TerrainScrapStain.hlsl</c>), and it paints over whatever ground is there; the
    /// terrain itself never changes, so the natural ground (grass that grew meanwhile included) simply shows again once
    /// a stain is gone. Each scrap item adds a stain when it comes to rest and removes it when it leaves, for a pickup
    /// or with its chunk. A stain is full strength at its centre and fades out to nothing at its radius
    /// (<see cref="WorldVisualSettings.ScrapStainRadius"/>, read once per game). Cells shared by several stains clean
    /// up with the last of them and take the colour of the strongest. Removing a stain leaves its tint in the map, so
    /// the ground heals in the colour it had.
    /// </summary>
    public static class ScrapStainMap
    {
        // Used when there are no world visual settings
        private const float DefaultRadius = 2.2f;

        // Texel of stained ground: it was "cleaned" so far ahead that the shader's fade never starts
        private const float Stained = 1e9f;

        private static readonly int TransformId = Shader.PropertyToID("_EcoScrapStainMapTransform");
        private static readonly int StainId = Shader.PropertyToID("_EcoScrapStain");

        private static readonly CellMap<float> Map = new("_EcoScrapStainMap", "Scrap Stain Map", TextureFormat.RFloat);
        // Colour premultiplied by the stain's strength in alpha. Filtered, which is what makes the fade a smooth one and
        // lets neighbouring tints run into each other; 0 where nothing ever stained the ground.
        private static readonly CellMap<Color32> Tints =
            new("_EcoScrapTintMap", "Scrap Tint Map", TextureFormat.RGBA32, FilterMode.Bilinear);
        private static byte[] _stainsPerCell;
        // The strongest stain now on each cell, as stamped in the tint map's alpha
        private static byte[] _cellStrength;
        private static float _radius;
        private static int _width;
        private static int _height;
        private static Vector3 _terrainPosition;
        private static Vector2 _cellsPerMetre;
        private static int _generation;
        private static bool _uploadScheduled;
        private static bool _supported;

        /// <summary>Clean ground for a new game on <paramref name="data"/>.</summary>
        public static void Reset(TerrainData data, Vector3 terrainPosition)
        {
            _generation++;
            _uploadScheduled = false;
            _width = data.detailWidth;
            _height = data.detailHeight;
            _terrainPosition = terrainPosition;
            _cellsPerMetre = new Vector2(_width / data.size.x, _height / data.size.z);

            _supported = SystemInfo.copyTextureSupport != CopyTextureSupport.None &&
                         SystemInfo.SupportsTextureFormat(TextureFormat.RFloat);
            if (!_supported)
            {
                Debug.LogWarning("Scrap can't stain the ground on this device (no texture copy or float textures).");
                Shader.SetGlobalVector(StainId, Vector4.zero);
                return;
            }

            var controller = CurvedWorldController.Active;
            _radius = controller != null && controller.Settings != null
                ? controller.Settings.ScrapStainRadius
                : DefaultRadius;
            _stainsPerCell = new byte[_width * _height];
            _cellStrength = new byte[_width * _height];
            Map.Reset(_width, _height);
            Tints.Reset(_width, _height);
            Shader.SetGlobalVector(TransformId, new Vector4(terrainPosition.x, terrainPosition.z,
                _cellsPerMetre.x, _cellsPerMetre.y));
            Shader.SetGlobalVector(StainId, new Vector4(1f, 0f, _width, _height));

            // The globals outlive play mode, and edit mode has to show the clean terrain
            Application.quitting -= Clear;
            Application.quitting += Clear;
        }

        private static void Clear() => Shader.SetGlobalVector(StainId, Vector4.zero);

        /// <summary>
        /// Stains the ground around <paramref name="position"/> in <paramref name="tint"/>; the world visual settings
        /// decide how strong it shows.
        /// </summary>
        public static void Add(Vector3 position, Color tint)
        {
            ChangeCells(position, true, tint.linear);
        }

        /// <summary>Takes back an <see cref="Add"/>; the ground there starts turning back to what it was.</summary>
        public static void Remove(Vector3 position)
        {
            ChangeCells(position, false, default);
        }

        private static void ChangeCells(Vector3 position, bool stain, Color tint)
        {
            if (!_supported)
            {
                return;
            }

            float cx = (position.x - _terrainPosition.x) * _cellsPerMetre.x;
            float cz = (position.z - _terrainPosition.z) * _cellsPerMetre.y;
            int zMin = Mathf.Max(0, Mathf.FloorToInt(cz - _radius * _cellsPerMetre.y));
            int zMax = Mathf.Min(_height - 1, Mathf.FloorToInt(cz + _radius * _cellsPerMetre.y));
            int xMin = Mathf.Max(0, Mathf.FloorToInt(cx - _radius * _cellsPerMetre.x));
            int xMax = Mathf.Min(_width - 1, Mathf.FloorToInt(cx + _radius * _cellsPerMetre.x));
            for (int z = zMin; z <= zMax; z++)
            {
                // Metres from the centre
                float dz = (z + 0.5f - cz) / _cellsPerMetre.y;
                for (int x = xMin; x <= xMax; x++)
                {
                    float dx = (x + 0.5f - cx) / _cellsPerMetre.x;
                    float distance = Mathf.Sqrt(dx * dx + dz * dz);
                    if (distance > _radius) continue;

                    int cell = z * _width + x;
                    if (stain)
                    {
                        float strength = 1f - distance / _radius;
                        byte alpha = (byte)Mathf.RoundToInt(strength * 255f);
                        if (alpha >= _cellStrength[cell])
                        {
                            _cellStrength[cell] = alpha;
                            Tints.Stamp(x, z, new Color(tint.r * strength, tint.g * strength, tint.b * strength, strength));
                        }

                        if (_stainsPerCell[cell]++ == 0)
                        {
                            Map.Stamp(x, z, Stained);
                        }
                    }
                    else if (_stainsPerCell[cell] > 0 && --_stainsPerCell[cell] == 0)
                    {
                        _cellStrength[cell] = 0;
                        Map.Stamp(x, z, Time.time);
                    }
                }
            }

            if (!_uploadScheduled)
            {
                _uploadScheduled = true;
                UploadNextFrame(_generation).Forget();
            }
        }

        // Everything a frame's chunks and pickups changed goes up together
        private static async UniTaskVoid UploadNextFrame(int generation)
        {
            await UniTask.NextFrame();

            // Reset started another game meanwhile
            if (generation != _generation)
            {
                return;
            }

            _uploadScheduled = false;
            using var marker = EcoProfilerMarkers.ScrapStain.Auto();
            Map.Upload();
            Tints.Upload();
        }
    }
}
