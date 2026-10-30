using System.IO;
using Eco.Scripts.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Eco.Scripts.EditorTools
{
    [CustomEditor(typeof(WorldVisualSettings))]
    public sealed class WorldVisualSettingsEditor : UnityEditor.Editor
    {
        public const string SettingsPath = "Assets/Eco/ContentDescriptions/Rendering/WorldVisualSettings.asset";

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Shared world curvature, atmosphere and terrain settings. Larger World Radius means a flatter-looking world. Terrain Anti Tiling breaks up repeated textures; Colour Variation and Patch Size add broad ground variation without changing texture detail scale.", MessageType.Info);
            if (DrawDefaultInspector())
            {
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
            var settings = (WorldVisualSettings)target;
            EditorGUILayout.LabelField("Effective Curvature", settings.Curvature.ToString("0.#####"));
        }

        [MenuItem("Eco/Rendering/World Visual Settings")]
        public static void OpenSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<WorldVisualSettings>(SettingsPath);
            if (settings == null)
            {
                Debug.LogWarning("Apply Curved World in the gameplay scene first to create the global settings.");
                return;
            }
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        public static WorldVisualSettings AssignTo(CurvedWorldController controller)
        {
            var settings = AssetDatabase.LoadAssetAtPath<WorldVisualSettings>(SettingsPath);
            if (settings == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                AssetDatabase.Refresh();
                settings = CreateInstance<WorldVisualSettings>();
                settings.InitializeFromCurvature(controller.Curvature, controller.FlatRadius);
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            Undo.RecordObject(controller, "Assign global world visual settings");
            controller.Settings = settings;
            EditorUtility.SetDirty(controller);
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            return settings;
        }
    }
}
