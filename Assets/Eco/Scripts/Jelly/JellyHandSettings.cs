using System.Collections.Generic;
using UnityEngine;

namespace Eco.Scripts.Jelly
{
    /// <summary>
    /// Tuning for the jelly's tentacle hands: where on the body they can grow, how they look, and how a collector
    /// hand times a pickup. Used by <see cref="JellyBody"/>, every <see cref="JellyTentacle"/> on it and the
    /// collector hands. Distances are in metres at the body's base size and grow with it. How close to the mouth
    /// hands may grow is in <see cref="JellyMouthSettings"/>; the spade's grip timing is on SpadeController.
    /// </summary>
    [CreateAssetMenu(menuName = "Eco/Jelly Hand Settings", fileName = "HandSettings")]
    public class JellyHandSettings : ScriptableObject
    {
        [Header("Where hands grow")]
        [Tooltip("Body areas tentacles can grow from.")]
        [SerializeField] private List<JellyHandZone> zones = new()
        {
            new JellyHandZone { minHeight = 0.18f, maxHeight = 0.82f, minAngle = -180f, maxAngle = 180f }
        };
        [Tooltip("Spacing of the candidate spots inside the zones, at unit body scale (the body's radius is about 1).")]
        [SerializeField, Min(0.02f)] private float anchorStep = 0.12f;
        [Tooltip("Tentacles out at the same time keep at least this far apart, at unit body scale.")]
        [SerializeField, Min(0f)] private float anchorSpacing = 0.45f;

        [Header("Look")]
        [Tooltip("Sides around the tube.")]
        [SerializeField, Range(4, 24)] private int sides = 12;
        [Tooltip("Rings along the tube.")]
        [SerializeField, Range(4, 48)] private int segments = 28;
        [SerializeField, Min(0f)] private float rootRadius = 0.26f;
        [SerializeField, Min(0f)] private float midRadius = 0.12f;
        [SerializeField, Min(0f)] private float tipRadius = 0.17f;
        [Tooltip("How deep inside the body the tentacle starts, so it grows out of the surface.")]
        [SerializeField, Min(0f)] private float embedDepth = 0.2f;
        [SerializeField, Min(0f)] private float swayAmplitude = 0.06f;
        [SerializeField, Min(0f)] private float swaySpeed = 5f;

        [Header("Pickup")]
        [Tooltip("Seconds to grow out and reach the item.")]
        [SerializeField, Min(0.01f)] private float reachDuration = 0.3f;
        [Tooltip("Seconds to carry it round to the front of the mouth.")]
        [SerializeField, Min(0.01f)] private float carryDuration = 0.45f;
        [Tooltip("Seconds for the hand to stretch in and push it through the mouth.")]
        [SerializeField, Min(0.01f)] private float pushDuration = 0.25f;
        [Tooltip("Seconds to pull back into the body.")]
        [SerializeField, Min(0.01f)] private float retractDuration = 0.25f;
        [Tooltip("Each pickup's timings vary by up to this share, so hands don't move in lockstep.")]
        [SerializeField, Range(0f, 0.5f)] private float timingVariation = 0.1f;
        [Tooltip("How high an item is lifted on its way to the mouth, in metres.")]
        [SerializeField, Min(0f)] private float carryArcHeight = 1.2f;
        [Tooltip("The arm lines the item up this far in front of the mouth before the hand pushes it in, plus " +
                 "half the item's length.")]
        [SerializeField, Min(0f)] private float mouthApproachDistance = 0.7f;

        public IReadOnlyList<JellyHandZone> Zones => zones;
        public float AnchorStep => anchorStep;
        public float AnchorSpacing => anchorSpacing;
        public int Sides => sides;
        public int Segments => segments;
        public float RootRadius => rootRadius;
        public float MidRadius => midRadius;
        public float TipRadius => tipRadius;
        public float EmbedDepth => embedDepth;
        public float SwayAmplitude => swayAmplitude;
        public float SwaySpeed => swaySpeed;
        public float ReachDuration => reachDuration;
        public float CarryDuration => carryDuration;
        public float PushDuration => pushDuration;
        public float RetractDuration => retractDuration;
        public float TimingVariation => timingVariation;
        public float CarryArcHeight => carryArcHeight;
        public float MouthApproachDistance => mouthApproachDistance;
    }
}
