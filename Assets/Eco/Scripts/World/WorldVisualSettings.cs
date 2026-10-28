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

        public event Action Changed;
        public float WorldRadius => worldRadius;
        public float DistortionStrength => distortionStrength;
        public float Curvature => distortionEnabled ? distortionStrength / (2 * Mathf.Max(25, worldRadius)) : 0;
        public float FlatRadius => flatRadius;

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
            Changed?.Invoke();
        }
    }
}
