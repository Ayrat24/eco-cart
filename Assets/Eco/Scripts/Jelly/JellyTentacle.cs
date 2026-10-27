using Eco.Scripts.Profiling;
using UnityEngine;

namespace Eco.Scripts.Jelly
{
    /// <summary>
    /// A tentacle that grows out of the jelly body. Hidden until <see cref="Show"/>; whoever drives it sets the tip
    /// and how grown it is every frame, and it rebuilds its tube mesh from the anchor on the (deforming) body
    /// surface to the tip. Its look comes from the body's <see cref="JellyHandSettings"/>.
    /// Lives on a child of the creature root with an identity local pose.
    /// </summary>
    [DefaultExecutionOrder(110)]
    public class JellyTentacle : MonoBehaviour
    {
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [Tooltip("Follows the tip; held items are parented here.")]
        [SerializeField] private Transform tip;

        private const int CapRings = 3;
        private const int ArmSamples = 64;

        private JellyBody _body;
        private JellyAnchor _anchor;
        private bool _shown;
        private float _swaySeed;
        private float _thinning = 1f;
        private int _sides;
        private int _segments;
        private readonly Vector3[] _armPoints = new Vector3[ArmSamples + 1];
        private readonly float[] _armLengths = new float[ArmSamples + 1];

        private Mesh _mesh;
        private Vector3[] _centerline;
        private Vector3[] _vertices;
        private Vector3[] _normals;

        public Transform Tip => tip;
        public bool IsShown => _shown;

        /// <summary>World position of the tip.</summary>
        public Vector3 TipPosition { get; set; }

        /// <summary>World direction the tip points at the end; zero to point along the tentacle.</summary>
        public Vector3 TipDirection { get; set; }

        /// <summary>0 = not grown out at all, 1 = full thickness.</summary>
        public float Visibility { get; set; }

        /// <summary>
        /// World point the hand stretches out to from the end of the arm, which keeps its shape. Used while
        /// <see cref="ReachWeight"/> is above 0.
        /// </summary>
        public Vector3 ReachPosition { get; set; }

        /// <summary>0 = no hand beyond the arm, 1 = it stretches all the way to <see cref="ReachPosition"/>.</summary>
        public float ReachWeight { get; set; }

        /// <summary>Where the tentacle leaves the body surface, as of the last update.</summary>
        public Vector3 SurfacePosition { get; private set; }

        public Vector3 SurfaceNormal { get; private set; }

        private void Awake()
        {
            _mesh = new Mesh { name = "JellyTentacle" };
            _mesh.MarkDynamic();
            meshFilter.sharedMesh = _mesh;
            meshRenderer.enabled = false;
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }

        public void Show(JellyBody body, JellyAnchor anchor)
        {
            _body = body;
            _anchor = anchor;
            _shown = true;
            EnsureBuffers(body.Hands);
            _swaySeed = Random.Range(0f, 10f);
            UpdateSurface();
            TipPosition = SurfacePosition;
            TipDirection = Vector3.zero;
            Visibility = 0f;
            ReachWeight = 0f;
        }

        public void Hide()
        {
            _shown = false;
            meshRenderer.enabled = false;
        }

        // Sized from the settings on each show, so ring and side counts can be tweaked while playing
        private void EnsureBuffers(JellyHandSettings hands)
        {
            if (_centerline != null && _segments == hands.Segments && _sides == hands.Sides)
            {
                return;
            }

            _segments = hands.Segments;
            _sides = hands.Sides;
            _centerline = new Vector3[_segments + 1];
            int ringCount = _segments + 1 + CapRings;
            _vertices = new Vector3[ringCount * _sides + 1];
            _normals = new Vector3[_vertices.Length];

            _mesh.Clear();
            _mesh.vertices = _vertices;
            _mesh.triangles = BuildTriangles(ringCount);
        }

