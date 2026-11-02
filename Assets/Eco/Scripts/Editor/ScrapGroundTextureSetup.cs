using System.IO;
using UnityEditor;
using UnityEngine;

namespace Eco.Scripts.EditorTools
{
    /// <summary>Bakes the seamless data texture of the ground under scrap once, with no runtime generation.</summary>
    public static class ScrapGroundTextureSetup
    {
        public const string TexturePath = "Assets/Eco/Data/Textures/Terrain/ScrapGround.png";
        private const int Resolution = 512;
        private const int CrackCells = 9;

        [MenuItem("Eco/Rendering/Rebuild Scrap Ground Texture")]
        public static void Rebuild()
        {
            var random = new System.Random(52817);
            var grid = new Vector2[CrackCells * CrackCells];
            for (int i = 0; i < grid.Length; i++)
                grid[i] = new Vector2((float)random.NextDouble(), (float)random.NextDouble());

            var pixels = new Color[Resolution * Resolution];
            for (int y = 0; y < Resolution; y++)
            {
                for (int x = 0; x < Resolution; x++)
                {
                    var uv = new Vector2((x + 0.5f) / Resolution, (y + 0.5f) / Resolution);
                    // Scorched crust: mottled patches, gritty speckle, thin wandering cracks that only show in places,
                    // and tarry puddles
                    float blotches = Stretch(Fractal(uv, 6, 6, 11f));
                    float grit = Fractal(uv, 64, 3, 59f) - 0.5f;
                    float crack = 1f - Mathf.SmoothStep(0f, 1f, CrackDistance(uv + new Vector2(
                        Fractal(uv, 5, 4, 31f) - 0.5f, Fractal(uv, 5, 4, 47f) - 0.5f) * 0.14f, grid) / 0.012f);
                    crack *= Mathf.SmoothStep(0.42f, 0.6f, Fractal(uv, 4, 3, 139f));
                    float tar = Mathf.SmoothStep(0.54f, 0.62f, Fractal(uv, 5, 5, 83f));
                    float grime = (0.3f + blotches * 0.7f + grit * 0.5f) * (1f - crack * 0.65f) * (1f - tar * 0.8f);

                    pixels[y * Resolution + x] = new Color(Mathf.Clamp01(grime),
                        Mathf.Clamp01(Stretch(Fractal(uv, 3, 3, 113f))),
                        Mathf.Clamp01(Stretch(Fractal(uv, 3, 3, 167f))),
                        Mathf.Clamp01(Stretch(Fractal(uv, 9, 4, 211f))));
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(TexturePath));
            var texture = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false, true);
            try
            {
                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            // Masks and noise, not colour; the shader colours it from the world visual settings
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.anisoLevel = 4;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        // Fractal noise clusters around the middle; the shaders want the full range
        private static float Stretch(float noise) => (noise - 0.5f) * 2.2f + 0.5f;

        // Sums wrapped value noise, doubling the cells each octave, so every octave tiles
        private static float Fractal(Vector2 uv, int cells, int octaves, float seed)
        {
            float sum = 0f;
            float amplitude = 0.5f;
            float total = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Noise(uv * cells, cells, seed + i * 19f) * amplitude;
                total += amplitude;
                amplitude *= 0.5f;
                cells *= 2;
            }
            return sum / total;
        }

        private static float Noise(Vector2 position, int period, float seed)
        {
            int x = Mathf.FloorToInt(position.x);
            int y = Mathf.FloorToInt(position.y);
            float fx = position.x - x;
            float fy = position.y - y;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            return Mathf.Lerp(
                Mathf.Lerp(Hash(x, y, period, seed), Hash(x + 1, y, period, seed), fx),
                Mathf.Lerp(Hash(x, y + 1, period, seed), Hash(x + 1, y + 1, period, seed), fx), fy);
        }

        private static float Hash(int x, int y, int period, float seed)
        {
            x = (x % period + period) % period;
            y = (y % period + period) % period;
            float value = Mathf.Sin(x * 127.1f + y * 311.7f + seed * 74.7f) * 43758.5453f;
            return value - Mathf.Floor(value);
        }

        // Distance in uv to the nearest edge between the plates of a wrapped Voronoi grid
        private static float CrackDistance(Vector2 uv, Vector2[] grid)
        {
            var scaled = uv * CrackCells;
            int cellX = Mathf.FloorToInt(scaled.x);
            int cellY = Mathf.FloorToInt(scaled.y);
            float nearest = float.MaxValue;
            float second = float.MaxValue;
            for (int y = cellY - 1; y <= cellY + 1; y++)
            {
                for (int x = cellX - 1; x <= cellX + 1; x++)
                {
                    int wrapped = ((y % CrackCells + CrackCells) % CrackCells) * CrackCells +
                                  (x % CrackCells + CrackCells) % CrackCells;
                    float distance = (new Vector2(x, y) + grid[wrapped] - scaled).magnitude;
                    if (distance < nearest)
                    {
                        second = nearest;
                        nearest = distance;
                    }
                    else if (distance < second)
                    {
                        second = distance;
                    }
                }
            }
            return (second - nearest) / CrackCells;
        }
    }
}
