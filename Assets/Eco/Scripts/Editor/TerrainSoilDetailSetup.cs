using System.IO;
using UnityEditor;
using UnityEngine;

namespace Eco.Scripts.EditorTools
{
    /// <summary>Bakes seamless pore gradients and cavity data once, with no runtime generation.</summary>
    public static class TerrainSoilDetailSetup
    {
        public const string TexturePath = "Assets/Eco/Data/Textures/Terrain/SoilDetail.png";
        private const int Resolution = 512;

        [MenuItem("Eco/Rendering/Rebuild Soil Detail Texture")]
        public static void Rebuild()
        {
            var random = new System.Random(73129);
            var heights = new float[Resolution * Resolution];
            var cavities = new float[heights.Length];
            // Broad, low clumps underneath scattered shallow pores. Wrap every stamp so
            // both the height field and its derivatives are continuous across tile edges.
            for (int i = 0; i < 170; i++)
                Stamp(heights, cavities, random, 14, 32, 0.5f, 2.0f, false);
            for (int i = 0; i < 280; i++)
                Stamp(heights, cavities, random, 5, 16, 1.0f, 3.0f, true);
            for (int i = 0; i < 450; i++)
                Stamp(heights, cavities, random, 2, 5, 0.15f, 0.5f, true);

            var pixels = new Color[heights.Length];
            for (int y = 0; y < Resolution; y++)
            {
                for (int x = 0; x < Resolution; x++)
                {
                    float dx = (heights[Index(x + 1, y)] - heights[Index(x - 1, y)]) * 0.5f;
                    float dy = (heights[Index(x, y + 1)] - heights[Index(x, y - 1)]) * 0.5f;
                    pixels[Index(x, y)] = new Color(Mathf.Clamp01(dx * 0.5f + 0.5f),
                        Mathf.Clamp01(dy * 0.5f + 0.5f), Mathf.Clamp01(cavities[Index(x, y)]), 1);
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
            // This is gradient/cavity data, not a conventional Unity normal-map encoding.
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.anisoLevel = 4;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static int Index(int x, int y)
        {
            return ((y + Resolution) % Resolution) * Resolution + (x + Resolution) % Resolution;
        }

        private static void Stamp(float[] heights, float[] cavities, System.Random random,
            float minRadius, float maxRadius, float minDepth, float maxDepth, bool pore)
        {
            float cx = (float)random.NextDouble() * Resolution;
            float cy = (float)random.NextDouble() * Resolution;
            float radius = Mathf.Lerp(minRadius, maxRadius, (float)random.NextDouble());
            float depth = Mathf.Lerp(minDepth, maxDepth, (float)random.NextDouble());
            float aspect = Mathf.Lerp(0.6f, 1.0f, (float)random.NextDouble());
            float angle = (float)random.NextDouble() * Mathf.PI * 2;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            for (int y = Mathf.FloorToInt(cy - radius); y <= Mathf.CeilToInt(cy + radius); y++)
            {
                for (int x = Mathf.FloorToInt(cx - radius); x <= Mathf.CeilToInt(cx + radius); x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float u = (cos * dx + sin * dy) / radius;
                    float v = (-sin * dx + cos * dy) / (radius * aspect);
                    float bowl = Mathf.Max(0, 1 - u * u - v * v);
                    bowl = bowl * bowl * bowl;
                    int index = Index(x, y);
                    heights[index] += bowl * depth * (pore ? -1 : 1);
                    if (pore) cavities[index] += bowl * 0.65f;
                }
            }
        }
    }
}
