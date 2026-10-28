using UnityEngine;

namespace Eco.Scripts
{
    /// <summary>
    /// Tuning for the gameplay camera's scroll-wheel zoom (see <see cref="CameraController"/>). Distances are relative
    /// to the cart's camera offset, so every cart keeps its own default view. Zooming in past the default also lowers
    /// the camera toward a third-person angle; zooming out only pulls it back.
    /// </summary>
    [CreateAssetMenu(menuName = "Eco/Camera Zoom Settings", fileName = "CameraZoomSettings")]
    public class CameraZoomSettings : ScriptableObject
    {
        [Header("Range")]
        [Tooltip("Camera distance at full zoom-in, as a fraction of the cart's default distance.")]
        [SerializeField, Range(0.1f, 1f)] private float minDistanceScale = 0.4f;
        [Tooltip("Camera distance at full zoom-out, as a multiple of the cart's default distance.")]
        [SerializeField, Range(1f, 3f)] private float maxDistanceScale = 1.3f;

        [Header("Input")]
        [Tooltip("Fraction of the camera distance one scroll-wheel notch moves in or out.")]
        [SerializeField, Range(0.01f, 0.5f)] private float scrollStep = 0.12f;
        [Tooltip("Seconds the camera takes to settle on a new zoom.")]
        [SerializeField, Min(0f)] private float smoothTime = 0.15f;

        [Header("Tilt")]
        [Tooltip("Camera elevation above the player at full zoom-in, in degrees (90 = straight down).")]
        [SerializeField, Range(5f, 89f)] private float zoomedInElevation = 28f;
        [Tooltip("Degrees the camera looks above the player at full zoom-in, so the player sits lower on screen " +
                 "and more of what's ahead is visible.")]
        [SerializeField, Range(0f, 30f)] private float zoomedInLookUp = 10f;
        [Tooltip("Share of the tilt applied (0..1) across the zoom-in range (0 = default view, 1 = full zoom-in).")]
        [SerializeField] private AnimationCurve tiltCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public float MinDistanceScale => minDistanceScale;
        public float MaxDistanceScale => maxDistanceScale;
        public float ScrollStep => scrollStep;
        public float SmoothTime => smoothTime;
        public float ZoomedInElevation => zoomedInElevation;
        public float ZoomedInLookUp => zoomedInLookUp;

        public float EvaluateTilt(float zoomIn) => Mathf.Clamp01(tiltCurve.Evaluate(Mathf.Clamp01(zoomIn)));
    }
}
