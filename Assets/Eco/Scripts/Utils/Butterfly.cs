using UnityEngine;

namespace Eco.Scripts.Utils
{
    public class Butterfly : MonoBehaviour
    {
        [Header("Orbit")]
        [SerializeField] private Vector3 centerOffset = Vector3.zero;
        [SerializeField] private float radiusX = 2f;
        [SerializeField] private float radiusZ = 1f;
        [SerializeField] private float orbitSpeed = 1f; // radians per second

        [Header("Vertical bob")]
        [SerializeField] private float verticalAmplitude = 0.3f;
        [SerializeField] private float verticalSpeed = 1.5f; // cycles per second

        [Header("Options")]
        [SerializeField] private bool useLocalCoordinates = true;
        [SerializeField] private bool faceDirection = true;
        [SerializeField] private bool randomizePhase = true;

        // internal state
        private float _angle; // current orbit angle (radians)
        private float _verticalPhase; // phase offset for vertical bob
        private Vector3 _origin; // world or local origin depending on option

        private void Start()
        {
            // Set origin based on whether we use local or world coordinates
            _origin = useLocalCoordinates ? transform.localPosition - centerOffset : transform.position - centerOffset;

            if (randomizePhase)
            {
                _angle = Random.Range(0f, Mathf.PI * 2f);
                _verticalPhase = Random.Range(0f, Mathf.PI * 2f);
            }
            else
            {
                _angle = 0f;
                _verticalPhase = 0f;
            }
        }

        private void Update()
        {
            // advance angle
            _angle += orbitSpeed * Time.deltaTime;

            // horizontal oval position
            float x = Mathf.Cos(_angle) * radiusX;
            float z = Mathf.Sin(_angle) * radiusZ;

            // vertical bobbing (independent of orbit speed)
            float y = Mathf.Sin((Time.time * verticalSpeed * Mathf.PI * 2f) + _verticalPhase) * verticalAmplitude;

            var target = new Vector3(x, y, z) + centerOffset + _origin;

            if (useLocalCoordinates)
                transform.localPosition = target;
            else
                transform.position = target;

            if (faceDirection)
            {
                // compute a small look-ahead position to derive motion direction
                float lookAhead = 0.02f;
                float futureAngle = _angle + orbitSpeed * lookAhead;
                float fx = Mathf.Cos(futureAngle) * radiusX;
                float fz = Mathf.Sin(futureAngle) * radiusZ;
                float fy = Mathf.Sin((Time.time + lookAhead) * verticalSpeed * Mathf.PI * 2f + _verticalPhase) * verticalAmplitude;

                var future = new Vector3(fx, fy, fz) + centerOffset + _origin;
                var forward = (future - target);

                if (forward.sqrMagnitude > 0.0001f)
                {
                    // Keep the butterfly upright by projecting forward onto XZ plane for yaw then combine pitch from vertical difference
                    Vector3 flatForward = new Vector3(forward.x, 0f, forward.z).normalized;
                    Quaternion yaw = flatForward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(flatForward, Vector3.up) : transform.rotation;

                    // compute small pitch to look slightly up/down according to vertical movement
                    float verticalDelta = forward.y;
                    float pitchAngle = Mathf.Clamp(verticalDelta * 10f, -30f, 30f);
                    Quaternion pitch = Quaternion.Euler(pitchAngle, 0f, 0f);

                    // combine yaw then pitch
                    transform.rotation = yaw * pitch;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // visualize oval path
            Gizmos.color = Color.magenta;
            Vector3 origin = useLocalCoordinates ? (Application.isPlaying ? (_origin + centerOffset) : (transform.localPosition + centerOffset)) : (Application.isPlaying ? (_origin + centerOffset) : (transform.position + centerOffset));
            int steps = 64;
            Vector3 prev = origin + new Vector3(Mathf.Cos(0) * radiusX, 0f, Mathf.Sin(0) * radiusZ);

            for (int i = 1; i <= steps; i++)
            {
                float t = (i / (float)steps) * Mathf.PI * 2f;
                Vector3 next = origin + new Vector3(Mathf.Cos(t) * radiusX, 0f, Mathf.Sin(t) * radiusZ);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }

            // draw center
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(origin + centerOffset, 0.05f);
        }
    }
}