        public void UpdateSurface()
        {
            _body.GetSurfacePose(_anchor, out var position, out var normal);
            SurfacePosition = position;
            SurfaceNormal = normal;
        }

        private void LateUpdate()
        {
            if (!_shown)
            {
                return;
            }

            using var marker = EcoProfilerMarkers.JellyTentacle.Auto();

            UpdateSurface();
            BuildCenterline();
            BuildTube();

            meshRenderer.enabled = Visibility > 0.001f;
        }

        private void BuildCenterline()
        {
            var surface = SurfacePosition;
            var normal = SurfaceNormal;
            var end = TipPosition;
            var toTip = end - surface;
            float length = toTip.magnitude;
            var tipDirection = TipDirection.sqrMagnitude > 1e-6f
                ? TipDirection.normalized
                : length > 1e-4f ? toTip / length : normal;

            var c1 = surface + normal * (length * 0.45f);
            var c2 = end - tipDirection * (length * 0.35f);

            var side = Vector3.Cross(normal, toTip);
            side = side.sqrMagnitude > 1e-6f ? side.normalized : Vector3.Cross(normal, Vector3.up).normalized;
            var hands = _body.Hands;
            float time = Time.timeSinceLevelLoad * hands.SwaySpeed + _swaySeed;

            // The arm, sampled finely so the rings can be spread evenly along it
            _armLengths[0] = 0f;
            for (int k = 0; k <= ArmSamples; k++)
            {
                float s = (float)k / ArmSamples;
                float u = 1f - s;
                var point = u * u * u * surface + 3f * u * u * s * c1 + 3f * u * s * s * c2 + s * s * s * end;
                float sway = Mathf.Sin(time - s * 6f) * hands.SwayAmplitude * Mathf.Sin(s * Mathf.PI) * Mathf.Min(1f, length);
                _armPoints[k] = point + side * sway;
                if (k > 0)
                {
                    _armLengths[k] = _armLengths[k - 1] + Vector3.Distance(_armPoints[k - 1], _armPoints[k]);
                }
            }

            // The hand grows out of the arm's end, setting off the way the arm points so there is no corner
            float armLength = _armLengths[ArmSamples];
            var armEnd = _armPoints[ArmSamples];
            var armTangent = (armEnd - _armPoints[ArmSamples - 1]).normalized;
            var handEnd = ReachWeight > 0f ? Vector3.Lerp(armEnd, ReachPosition, ReachWeight) : armEnd;
            float handLength = Vector3.Distance(armEnd, handEnd);
            var handBend = armEnd + armTangent * (handLength * 0.5f);
            float total = armLength + handLength;

            _centerline[0] = surface - normal * (hands.EmbedDepth * _body.Growth);
            int sample = 0;
            for (int i = 1; i <= _segments; i++)
            {
                float target = total * (i - 1) / (_segments - 1);
                if (target <= armLength || handLength < 1e-5f)
                {
                    while (sample < ArmSamples - 1 && _armLengths[sample + 1] < target)
                    {
                        sample++;
                    }

                    float span = _armLengths[sample + 1] - _armLengths[sample];
                    float t = span > 1e-6f ? (Mathf.Min(target, armLength) - _armLengths[sample]) / span : 0f;
                    _centerline[i] = Vector3.Lerp(_armPoints[sample], _armPoints[sample + 1], Mathf.Clamp01(t));
                }
                else
                {
                    float h = (target - armLength) / handLength;
                    float g = 1f - h;
                    _centerline[i] = g * g * armEnd + 2f * g * h * handBend + h * h * handEnd;
                }
            }

            // A stretched-out tentacle gets a little thinner all along
            _thinning = armLength > 1e-4f ? Mathf.Pow(armLength / total, 0.25f) : 1f;
        }

