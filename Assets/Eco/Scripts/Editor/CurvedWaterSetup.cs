using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Eco.Scripts.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Eco.Scripts.EditorTools
{
    public static class CurvedWaterSetup
    {
        private const string GraphPath = "Assets/Bitgem/StylisedWater/URP/Shaders/WaterVolume-URP.shadergraph";
        private const string ShaderPath = "Assets/Eco/Data/Shaders/CurvedWorld/CurvedWater.shader";
        private const string MaterialPath = "Assets/Eco/Data/Materials/CurvedWorld/WaterCurved.mat";
        private const string PrefabPath = "Assets/Eco/Data/Prefabs/Chunks/WaterChunk.prefab";
        private const string ShaderName = "Eco/Curved World/Water";

        [MenuItem("Eco/Rendering/Apply Curved Water")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Apply water materials in Edit mode.");

            GenerateShader();
            var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var source = materials[i];
                        if (source == null || source.shader.name != "Shader Graphs/WaterVolume-URP") continue;
                        var curved = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                        if (curved == null)
                        {
                            curved = new Material(source) { name = "Water Curved", shader = Shader.Find(ShaderName) };
                            // Assigning a shader resets the render queue and pass state. Water must
                            // draw after opaque depth/color are available for refraction and foam.
                            curved.renderQueue = source.renderQueue;
                            for (int pass = 0; pass < source.passCount; pass++)
                            {
                                string passName = source.GetPassName(pass);
                                curved.SetShaderPassEnabled(passName, source.GetShaderPassEnabled(passName));
                            }
                            // Shader Graph supplies default textures outside the generated shader text.
                            // Material copying alone can miss those implicit texture assignments.
                            for (int property = 0; property < source.shader.GetPropertyCount(); property++)
                            {
                                if (source.shader.GetPropertyType(property) != ShaderPropertyType.Texture) continue;
                                string name = source.shader.GetPropertyName(property);
                                if (curved.HasProperty(name) && source.GetTexture(name) != null)
                                    curved.SetTexture(name, source.GetTexture(name));
                            }
                            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
                            AssetDatabase.Refresh();
                            AssetDatabase.CreateAsset(curved, MaterialPath);
                        }
                        materials[i] = curved;
                    }
                    renderer.sharedMaterials = materials;
                }
                if (prefab.GetComponent<CurvedWorldBounds>() == null)
                    prefab.AddComponent<CurvedWorldBounds>();
                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            Debug.Log("[CurvedWorld] Water chunk now uses the shared world bend, preserving waves and foam.");
        }

        public static void GenerateShader()
        {
            // Use the installed graph importer to preserve its upgraded URP passes and all graph effects.
            var importer = AssetImporter.GetAtPath(GraphPath);
            var method = importer.GetType().GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .SingleOrDefault(m => m.Name == "GetShaderText" && m.GetParameters().Length == 4 && m.GetParameters()[3].IsOut);
            if (method == null) throw new InvalidOperationException("Shader Graph export API changed; review the water adapter.");
            var args = new object[] { GraphPath, null, null, null };
            string shader = (string)method.Invoke(null, args);
            const string originalName = "Shader \"Shader Graphs/WaterVolume-URP\"";
            if (string.IsNullOrEmpty(shader) || !shader.Contains(originalName))
                throw new InvalidOperationException("Water Shader Graph generation failed.");
            shader = shader.Replace(originalName, "Shader \"" + ShaderName + "\"");
            shader = PreserveMaterialNormals(shader);
            const string function = "VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)";
            if (!shader.Contains(function)) throw new InvalidOperationException("Water graph vertex signature changed.");
            shader = shader.Replace(function,
                "#include \"Assets/Eco/Data/Shaders/CurvedWorld/CurvedWorld.hlsl\"\n" + function);
            int hooks = 0;
            shader = Regex.Replace(shader,
                @"VertexDescription VertexDescriptionFunction\(VertexDescriptionInputs IN\)\s*\{.*?return description;",
                match =>
                {
                    hooks++;
                    string bend = match.Value.Contains("description.Normal =")
                        ? "float4 ecoTangent = float4(description.Tangent, 1);\nEcoBendVertex(description.Position, description.Normal, ecoTangent);\ndescription.Tangent = ecoTangent.xyz;\n"
                        : "EcoBendPosition(description.Position);\n";
                    return match.Value.Replace("return description;", "#if SHADERPASS != SHADERPASS_META\n" + bend + "#endif\nreturn description;");
                }, RegexOptions.Singleline);
            if (hooks == 0) throw new InvalidOperationException("No water vertex passes found.");
            shader = AtmosphereSurfacePass.AddToShader(shader);
            Directory.CreateDirectory(Path.GetDirectoryName(ShaderPath));
            File.WriteAllText(ShaderPath, "// Generated from the installed Bitgem water graph by CurvedWaterSetup.\n" + shader);
            AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static string PreserveMaterialNormals(string shader)
        {
            // The graph scales its world UVs by normal.y and also uses it for top/side masks.
            // Feeding bent normals into those nodes makes the texture phase change whenever
            // the bend origin moves. Carry the original normal separately; URP lighting still
            // uses the bent normal/tangent, and depth/refraction still use the bent position.
            int passes = 0;
            shader = Regex.Replace(shader, @"HLSLPROGRAM.*?ENDHLSL", match =>
            {
                string pass = match.Value;
                if (!pass.Contains("float3 WorldSpaceNormal;")) return pass;
                passes++;
                int semantic = Regex.Matches(pass, @"\bINTERP(\d+)\b").Cast<Match>()
                    .Select(m => int.Parse(m.Groups[1].Value)).DefaultIfEmpty(-1).Max() + 1;
                pass = ReplaceOnce(pass, @"struct Varyings\s*\{", "$0\nfloat3 ecoMaterialNormalWS;");
                pass = ReplaceOnce(pass, @"struct PackedVaryings\s*\{",
                    "$0\nfloat3 ecoMaterialNormalWS : INTERP" + semantic + ";");
                pass = ReplaceOnce(pass, @"struct VertexDescription\s*\{", "$0\nfloat3 ecoMaterialNormalWS;");
                pass = ReplaceOnce(pass, @"VertexDescription description = \(VertexDescription\)0;",
                    "$0\ndescription.ecoMaterialNormalWS = TransformObjectToWorldNormal(IN.ObjectSpaceNormal);");
                pass = ReplaceOnce(pass, @"ZERO_INITIALIZE\(PackedVaryings, output\);",
                    "$0\noutput.ecoMaterialNormalWS = input.ecoMaterialNormalWS;");
                pass = ReplaceOnce(pass, @"Varyings UnpackVaryings\s*\(PackedVaryings input\)\s*\{\s*Varyings output;",
                    "$0\noutput.ecoMaterialNormalWS = input.ecoMaterialNormalWS;");
                pass = ReplaceOnce(pass, @"Varyings CustomInterpolatorPassThroughFunc\(inout Varyings output, VertexDescription input\)\s*\{",
                    "$0\noutput.ecoMaterialNormalWS = input.ecoMaterialNormalWS;");
                return ReplaceOnce(pass, @"output.WorldSpaceNormal = [^;]+;",
                    "output.WorldSpaceNormal = normalize(input.ecoMaterialNormalWS);");
            }, RegexOptions.Singleline);
            if (passes == 0) throw new InvalidOperationException("No water material normal inputs found.");
            return shader;
        }

        private static string ReplaceOnce(string source, string pattern, string replacement)
        {
            if (Regex.Matches(source, pattern).Count != 1)
                throw new InvalidOperationException("Water graph layout changed; review normal adapter: " + pattern);
            return Regex.Replace(source, pattern, replacement);
        }
    }
}
