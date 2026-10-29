using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eco.Scripts.EditorTools
{
    /// <summary>Imports the original Blender scrap collection as reusable visual prefabs.</summary>
    public static class OriginalScrapArtBuilder
    {
        private const string ModelFolder = "Assets/Eco/Data/Models/OriginalScrap";
        private const string PrefabFolder = "Assets/Eco/Data/Prefabs/ScrapArt";
        private const string MaterialFolder = ModelFolder + "/Materials";

        [MenuItem("Eco/Art/Rebuild Original Scrap Prefabs")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Build scrap art outside Play mode.");
            }

            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);
            var shader = Shader.Find("Eco/Curved World/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("The project's curved-world Lit shader is missing.");
            }

            var texturePath = ModelFolder + "/ScrapPalette.png";
            var textureImporter = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.sRGBTexture = true;
            textureImporter.mipmapEnabled = false;
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.wrapMode = TextureWrapMode.Clamp;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.maxTextureSize = 256;
            textureImporter.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            var smoothness = new Dictionary<string, float>
            {
                { "Food", 0.3f }, { "Paper", 0.1f }, { "Plastic", 0.6f }, { "Glass", 0.8f }
            };
            var report = new List<string> { "Original Scrap Art — Unity import validation" };
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                foreach (var category in smoothness.Keys)
                {
                    EnsureFolder(PrefabFolder + "/" + category);
                    var materialPath = MaterialFolder + "/Scrap_" + category + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (material == null)
                    {
                        material = new Material(shader);
                        AssetDatabase.CreateAsset(material, materialPath);
                    }

                    material.shader = shader;
                    material.SetTexture("_BaseMap", texture);
                    material.SetColor("_BaseColor", Color.white);
                    material.SetFloat("_Smoothness", smoothness[category]);
                    material.SetFloat("_Metallic", 0);
                    material.enableInstancing = true;
                    EditorUtility.SetDirty(material);
                    AssetDatabase.SaveAssetIfDirty(material);

                    var models = AssetDatabase.FindAssets("t:Model", new[] { ModelFolder + "/" + category })
                        .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray();
                    if (models.Length != 3)
                    {
                        throw new InvalidOperationException($"Expected 3 models for {category}, found {models.Length}.");
                    }

                    foreach (var path in models)
                    {
                        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                        importer.globalScale = 1;
                        importer.bakeAxisConversion = true;
                        importer.importAnimation = false;
                        importer.animationType = ModelImporterAnimationType.None;
                        importer.importCameras = false;
                        importer.importLights = false;
                        importer.isReadable = false;
                        importer.meshCompression = ModelImporterMeshCompression.Off;
                        importer.importNormals = ModelImporterNormals.Import;
                        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Scrap_" + category), material);
                        importer.SaveAndReimport();

                        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
                        try
                        {
                            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                            instance.name = Path.GetFileNameWithoutExtension(path);
                            var filters = instance.GetComponentsInChildren<MeshFilter>();
                            var renderers = instance.GetComponentsInChildren<MeshRenderer>();
                            if (filters.Length != 1 || renderers.Length != 1 || filters[0].sharedMesh.subMeshCount != 1)
                            {
                                throw new InvalidOperationException(path + " must have one mesh, renderer and submesh.");
                            }

                            renderers[0].sharedMaterial = material;
                            var mesh = filters[0].sharedMesh;
                            var size = renderers[0].bounds.size;
                            var extent = Mathf.Max(size.x, size.y, size.z);
                            if (extent < 0.5f || extent > 1.3f)
                            {
                                throw new InvalidOperationException(path + " has incorrect metre scale: " + size);
                            }

                            // Local mesh bounds guarantee correct fit regardless of FBX axis conversion.
                            var collider = filters[0].gameObject.AddComponent<BoxCollider>();
                            collider.center = mesh.bounds.center;
                            collider.size = mesh.bounds.size;
                            collider.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(
                                "Assets/Eco/Data/Prefabs/ScrapPrefabs/ScrapPM.physicMaterial");

                            var prefabPath = PrefabFolder + "/" + category + "/" + instance.name + ".prefab";
                            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath, out var success);
                            if (!success)
                            {
                                throw new InvalidOperationException("Could not save " + prefabPath);
                            }
                            report.Add($"{category}/{instance.name}: {mesh.GetIndexCount(0) / 3} triangles; " +
                                $"bounds {size}; 1 renderer; 1 material; 1 box collider");
                        }
                        finally
                        {
                            UnityEngine.Object.DestroyImmediate(instance);
                        }
                    }
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }

            Directory.CreateDirectory("Logs/ScrapArt");
            File.WriteAllLines("Logs/ScrapArt/unity-validation.txt", report);
            Debug.Log(string.Join("\n", report));
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
