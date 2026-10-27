using System.IO;
using Eco.Scripts.Jelly;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Eco.Scripts.EditorTools
{
    /// <summary>
    /// The jelly body builds its mesh at runtime from its shape settings. This bakes the same mesh into an asset
    /// so the body shows up in the Scene view (and for HighlightPlus) outside play mode.
    /// </summary>
    public static class JellyBodyMenu
    {
        private const string MeshPath = "Assets/Eco/Data/Models/Jelly/JellyBody.asset";

        [MenuItem("Eco/Jelly/Bake Body Preview Mesh")]
        private static void BakePreviewMesh()
        {
            var body = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<JellyBody>()
                : null;
            if (body == null)
            {
                body = Object.FindAnyObjectByType<JellyBody>(FindObjectsInactive.Include);
            }

            if (body == null)
            {
                Debug.LogWarning("No JellyBody in the open scenes.");
                return;
            }

            var mesh = body.BuildMesh();
            var asset = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (asset != null)
            {
                EditorUtility.CopySerialized(mesh, asset);
                Object.DestroyImmediate(mesh);
                AssetDatabase.SaveAssetIfDirty(asset);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(MeshPath)!);
                AssetDatabase.CreateAsset(mesh, MeshPath);
                asset = mesh;
            }

            var meshFilter = body.GetComponent<MeshFilter>();
            Undo.RecordObject(meshFilter, "Bake Jelly Body Preview Mesh");
            meshFilter.sharedMesh = asset;
            EditorSceneManager.MarkSceneDirty(body.gameObject.scene);
            Debug.Log($"Baked the jelly body preview mesh to {MeshPath}.");
        }
    }
}
