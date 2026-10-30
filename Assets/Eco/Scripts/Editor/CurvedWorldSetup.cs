using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Eco.Scripts.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Eco.Scripts.EditorTools
{
    /// <summary>Generate adapters against the installed URP version, keeping original art assets intact.</summary>
    public static class CurvedWorldSetup
    {
        private const string Folder = "Assets/Eco/Data/Shaders/CurvedWorld/";
        private const string Materials = "Assets/Eco/Data/Materials/CurvedWorld/";
        private const string Urp = "Packages/com.unity.render-pipelines.universal/";
        private const string Core = "#include \"Assets/Eco/Data/Shaders/CurvedWorld/CurvedWorld.hlsl\"\n";
        private const string GrassGrowth = "#include \"Assets/Eco/Data/Shaders/GrassGrowth.hlsl\"\n";
        private const string Polytope = "Assets/Polytope Studio/Lowpoly_Environments/Sources/Shaders/";

        [MenuItem("Eco/Rendering/Apply Curved World")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Apply Curved World in Edit mode.");

            GenerateShaders();
            CurvedWaterSetup.Apply();
            var shaderMap = new Dictionary<string, string>
            {
                ["Universal Render Pipeline/Lit"] = "Eco/Curved World/Lit",
                ["Polytope Studio/PT_Vegetation_Opaque_Shader"] = "Eco/Curved World/Vegetation Opaque",
                ["Polytope Studio/PT_Vegetation_Foliage_Shader"] = "Eco/Curved World/Vegetation Foliage"
            };
            Directory.CreateDirectory(Materials);
            AssetDatabase.Refresh();
            var materialMap = new Dictionary<Material, Material>();
            int changedPrefabs = 0;
            var paths = AssetDatabase.FindAssets("t:Prefab", new[]
            {
                "Assets/Eco/Data/Prefabs/ScrapPrefabs", "Assets/Eco/Data/Prefabs/Trees"
            }).Select(AssetDatabase.GUIDToAssetPath).ToArray();

            foreach (string path in paths)
            {
                var prefab = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    bool changed = false;
                    foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                        var materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++)
                        {
                            var source = materials[i];
                            if (source == null || !shaderMap.TryGetValue(source.shader.name, out var target)) continue;
                            if (!materialMap.TryGetValue(source, out var curved))
                            {
                                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long localId);
                                string materialPath = Materials + guid + "_" + localId + ".mat";
                                curved = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                                if (curved == null)
                                {
                                    curved = new Material(source) { name = source.name + " (Curved)" };
                                    curved.shader = Shader.Find(target);
                                    AssetDatabase.CreateAsset(curved, materialPath);
                                }
                                materialMap.Add(source, curved);
                            }
                            materials[i] = curved;
                            changed = true;
                        }
                        renderer.sharedMaterials = materials;
                    }
                    if (prefab.GetComponentsInChildren<MeshRenderer>(true).Length > 0 &&
                        prefab.GetComponent<CurvedWorldBounds>() == null)
                    {
                        prefab.AddComponent<CurvedWorldBounds>();
                        changed = true;
                    }
                    if (changed)
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefab, path);
                        changedPrefabs++;
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }

            // The instanced terrain grass uses the prototype material directly. Only this material
            // is updated; never save the TerrainData, which may contain unrelated pending edits.
            foreach (var terrain in UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
            {
                Undo.RecordObject(terrain, "Apply Curved World terrain");
                const string terrainMaterialPath = Materials + "TerrainCurved.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(terrainMaterialPath);
                if (material == null)
                {
                    material = new Material(terrain.materialTemplate) { name = "Terrain Curved" };
                    material.shader = Shader.Find("Eco/Curved World/Terrain Lit");
                    material.DisableKeyword("_TERRAIN_INSTANCED_PERPIXEL_NORMAL");
                    AssetDatabase.CreateAsset(material, terrainMaterialPath);
                }
                terrain.materialTemplate = material;
                // Keep enough vertices for curvature even on the original perfectly flat plateau.
                terrain.heightmapPixelError = 1;
                // Flat patches have no height for patchBoundsMultiplier to scale; bend the patch bounds instead
                terrain.patchBoundsMultiplier = Vector3.one;
                if (terrain.GetComponent<CurvedTerrainBounds>() == null)
                    Undo.AddComponent<CurvedTerrainBounds>(terrain.gameObject);
                foreach (var detail in terrain.terrainData.detailPrototypes)
                {
                    if (detail.prototype == null) continue;
                    foreach (var grassMaterial in detail.prototype.GetComponentsInChildren<Renderer>(true)
                                 .SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct())
                    {
                        if (grassMaterial.shader.name != "Polytope Studio/PT_Vegetation_Plants_Shader") continue;
                        Undo.RecordObject(grassMaterial, "Curve terrain grass");
                        grassMaterial.shader = Shader.Find("Eco/Curved World/Vegetation Plants");
                        EditorUtility.SetDirty(grassMaterial);
                        AssetDatabase.SaveAssetIfDirty(grassMaterial);
                    }
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(terrain);
                EditorUtility.SetDirty(terrain);
            }

            var world = UnityEngine.Object.FindAnyObjectByType<WorldController>();
            if (world == null) throw new InvalidOperationException("Open the gameplay scene first.");
            var controller = world.GetComponent<CurvedWorldController>();
            if (controller == null) controller = Undo.AddComponent<CurvedWorldController>(world.gameObject);
            Undo.RecordObject(controller, "Configure world curvature");
            controller.FollowTarget = new SerializedObject(world).FindProperty("player").objectReferenceValue as Transform;
            WorldVisualSettingsEditor.AssignTo(controller);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
            EditorSceneManager.SaveScene(world.gameObject.scene);
            Debug.Log($"[CurvedWorld] Applied to terrain and {changedPrefabs} prefabs; created/reused {materialMap.Count} materials.");
        }

        public static void GenerateShaders()
        {
            Directory.CreateDirectory(Folder);
            string lit = File.ReadAllText(Urp + "Shaders/Lit.shader")
                .Replace("Shader \"Universal Render Pipeline/Lit\"", "Shader \"Eco/Curved World/Lit\"");
            lit = Wrap(lit, "LitForwardPass.hlsl", "LitPassVertex", "EcoBendVertex(input.positionOS.xyz, input.normalOS, input.tangentOS);");
            lit = Wrap(lit, "LitGBufferPass.hlsl", "LitGBufferPassVertex", "EcoBendVertex(input.positionOS.xyz, input.normalOS, input.tangentOS);");
            lit = Wrap(lit, "ShadowCasterPass.hlsl", "ShadowPassVertex", "EcoBendVertex(input.positionOS.xyz, input.normalOS);");
            lit = Wrap(lit, "DepthOnlyPass.hlsl", "DepthOnlyVertex", "EcoBendPosition(input.position.xyz);");
            lit = Wrap(lit, "LitDepthNormalsPass.hlsl", "DepthNormalsVertex", "EcoBendVertex(input.positionOS.xyz, input.normal, input.tangentOS);");
            lit = lit.Replace(Urp + "ShaderLibrary/ObjectMotionVectors.hlsl", Folder + "CurvedMotionVectors.hlsl");
            Write("CurvedLit.shader", lit);

            string motion = File.ReadAllText(Urp + "ShaderLibrary/ObjectMotionVectors.hlsl");
            motion = motion.Replace("// Structs", Core + "\n// Structs");
            motion = motion.Replace("const VertexPositionInputs vertexInput = GetVertexPositionInputs(input.position.xyz);",
                "float3 ecoPositionWS = EcoBendWorldPosition(TransformObjectToWorld(input.position.xyz), _EcoWorldBend);");
            motion = motion.Replace("vertexInput.positionCS", "TransformWorldToHClip(ecoPositionWS)")
                .Replace("mul(UNITY_MATRIX_M, input.position)", "float4(ecoPositionWS, 1)")
                .Replace("mul(UNITY_PREV_MATRIX_M, prevPos)",
                    "float4(EcoBendWorldPosition(mul(UNITY_PREV_MATRIX_M, prevPos).xyz, _EcoWorldBendPrevious), 1)");
            Write("CurvedMotionVectors.hlsl", motion);

            // The grass layer grows into the ground where the slime trail's grass grows (GrassGrowth.hlsl)
            string passes = File.ReadAllText(Urp + "Shaders/Terrain/TerrainLitPasses.hlsl");
            const string controlSample = "half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);";
            const string lastInclude = "#include \"" + Urp + "ShaderLibrary/DBuffer.hlsl\"";
            if (!passes.Contains(controlSample) || !passes.Contains(lastInclude))
                throw new InvalidOperationException("URP shader changed: TerrainLitPasses.hlsl");
            passes = passes.Replace(lastInclude, lastInclude + "\n" + GrassGrowth)
                .Replace(controlSample, controlSample + "\n#ifndef TERRAIN_SPLAT_ADDPASS\n" +
                    "    EcoGrassGroundGrowth(splatControl, splatUV, _Control_TexelSize.zw);\n#endif");
            passes = passes.Replace(lastInclude, lastInclude + "\n#include \"" + Folder + "TerrainVariation.hlsl\"");
            passes = AddTerrainSampling(passes);
            const string initializeInput = "    InitializeInputData(IN, normalTS, inputData);";
            if (!passes.Contains(initializeInput))
                throw new InvalidOperationException("URP terrain fragment changed: macro colour hook missing.");
            passes = passes.Replace(initializeInput,
                "    albedo = EcoTerrainMacroColour(albedo, IN.positionWS.xz);\n" + initializeInput);
            Write("CurvedTerrainLitPasses.hlsl", passes);

            // Generate the distant colour/metallic cache using the same random samples as
            // the near passes. Macro colour remains world-space and is applied at draw time.
            string basemap = File.ReadAllText(Urp + "Shaders/Terrain/TerrainLitBasemapGen.shader")
                .Replace("Hidden/Universal Render Pipeline/Terrain/Lit (Basemap Gen)", "Hidden/Eco/Curved World/Terrain Basemap Gen")
                .Replace("\"TerrainLitInput.hlsl\"", "\"" + Urp + "Shaders/Terrain/TerrainLitInput.hlsl\"")
                .Replace("\"TerrainLitPasses.hlsl\"", "\"" + Folder + "CurvedTerrainLitPasses.hlsl\"");
            Write("TerrainLitBasemapGen.shader", AddTerrainSampling(basemap));

            foreach (var file in new[] { "TerrainLit.shader", "TerrainLitAdd.shader", "TerrainLitBase.shader" })
            {
                string terrain = File.ReadAllText(Urp + "Shaders/Terrain/" + file);
                terrain = terrain.Replace("Hidden/Universal Render Pipeline/Terrain/Lit (Add Pass)", "Hidden/Eco/Curved World/Terrain Add")
                    .Replace("Hidden/Universal Render Pipeline/Terrain/Lit (Base Pass)", "Hidden/Eco/Curved World/Terrain Base")
                    .Replace("Hidden/Universal Render Pipeline/Terrain/Lit (Basemap Gen)", "Hidden/Eco/Curved World/Terrain Basemap Gen")
                    .Replace("Shader \"Universal Render Pipeline/Terrain/Lit\"", "Shader \"Eco/Curved World/Terrain Lit\"");
                // Leave meta/lightmap baking flat. All real-time passes use the same deformation.
                var blocks = terrain.Split("HLSLPROGRAM");
                for (int i = 1; i < blocks.Length; i++)
                    if (!blocks[i].Contains("#pragma vertex TerrainVertexMeta"))
                        blocks[i] = blocks[i].Replace(Urp + "Shaders/Terrain/TerrainLitInput.hlsl", Folder + "CurvedTerrainInput.hlsl")
                            .Replace(Urp + "Shaders/Terrain/TerrainLitPasses.hlsl", Folder + "CurvedTerrainLitPasses.hlsl");
                terrain = string.Join("HLSLPROGRAM", blocks);
                if (file != "TerrainLitAdd.shader") terrain = AtmosphereSurfacePass.AddToShader(terrain);
                Write(file, terrain);
            }

            foreach (var type in new[] { "Opaque", "Foliage", "Plants" })
            {
                string shader = File.ReadAllText(Polytope + "PT_Vegetation_" + type + "_Shader.shader");
                shader = shader.Replace("Shader \"Polytope Studio/PT_Vegetation_" + type + "_Shader\"",
                    "Shader \"Eco/Curved World/Vegetation " + type + "\"");
                // Only the terrain grass uses Plants: its blades grow in where the slime trail plants them
                bool grows = type == "Plants";
                string grow = grows ? "EcoGrassGrowth(input.positionOS.xyz);\n" : "";
                shader = shader.Replace("#include \"" + Urp + "ShaderLibrary/Core.hlsl\"",
                    "#include \"" + Urp + "ShaderLibrary/Core.hlsl\"\n" + Core + (grows ? GrassGrowth : ""));
                shader = Regex.Replace(shader, @"input\.normalOS = input\.normalOS;\s*input\.tangentOS = input\.tangentOS;",
                    "#if SHADERPASS != SHADERPASS_META\n" + grow + "EcoBendVertex(input.positionOS.xyz, input.normalOS, input.tangentOS);\n#endif");
                shader = shader.Replace("input.normalOS = input.normalOS;",
                    "#if SHADERPASS != SHADERPASS_META\n" + grow + "EcoBendVertex(input.positionOS.xyz, input.normalOS);\n#endif");
                // Amplify's motion pass has no normal assignment to hook; bend both frame positions.
                int motionStart = shader.IndexOf("Name \"MotionVectors\"", StringComparison.Ordinal);
                if (motionStart >= 0)
                {
                    string motionPass = shader.Substring(motionStart);
                    motionPass = motionPass.Replace("VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );",
                        grow + "float3 ecoPositionWS = EcoBendWorldPosition(TransformObjectToWorld(input.positionOS.xyz), _EcoWorldBend);");
                    motionPass = motionPass.Replace("vertexInput.positionCS", "TransformWorldToHClip(ecoPositionWS)");
                    motionPass = Regex.Replace(motionPass, @"mul\(\s*UNITY_MATRIX_M,\s*input.positionOS\s*\)", "float4(ecoPositionWS, 1)");
                    motionPass = Regex.Replace(motionPass, @"mul\(\s*UNITY_PREV_MATRIX_M,\s*prevPos\s*\)",
                        "float4(EcoBendWorldPosition(mul(UNITY_PREV_MATRIX_M, prevPos).xyz, _EcoWorldBendPrevious), 1)");
                    shader = shader.Substring(0, motionStart) + motionPass;
                }
                // Keep generated derivatives separate from the source Amplify graphs.
                int graph = shader.IndexOf("/*ASEBEGIN", StringComparison.Ordinal);
                if (graph >= 0) shader = shader.Substring(0, graph);
                Write("CurvedVegetation" + type + ".shader", shader);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static string Wrap(string shader, string include, string entry, string bend)
        {
            string line = "#include \"" + Urp + "Shaders/" + include + "\"";
            if (!shader.Contains(line)) throw new InvalidOperationException("URP shader changed: " + include);
            return shader.Replace(line, Core + "#define " + entry + " EcoOriginal" + entry + "\n" + line +
                "\n#undef " + entry + "\nVaryings " + entry + "(Attributes input)\n{\n" +
                "UNITY_SETUP_INSTANCE_ID(input);\n" + bend + "\nreturn EcoOriginal" + entry + "(input);\n}\n");
        }

        private static string AddTerrainSampling(string source)
        {
            // Replace only layer samples: control, holes, growth and cached base maps keep
            // their original UVs. All channels share offsets so normal/mask detail matches.
            int replacements = 0;
            string result = Regex.Replace(source,
                @"SAMPLE_TEXTURE2D\((_Splat[0-3]|_Normal[0-3]|_Mask[0-3]),\s*(sampler_\w+),\s*([^\)]+)\)",
                match =>
                {
                    replacements++;
                    return "EcoSampleTerrain(TEXTURE2D_ARGS(" + match.Groups[1].Value + ", " +
                           match.Groups[2].Value + "), " + match.Groups[3].Value + ")";
                });
            if (replacements == 0)
                throw new InvalidOperationException("URP terrain layer sampling changed: anti-tiling hook missing.");
            return result;
        }

        private static void Write(string file, string content)
        {
            File.WriteAllText(Folder + file, "// Generated by CurvedWorldSetup from installed shaders. Regenerate after a URP/art update.\n" + content);
            AssetDatabase.ImportAsset(Folder + file, ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
