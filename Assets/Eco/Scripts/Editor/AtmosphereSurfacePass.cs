using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Eco.Scripts.EditorTools
{
    /// <summary>Reuse each surface shader's exact depth geometry, including terrain holes and water waves.</summary>
    public static class AtmosphereSurfacePass
    {
        public static string AddToShader(string shader)
        {
            if (shader.Contains("\"EcoAtmosphereSurface\"")) return shader;
            foreach (Match match in Regex.Matches(shader, @"\bPass\s*\{"))
            {
                int brace = shader.IndexOf('{', match.Index);
                int nesting = 1;
                int end = brace + 1;
                for (; end < shader.Length && nesting > 0; end++)
                {
                    if (shader[end] == '{') nesting++;
                    else if (shader[end] == '}') nesting--;
                }
                string pass = shader.Substring(match.Index, end - match.Index);
                if (!Regex.IsMatch(pass, "Name\\s+\"DepthOnly\"")) continue;
                string surface = pass.Replace("\"DepthOnly\"", "\"EcoAtmosphereSurface\"");
                surface = Regex.Replace(surface, @"[ \t]+(?=\r?$)", "", RegexOptions.Multiline);
                return shader.Insert(end, "\n\n        // Terrain/water silhouette for the atmosphere; never used by the camera depth copy.\n        " + surface);
            }
            throw new InvalidOperationException("Surface shader has no DepthOnly pass; review atmosphere integration.");
        }

        public static void UpdateGeneratedShaders()
        {
            foreach (string name in new[] { "TerrainLit.shader", "TerrainLitBase.shader", "CurvedWater.shader" })
            {
                string path = "Assets/Eco/Data/Shaders/CurvedWorld/" + name;
                string original = File.ReadAllText(path);
                string updated = AddToShader(original);
                if (original == updated) continue;
                File.WriteAllText(path, updated);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
        }
    }
}
