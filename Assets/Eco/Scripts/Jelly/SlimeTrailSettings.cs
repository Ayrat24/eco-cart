using UnityEngine;
using UnityEngine.Serialization;

namespace Eco.Scripts.Jelly
{
    /// <summary>
    /// Tuning for the slime the jelly leaves behind (<see cref="SlimeTrail"/>), how it looks, and the grass that
    /// grows on it. The look goes onto the trail's material (<c>Eco/SlimeTrail</c>) when the trail starts, and
    /// every frame in the editor so it can be tuned in play mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Eco/Slime Trail Settings", fileName = "SlimeTrailSettings")]
    public class SlimeTrailSettings : ScriptableObject
    {
        [Header("Trail")]
        [Tooltip("Seconds a piece of trail stays on the ground. It dries up and fades out by the end.")]
        [SerializeField, Min(0.1f)] private float lifetime = 5f;
        [Tooltip("Trail width at unit body scale; it grows with the body and the trail width upgrade.")]
        [SerializeField, Min(0.05f)] private float width = 1.3f;
        [Tooltip("How much the width wanders along the trail, as a share of it.")]
        [SerializeField, Range(0f, 0.5f)] private float widthWobble = 0.15f;
        [Tooltip("Metres between trail points. Smaller follows turns more closely but updates the mesh more often.")]
        [SerializeField, Min(0.05f)] private float pointSpacing = 0.35f;
        [Tooltip("A jump longer than this (a teleport) starts a new trail instead of joining the old one.")]
        [SerializeField, Min(0.5f)] private float maxGap = 3f;
        [Tooltip("Height above the ground, so the terrain doesn't poke through.")]
        [SerializeField, Min(0f)] private float groundOffset = 0.04f;
        [Tooltip("Fastest speed (m/s) the trail keeps its full length at; faster, the oldest slime goes early. " +
                 "Sets how many points the trail holds, so read when the trail starts.")]
        [SerializeField, Min(1f)] private float maxTrailSpeed = 30f;
        [Tooltip("Metres of dry ground kept between the water at high tide and the slime and the grass it grows.")]
        [SerializeField, Min(0f)] private float waterClearance = 1f;

        [Header("Look")]
        [Tooltip("Colour of the slime; alpha is how thick it looks.")]
        [SerializeField] private Color color = new(0.1f, 0.62f, 0.56f, 0.35f);
        [Tooltip("Colour of the thicker slime along the edges.")]
        [SerializeField] private Color rimColor = new(0.06f, 0.48f, 0.44f, 0.65f);
        [Tooltip("Width of the thicker edge, as a share of the half width.")]
        [SerializeField, Range(0f, 1f)] private float rimWidth = 0.3f;
        [Tooltip("How soft the edges are, as a share of the half width.")]
        [SerializeField, Range(0.01f, 1f)] private float edgeSoftness = 0.25f;
        [Tooltip("How far the edges wander in and out, as a share of the half width.")]
        [SerializeField, Range(0f, 0.5f)] private float edgeWobble = 0.2f;
        [Tooltip("Wanders per metre of trail.")]
        [SerializeField, Min(0f)] private float edgeWobbleScale = 1.3f;
        [Tooltip("How far the edges pull in by the time the slime has dried, as a share of the half width.")]
        [SerializeField, Range(0f, 1f)] private float dryShrink = 0.45f;
        [Tooltip("Seconds of fading out at the end of the lifetime.")]
        [SerializeField, Min(0.01f)] private float fadeTime = 1.5f;
        [Tooltip("Sharpness of the wet highlights.")]
        [SerializeField, Range(4f, 256f)] private float gloss = 60f;
        [Tooltip("Brightness of the wet highlights.")]
        [SerializeField, Range(0f, 4f)] private float specular = 1.2f;
        [Tooltip("Sheen at grazing angles.")]
        [SerializeField, Range(0f, 2f)] private float fresnel = 0.5f;
        [Tooltip("Share of the highlights gone by the time the slime has dried.")]
        [SerializeField, Range(0f, 1f)] private float dryGloss = 0.7f;
        [Tooltip("Lumps per metre in the slime's surface, which break up the highlights.")]
        [SerializeField, Min(0f)] private float lumpFrequency = 7f;
        [Tooltip("How much the lumps tilt the surface.")]
        [SerializeField, Range(0f, 1f)] private float lumpStrength = 0.12f;

