using System;
using System.Linq;
using Eco.Scripts.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Eco.Scripts.EditorTools
{
    public static class CurvedAtmosphereSetup
    {
        private const string MaterialPath = "Assets/Eco/Data/Materials/CurvedWorld/Atmosphere.mat";

        [MenuItem("Eco/Rendering/Apply Curved Atmosphere")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Apply atmosphere in Edit mode.");

            AtmosphereSurfacePass.UpdateGeneratedShaders();

            var shader = Shader.Find("Eco/Curved World/Atmosphere");
            if (shader == null) throw new InvalidOperationException("Atmosphere shader is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Curved Atmosphere" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            // Only the gameplay renderers; leave vendor/demo renderer assets untouched.
            foreach (string path in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" })
            {
                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (renderer == null) throw new InvalidOperationException("Missing gameplay renderer: " + path);
                var feature = renderer.rendererFeatures.OfType<CurvedAtmosphereFeature>().FirstOrDefault();
                if (feature == null)
                {
                    feature = ScriptableObject.CreateInstance<CurvedAtmosphereFeature>();
                    feature.name = "Curved World Atmosphere";
                    AssetDatabase.AddObjectToAsset(feature, renderer);
                    renderer.rendererFeatures.Add(feature);
                }
                feature.SetMaterial(material);
                feature.SetActive(true);
                feature.Create();
                // Keep URP's recovery map aligned with the serialized feature list after reloads.
                var serializedRenderer = new SerializedObject(renderer);
                var map = serializedRenderer.FindProperty("m_RendererFeatureMap");
                map.arraySize = renderer.rendererFeatures.Count;
                for (int i = 0; i < renderer.rendererFeatures.Count; i++)
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[i], out string _, out long id);
                    map.GetArrayElementAtIndex(i).longValue = id;
                }
                serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(feature);
                renderer.SetDirty();
                EditorUtility.SetDirty(renderer);
                AssetDatabase.SaveAssetIfDirty(renderer);
            }

            AssetDatabase.SaveAssetIfDirty(material);
            var settings = AssetDatabase.LoadAssetAtPath<World.WorldVisualSettings>(WorldVisualSettingsEditor.SettingsPath);
            if (settings != null)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
            }
            Debug.Log("[CurvedWorld] Atmosphere installed on PC and Mobile renderers. Tune it in World Visual Settings.");
        }
    }
}
