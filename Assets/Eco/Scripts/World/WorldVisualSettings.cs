using System;
using UnityEngine;

namespace Eco.Scripts.World
{
    [CreateAssetMenu(fileName = "WorldVisualSettings", menuName = "Eco/World Visual Settings")]
    public sealed class WorldVisualSettings : ScriptableObject
    {
        [SerializeField] private bool distortionEnabled = true;
        [Tooltip("Apparent planet radius in world units. Larger values make the world look flatter. This does not change map size.")]
        [SerializeField, Min(25)] private float worldRadius = 41.666668f;
        [Tooltip("Multiplier for the world bend. Zero is flat; one uses the radius above; two doubles the bend.")]
        [SerializeField, Range(0, 2)] private float distortionStrength = 1;
        [Tooltip("Radius around the player that stays flat for close interactions.")]
        [SerializeField, Min(0)] private float flatRadius = 6;

        [Header("Atmosphere")]
        [SerializeField] private bool atmosphereEnabled = true;
        [SerializeField] private Color atmosphereHorizonColor = new Color(0.48f, 0.76f, 1f);
        [SerializeField] private Color atmosphereSpaceColor = new Color(0.06f, 0.22f, 0.55f);
        [SerializeField, Range(0, 1)] private float atmosphereStrength = 0.85f;
        [Tooltip("Apparent width of the sky glow in degrees. Independent of terrain height and triangle depth; scene depth keeps it off objects and water.")]
        [SerializeField, Range(0.1f, 20)] private float atmosphereThickness = 4;
        [Tooltip("Falloff shape within the rim. The effect is always zero outside its thickness.")]
        [SerializeField, Range(0.5f, 3)] private float atmosphereSoftness = 1.3f;

        [Header("Terrain")]
        [Tooltip("Blends randomly offset texture samples to hide repetition. Zero restores regular tiling; one gives the full effect.")]
        [SerializeField, Range(0, 1)] private float terrainAntiTiling = 1;
        [Tooltip("Subtle brightness and warm/cool variation across broad patches of ground. Zero disables it.")]
        [SerializeField, Range(0, 0.5f)] private float terrainColourVariation = 0.16f;
        [Tooltip("Size of the broad ground colour patches in metres. Does not change the detail texture's scale.")]
        [SerializeField, Min(1)] private float terrainColourPatchSize = 22;

        public event Action Changed;
        public float WorldRadius => worldRadius;
        public float DistortionStrength => distortionStrength;
        public float Curvature => distortionEnabled ? distortionStrength / (2 * Mathf.Max(25, worldRadius)) : 0;
        public float FlatRadius => flatRadius;
        public bool AtmosphereEnabled => atmosphereEnabled;
        public Color AtmosphereHorizonColor => atmosphereHorizonColor;
        public Color AtmosphereSpaceColor => atmosphereSpaceColor;
        public float AtmosphereStrength => atmosphereStrength;
        public float AtmosphereThickness => atmosphereThickness;
        public float AtmosphereSoftness => atmosphereSoftness;
        public Vector4 TerrainVariation => new Vector4(terrainAntiTiling, terrainColourVariation,
            1 / Mathf.Max(1, terrainColourPatchSize), 0);

        public void InitializeFromCurvature(float curvature, float interactionRadius)
        {
            worldRadius = curvature > 0 ? Mathf.Max(25, 1 / (2 * curvature)) : 41.666668f;
            distortionStrength = Mathf.Clamp(curvature * 2 * worldRadius, 0, 2);
            flatRadius = Mathf.Max(0, interactionRadius);
            Changed?.Invoke();
        }

        private void OnValidate()
        {
            worldRadius = Mathf.Max(25, worldRadius);
            distortionStrength = Mathf.Clamp(distortionStrength, 0, 2);
            flatRadius = Mathf.Max(0, flatRadius);
            atmosphereStrength = Mathf.Clamp01(atmosphereStrength);
            atmosphereThickness = Mathf.Clamp(atmosphereThickness, 0.1f, 20);
            atmosphereSoftness = Mathf.Clamp(atmosphereSoftness, 0.5f, 3);
            terrainAntiTiling = Mathf.Clamp01(terrainAntiTiling);
            terrainColourVariation = Mathf.Clamp(terrainColourVariation, 0, 0.5f);
            terrainColourPatchSize = Mathf.Max(1, terrainColourPatchSize);
            Changed?.Invoke();
        }
    }
}