        [Header("Grass")]
        [Tooltip("Seconds after the trail appears that grass starts growing on it. The terrain is written in one " +
                 "batch just before, so a longer delay means fewer, larger writes.")]
        [SerializeField, Min(0f)] private float growDelay = 3f;
        [Tooltip("Seconds the grass takes to grow to full size.")]
        [SerializeField, Min(0.1f)] private float growDuration = 10f;
        [Tooltip("Grass blades grow on the terrain detail cells (about 1 m) whose centre is within this share of " +
                 "the trail's half width. 0: no blades.")]
        [FormerlySerializedAs("grassWidth")]
        [SerializeField, Range(0f, 3f)] private float grassDetailWidth = 1.4f;
        [Tooltip("The terrain's grass texture layer grows into the ground of the cells (about 1 m) whose centre is " +
                 "within this share of the trail's half width. Keep it wider than the blades: their clumps are 2-3 m " +
                 "across and look bad on bare ground. 0: the ground stays as it is.")]
        [SerializeField, Range(0f, 4f)] private float grassTextureWidth = 2.6f;

        public float Lifetime => lifetime;
        public float Width => width;
        public float WidthWobble => widthWobble;
        public float PointSpacing => pointSpacing;
        public float MaxGap => maxGap;
        public float GroundOffset => groundOffset;
        public float MaxTrailSpeed => maxTrailSpeed;
        public float WaterClearance => waterClearance;
        public float GrowDelay => growDelay;
        public float GrowDuration => growDuration;
        public float GrassDetailWidth => grassDetailWidth;
        public float GrassTextureWidth => grassTextureWidth;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        private static readonly int RimWidthId = Shader.PropertyToID("_RimWidth");
        private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int EdgeWobbleId = Shader.PropertyToID("_EdgeWobble");
        private static readonly int EdgeWobbleScaleId = Shader.PropertyToID("_EdgeWobbleScale");
        private static readonly int DryShrinkId = Shader.PropertyToID("_DryShrink");
        private static readonly int FadeTimeId = Shader.PropertyToID("_FadeTime");
        private static readonly int GlossId = Shader.PropertyToID("_Gloss");
        private static readonly int SpecularId = Shader.PropertyToID("_Specular");
        private static readonly int FresnelId = Shader.PropertyToID("_Fresnel");
        private static readonly int DryGlossId = Shader.PropertyToID("_DryGloss");
        private static readonly int LumpFrequencyId = Shader.PropertyToID("_BumpScale");
        private static readonly int LumpStrengthId = Shader.PropertyToID("_BumpStrength");

        /// <summary>Puts the look onto <paramref name="material"/> (an <c>Eco/SlimeTrail</c> material).</summary>
        public void ApplyLook(Material material)
        {
            material.SetColor(ColorId, color);
            material.SetColor(RimColorId, rimColor);
            material.SetFloat(RimWidthId, rimWidth);
            material.SetFloat(EdgeSoftnessId, edgeSoftness);
            material.SetFloat(EdgeWobbleId, edgeWobble);
            material.SetFloat(EdgeWobbleScaleId, edgeWobbleScale);
            material.SetFloat(DryShrinkId, dryShrink);
            material.SetFloat(FadeTimeId, fadeTime);
            material.SetFloat(GlossId, gloss);
            material.SetFloat(SpecularId, specular);
            material.SetFloat(FresnelId, fresnel);
            material.SetFloat(DryGlossId, dryGloss);
            material.SetFloat(LumpFrequencyId, lumpFrequency);
            material.SetFloat(LumpStrengthId, lumpStrength);
        }
    }
}
