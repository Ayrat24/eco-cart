using UnityEngine;

namespace Eco.Scripts.Utils
{
    public class WheelSpinner : MonoBehaviour
    {
        private enum SampleMode { Update, LateUpdate, FixedUpdate }

        [Header("Wheel geometry")]
        [Tooltip("Wheel radius in meters (distance from center to contact).")]
        [SerializeField] private float wheelRadius = 0.3f;

        [Header("Axes (local)")]
        [Tooltip("Local axis the wheel rotates around (usually X).")]
        [SerializeField] private Vector3 localRotationAxis = Vector3.right;
        [Tooltip("Local forward direction that corresponds to vehicle forward for signed rotation (usually Z).")]
        [SerializeField] private Vector3 localForwardAxis = Vector3.forward;

        [Header("Options")]
        [Tooltip("Which update loop to sample movement from. Choose FixedUpdate for physics-driven motion, LateUpdate for animations/transform updates.")]
        [SerializeField] private SampleMode sampleMode = SampleMode.LateUpdate;
        [Tooltip("Optional transform to sample movement from (defaults to top-level root). Use this when wheel's own transform doesn't reflect vehicle translation.)")]
        [SerializeField] private Transform referenceTransform;
        [Tooltip("Multiplier to tune rotation speed (1 = physical).")]
        [SerializeField] private float speedMultiplier = 1f;
        [Tooltip("Smooth changes in rotation speed.")]
        [SerializeField] private bool smooth;
        [Tooltip("Smoothing factor if enabled.")]
        [SerializeField] private float smoothing = 8f;
        [Tooltip("Ignore vertical (Y) movement when computing distance. Useful to avoid wheel spinning on bumps.")]
        [SerializeField] private bool ignoreVertical = true;
        [Tooltip("Minimum movement (meters) considered as motion to avoid jitter.")]
        [SerializeField] private float movementEpsilon = 0.0001f;

        private float _currentDegPerSec;
        private Vector3 _previousPosition;
        private bool _hasPreviousPosition;

        private void Start()
        {
            // default reference transform to the root so straight translation of the vehicle rotates wheels
            if (referenceTransform == null)
                referenceTransform = transform.root;

            _previousPosition = referenceTransform.position;
            _hasPreviousPosition = true;

            wheelRadius = Mathf.Max(0.0001f, wheelRadius);
            if (localForwardAxis == Vector3.zero) localForwardAxis = Vector3.forward;
            if (localRotationAxis == Vector3.zero) localRotationAxis = Vector3.right;
        }

        private void Update()
        {
            if (sampleMode == SampleMode.Update)
                Sample(Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (sampleMode == SampleMode.LateUpdate)
                Sample(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (sampleMode == SampleMode.FixedUpdate)
                Sample(Time.fixedDeltaTime);
        }

        // Centralized sampling logic: rotation depends only on movement magnitude (speed)
        private void Sample(float dt)
        {
            if (dt <= 0f) return;

            if (!_hasPreviousPosition)
            {
                _previousPosition = referenceTransform.position;
                _hasPreviousPosition = true;
                return; // no movement to sample this frame
            }

            // Compute delta from the configured reference transform
            Vector3 delta = referenceTransform.position - _previousPosition;
            if (ignoreVertical) delta.y = 0f;
            float distance = delta.magnitude; // meters
            _previousPosition = referenceTransform.position;

            // Treat tiny movement as zero to prevent jitter
            float speed = 0f;
            if (distance > movementEpsilon)
            {
                speed = distance / Mathf.Max(0.00001f, dt);
            }

            // angular speed (deg/s) from linear speed magnitude: (v / r) [rad/s] * Rad2Deg
            float targetDegPerSec = (speed / wheelRadius) * Mathf.Rad2Deg * speedMultiplier;

            if (smooth)
            {
                _currentDegPerSec = Mathf.Lerp(_currentDegPerSec, targetDegPerSec, Mathf.Clamp01(dt * smoothing));
            }
            else
            {
                _currentDegPerSec = targetDegPerSec;
            }

            // rotate by angle = deg/sec * deltaTime around local rotation axis
            float angleThisStep = _currentDegPerSec * dt;
            transform.Rotate(localRotationAxis.normalized, angleThisStep, Space.Self);
        }

        // Call to reset the baseline to current position (useful after teleporting or snapping)
        public void ResetBaseline()
        {
            _previousPosition = (referenceTransform != null) ? referenceTransform.position : transform.position;
            _hasPreviousPosition = true;
            _currentDegPerSec = 0f;
        }
    }
}