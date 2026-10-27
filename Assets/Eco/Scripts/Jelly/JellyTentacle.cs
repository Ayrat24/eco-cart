using Eco.Scripts.Profiling;
using UnityEngine;

namespace Eco.Scripts.Jelly
{
    /// <summary>
    /// A tentacle that grows out of the jelly body. Hidden until <see cref="Show"/>; whoever drives it sets the tip
    /// and how grown it is every frame, and it rebuilds its tube mesh from the anchor on the (deforming) body
    /// surface to the tip. Lives on a child of the creature root with an identity local pose.
    /// </summary>
    [DefaultExecutionOrder(110)]
    public class JellyTentacle : MonoBehaviour
    {
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [Tooltip("Follows the tip; held items are parented here.")]
        [SerializeField] private Transform tip;
        [SerializeField, Range(4, 24)] private int sides = 12;
        [SerializeField, Range(4, 48)] private int segments = 18;
        [Tooltip("Radii at the body's base size, in metres; they grow and shrink with the body.")]
        [SerializeField] private float rootRadius = 0.26f;
        [SerializeField] private float midRadius = 0.12f;
        [SerializeField] private float tipRadius = 0.17f;
        [Tooltip("How deep inside the body the tentacle starts, so it grows out of the surface.")]
        [SerializeField] private float embedDepth = 0.2f;
        [SerializeField] private float swayAmplitude = 0.06f;
        [SerializeField] private float swaySpeed = 5f;

        private const int CapRings = 3;

        private JellyBody _body;
        private JellyAnchor _anchor;
        private bool _shown;
        private float _swaySeed;

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

        /// <summary>Where the tentacle leaves the body surface, as of the last update.</summary>
        public Vector3 SurfacePosition { get; private set; }

        public Vector3 SurfaceNormal { get; private set; }

        private void Awake()
        {
            _centerline = new Vector3[segments + 1];
            int ringCount = segments + 1 + CapRings;
            _vertices = new Vector3[ringCount * sides + 1];
            _normals = new Vector3[_vertices.Length];

            _mesh = new Mesh { name = "JellyTentacle" };
            _mesh.MarkDynamic();
            _mesh.vertices = _vertices;
            _mesh.triangles = BuildTriangles(ringCount);
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
            _swaySeed = Random.Range(0f, 10f);
            UpdateSurface();
            TipPosition = SurfacePosition;
            TipDirection = Vector3.zero;
            Visibility = 0f;
        }

        public void Hide()
        {
            _shown = false;
            meshRenderer.enabled = false;
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
            float time = Time.timeSinceLevelLoad * swaySpeed + _swaySeed;

            _centerline[0] = surface - normal * (embedDepth * _body.Growth);
            for (int i = 1; i <= segments; i++)
            {
                float s = (float)(i - 1) / (segments - 1);
                float u = 1f - s;
                var point = u * u * u * surface + 3f * u * u * s * c1 + 3f * u * s * s * c2 + s * s * s * end;
                float sway = Mathf.Sin(time - s * 6f) * swayAmplitude * Mathf.Sin(s * Mathf.PI) * Mathf.Min(1f, length);
                _centerline[i] = point + side * sway;
            }
        }

        private void BuildTube()
        {
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

            for (int i = 0; i <= segments; i++)
            {
                var next = i < segments ? _centerline[i + 1] : _centerline[i] + tangent;
                var previous = i > 0 ? _centerline[i - 1] : _centerline[i] - tangent;
                var newTangent = (next - previous).normalized;
                if (newTangent.sqrMagnitude < 1e-6f)
                {
                    newTangent = tangent;
                }

                // Parallel transport keeps the rings from twisting
                frameNormal = Quaternion.FromToRotation(tangent, newTangent) * frameNormal;
                tangent = newTangent;

                float s = (float)i / segments;
                float radius = Mathf.Lerp(rootRadius, midRadius, Mathf.SmoothStep(0f, 1f, s / 0.4f))
                               + (tipRadius - midRadius) * Mathf.SmoothStep(0f, 1f, (s - 0.75f) / 0.25f);
                radius *= thickness;
                tipRingRadius = radius;

                AddRing(ref vertex, _centerline[i], tangent, frameNormal, radius, Vector3.zero);
            }

            // Rounded cap on the tip
            var end = _centerline[segments];
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
            for (int j = 0; j < sides; j++)
            {
                float angle = j * Mathf.PI * 2f / sides;
                var outward = frameNormal * Mathf.Cos(angle) + binormal * Mathf.Sin(angle);
                _vertices[vertex] = transform.InverseTransformPoint(center + outward * radius);
                _normals[vertex] = transform.InverseTransformDirection((outward * (1f - normalBend.magnitude) + normalBend).normalized);
                vertex++;
            }
        }

        private int[] BuildTriangles(int ringCount)
        {
            var triangles = new int[(ringCount - 1) * sides * 6 + sides * 3];
            int t = 0;
            for (int i = 0; i < ringCount - 1; i++)
            {
                for (int j = 0; j < sides; j++)
                {
                    int a = i * sides + j;
                    int b = i * sides + (j + 1) % sides;
                    int c = a + sides;
                    int d = b + sides;
                    triangles[t++] = a;
                    triangles[t++] = b;
                    triangles[t++] = c;
                    triangles[t++] = b;
                    triangles[t++] = d;
                    triangles[t++] = c;
                }
            }

            int pole = ringCount * sides;
            int last = (ringCount - 1) * sides;
            for (int j = 0; j < sides; j++)
            {
                triangles[t++] = last + j;
                triangles[t++] = last + (j + 1) % sides;
                triangles[t++] = pole;
            }

            return triangles;
        }
    }
}