        private void BuildTube()
        {
            var hands = _body.Hands;
            float thickness = _body.Growth * Mathf.SmoothStep(0f, 1f, Visibility);

            var tangent = (_centerline[1] - _centerline[0]).normalized;
            var frameNormal = Vector3.Cross(tangent, Vector3.up);
            if (frameNormal.sqrMagnitude < 1e-6f)
            {
                frameNormal = Vector3.Cross(tangent, Vector3.right);
            }

            frameNormal.Normalize();
            int vertex = 0;
            float tipRingRadius = 0f;

            for (int i = 0; i <= _segments; i++)
            {
                var next = i < _segments ? _centerline[i + 1] : _centerline[i] + tangent;
                var previous = i > 0 ? _centerline[i - 1] : _centerline[i] - tangent;
                var newTangent = (next - previous).normalized;
                if (newTangent.sqrMagnitude < 1e-6f)
                {
                    newTangent = tangent;
                }

                // Parallel transport keeps the rings from twisting
                frameNormal = Quaternion.FromToRotation(tangent, newTangent) * frameNormal;
                tangent = newTangent;

                float s = (float)i / _segments;
                float radius = Mathf.Lerp(hands.RootRadius, hands.MidRadius, Mathf.SmoothStep(0f, 1f, s / 0.4f))
                               + (hands.TipRadius - hands.MidRadius) * Mathf.SmoothStep(0f, 1f, (s - 0.75f) / 0.25f);
                radius *= thickness * _thinning;
                tipRingRadius = radius;

                AddRing(ref vertex, _centerline[i], tangent, frameNormal, radius, Vector3.zero);
            }

            // Rounded cap on the tip
            var end = _centerline[_segments];
            for (int k = 1; k <= CapRings; k++)
            {
                float angle = k * Mathf.PI * 0.5f / (CapRings + 1);
                AddRing(ref vertex, end + tangent * (tipRingRadius * Mathf.Sin(angle)), tangent, frameNormal,
                    tipRingRadius * Mathf.Cos(angle), tangent * Mathf.Sin(angle));
            }

            _vertices[vertex] = transform.InverseTransformPoint(end + tangent * tipRingRadius);
            _normals[vertex] = transform.InverseTransformDirection(tangent);

            _mesh.vertices = _vertices;
            _mesh.normals = _normals;
            _mesh.RecalculateBounds();

            tip.SetPositionAndRotation(end, Quaternion.LookRotation(tangent, frameNormal));
        }

        private void AddRing(ref int vertex, Vector3 center, Vector3 tangent, Vector3 frameNormal, float radius,
            Vector3 normalBend)
        {
            var binormal = Vector3.Cross(tangent, frameNormal);
            for (int j = 0; j < _sides; j++)
            {
                float angle = j * Mathf.PI * 2f / _sides;
                var outward = frameNormal * Mathf.Cos(angle) + binormal * Mathf.Sin(angle);
                _vertices[vertex] = transform.InverseTransformPoint(center + outward * radius);
                _normals[vertex] = transform.InverseTransformDirection((outward * (1f - normalBend.magnitude) + normalBend).normalized);
                vertex++;
            }
        }

        private int[] BuildTriangles(int ringCount)
        {
            var triangles = new int[(ringCount - 1) * _sides * 6 + _sides * 3];
            int t = 0;
            for (int i = 0; i < ringCount - 1; i++)
            {
                for (int j = 0; j < _sides; j++)
                {
                    int a = i * _sides + j;
                    int b = i * _sides + (j + 1) % _sides;
                    int c = a + _sides;
                    int d = b + _sides;
                    triangles[t++] = a;
                    triangles[t++] = b;
                    triangles[t++] = c;
                    triangles[t++] = b;
                    triangles[t++] = d;
                    triangles[t++] = c;
                }
            }

            int pole = ringCount * _sides;
            int last = (ringCount - 1) * _sides;
            for (int j = 0; j < _sides; j++)
            {
                triangles[t++] = last + j;
                triangles[t++] = last + (j + 1) % _sides;
                triangles[t++] = pole;
            }

            return triangles;
        }
    }
}
