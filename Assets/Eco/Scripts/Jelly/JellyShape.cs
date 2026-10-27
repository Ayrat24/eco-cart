using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eco.Scripts.Jelly
{
    /// <summary>
    /// Rest shape of the jelly body: a surface of revolution around y through a smooth profile curve, with lumps
    /// around the base like a settled puddle. Unit scale, the ground at y = 0, the front (mouth side) along +z.
    /// </summary>
    [Serializable]
    public class JellyShape
    {
        [Tooltip("Profile from the bottom centre to the top pole as (radius, height). A smooth curve passes " +
                 "through the points.")]
        [SerializeField] private Vector2[] profile =
        {
            new(0f, -0.04f), new(0.85f, -0.04f), new(1.06f, 0.03f), new(1.1f, 0.11f), new(1f, 0.2f),
            new(0.87f, 0.27f), new(0.88f, 0.42f), new(0.86f, 0.75f), new(0.78f, 1.05f), new(0.6f, 1.3f),
            new(0.33f, 1.46f), new(0f, 1.52f)
        };

        [SerializeField, Range(8, 128)] private int segments = 64;
        [SerializeField, Range(8, 128)] private int rings = 56;
        [SerializeField, Range(0f, 0.3f)] private float lumpAmplitude = 0.07f;
        [SerializeField] private float lumpHeight = 0.22f;
        [SerializeField] private int lumpSeed = 3;

        private const int SamplesPerSpan = 32;

        private float[] _ringRadius;
        private float[] _ringHeight;
        private Vector2[] _ringNormal;

        public float Height { get; private set; }
        public float MaxRadius { get; private set; }

        /// <summary>Resamples the profile into evenly spaced rings. Call before anything else.</summary>
        public void Build()
        {
            var curve = SampleCurve();

            var lengths = new float[curve.Count];
            for (int i = 1; i < curve.Count; i++)
            {
                lengths[i] = lengths[i - 1] + Vector2.Distance(curve[i - 1], curve[i]);
            }

            _ringRadius = new float[rings];
            _ringHeight = new float[rings];
            _ringNormal = new Vector2[rings];

            float total = lengths[^1];
            int span = 0;
            for (int i = 0; i < rings; i++)
            {
                float target = total * i / (rings - 1);
                while (span < curve.Count - 2 && lengths[span + 1] < target)
                {
                    span++;
                }

                float spanLength = lengths[span + 1] - lengths[span];
                float t = spanLength > 0f ? (target - lengths[span]) / spanLength : 0f;
                var point = Vector2.Lerp(curve[span], curve[span + 1], t);
                _ringRadius[i] = i == 0 || i == rings - 1 ? 0f : Mathf.Max(0f, point.x);
                _ringHeight[i] = point.y;
            }

            Height = _ringHeight[rings - 1];
            MaxRadius = 0f;
            for (int i = 0; i < rings; i++)
            {
                MaxRadius = Mathf.Max(MaxRadius, _ringRadius[i]);

                int prev = Mathf.Max(0, i - 1);
                int next = Mathf.Min(rings - 1, i + 1);
                var along = new Vector2(_ringRadius[next] - _ringRadius[prev], _ringHeight[next] - _ringHeight[prev]);
                _ringNormal[i] = new Vector2(along.y, -along.x).normalized;
            }

            _ringNormal[0] = Vector2.down;
            _ringNormal[rings - 1] = Vector2.up;
        }

        /// <summary>
        /// Rest surface point at an angle around y (radians, 0 = front) and a position along the profile
        /// (0 = bottom pole, 1 = top pole). The normal ignores the base lumps.
        /// </summary>
        public void Evaluate(float angle, float profileT, out Vector3 position, out Vector3 normal, out Vector3 tangent)
        {
            float ring = Mathf.Clamp01(profileT) * (rings - 1);
            int i0 = Mathf.Min((int)ring, rings - 2);
            float t = ring - i0;

            float height = Mathf.Lerp(_ringHeight[i0], _ringHeight[i0 + 1], t);
            float radius = Mathf.Lerp(_ringRadius[i0], _ringRadius[i0 + 1], t) * (1f + Lump(angle, height));
            var normal2D = Vector2.Lerp(_ringNormal[i0], _ringNormal[i0 + 1], t).normalized;

            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);
            position = new Vector3(radius * sin, height, radius * cos);
            normal = new Vector3(normal2D.x * sin, normal2D.y, normal2D.x * cos);
            tangent = new Vector3(cos, 0f, -sin);
        }

        /// <summary>Profile position of the side point at a height (0 = ground, 1 = top).</summary>
        public float HeightToProfile(float height01)
        {
            float height = Mathf.Clamp01(height01) * Height;

            // The side starts after the flat bottom; heights only rise from there on
            int first = 1;
            while (first < rings - 1 && _ringHeight[first] <= _ringHeight[0])
            {
                first++;
            }

            if (height <= _ringHeight[first])
            {
                return (float)first / (rings - 1);
            }

            for (int i = first; i < rings - 1; i++)
            {
                if (height <= _ringHeight[i + 1])
                {
                    float t = Mathf.InverseLerp(_ringHeight[i], _ringHeight[i + 1], height);
                    return (i + t) / (rings - 1);
                }
            }

            return 1f;
        }

        public float RadiusAtHeight(float height01)
        {
            float ring = HeightToProfile(height01) * (rings - 1);
            int i0 = Mathf.Min((int)ring, rings - 2);
            return Mathf.Lerp(_ringRadius[i0], _ringRadius[i0 + 1], ring - i0);
        }

        /// <summary>Builds the body mesh: poles at the bottom and top, rings in between, no seam.</summary>
        public Mesh CreateMesh()
        {
            int sideRings = rings - 2;
            int vertexCount = sideRings * segments + 2;
            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var tangents = new Vector4[vertexCount];

            int bottom = 0;
            int top = vertexCount - 1;
            vertices[bottom] = new Vector3(0f, _ringHeight[0], 0f);
            vertices[top] = new Vector3(0f, _ringHeight[rings - 1], 0f);

            for (int ring = 1; ring < rings - 1; ring++)
            {
                for (int s = 0; s < segments; s++)
                {
                    float angle = s * Mathf.PI * 2f / segments;
                    float radius = _ringRadius[ring] * (1f + Lump(angle, _ringHeight[ring]));
                    vertices[Index(ring, s)] = new Vector3(radius * Mathf.Sin(angle), _ringHeight[ring],
                        radius * Mathf.Cos(angle));
                }
            }

            normals[bottom] = Vector3.down;
            normals[top] = Vector3.up;
            tangents[bottom] = tangents[top] = new Vector4(1f, 0f, 0f, 1f);

            for (int ring = 1; ring < rings - 1; ring++)
            {
                for (int s = 0; s < segments; s++)
                {
                    var around = vertices[Index(ring, s + 1)] - vertices[Index(ring, s - 1)];
                    var below = ring == 1 ? vertices[bottom] : vertices[Index(ring - 1, s)];
                    var above = ring == rings - 2 ? vertices[top] : vertices[Index(ring + 1, s)];
                    var normal = Vector3.Cross(around, above - below).normalized;
                    var tangent = (around - normal * Vector3.Dot(around, normal)).normalized;

                    normals[Index(ring, s)] = normal;
                    tangents[Index(ring, s)] = new Vector4(tangent.x, tangent.y, tangent.z, 1f);
                }
            }

            var triangles = new List<int>(segments * (sideRings - 1) * 6 + segments * 6);
            for (int s = 0; s < segments; s++)
            {
                AddTriangle(triangles, vertices, normals, bottom, Index(1, s), Index(1, s + 1));
                AddTriangle(triangles, vertices, normals, top, Index(rings - 2, s + 1), Index(rings - 2, s));

                for (int ring = 1; ring < rings - 2; ring++)
                {
                    int a = Index(ring, s);
                    int b = Index(ring, s + 1);
                    int c = Index(ring + 1, s);
                    int d = Index(ring + 1, s + 1);
                    AddTriangle(triangles, vertices, normals, a, b, c);
                    AddTriangle(triangles, vertices, normals, b, d, c);
                }
            }

            var mesh = new Mesh { name = "JellyBody" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTangents(tangents);
            mesh.SetTriangles(triangles, 0);

            // The shader leans, squashes and spreads the body; keep it from being culled while it does
            mesh.RecalculateBounds();
            var bounds = mesh.bounds;
            bounds.Expand(new Vector3(1.2f, 0.8f, 1.2f));
            mesh.bounds = bounds;
            return mesh;

            int Index(int ring, int segment)
            {
                segment = (segment % segments + segments) % segments;
                return 1 + (ring - 1) * segments + segment;
            }
        }

        // Front faces wind clockwise: cross(b - a, c - a) points out of the body
        private static void AddTriangle(List<int> triangles, Vector3[] vertices, Vector3[] normals, int a, int b, int c)
        {
            var faceNormal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            bool outward = Vector3.Dot(faceNormal, normals[a] + normals[b] + normals[c]) >= 0f;
            triangles.Add(a);
            triangles.Add(outward ? b : c);
            triangles.Add(outward ? c : b);
        }

        private float Lump(float angle, float height)
        {
            float weight = Mathf.Exp(-Mathf.Pow(Mathf.Max(0f, height) / lumpHeight, 2f));
            float noise = 0.5f * Mathf.Sin(3f * angle + lumpSeed)
                          + 0.3f * Mathf.Sin(5f * angle + 2.1f * lumpSeed)
                          + 0.2f * Mathf.Sin(8f * angle + 0.7f * lumpSeed);
            return lumpAmplitude * weight * noise;
        }

        // Catmull-Rom through the profile points
        private List<Vector2> SampleCurve()
        {
            var points = new List<Vector2>((profile.Length - 1) * SamplesPerSpan + 1);
            for (int i = 0; i < profile.Length - 1; i++)
            {
                var p0 = profile[Mathf.Max(0, i - 1)];
                var p1 = profile[i];
                var p2 = profile[i + 1];
                var p3 = profile[Mathf.Min(profile.Length - 1, i + 2)];

                for (int s = 0; s < SamplesPerSpan; s++)
                {
                    float t = (float)s / SamplesPerSpan;
                    float t2 = t * t;
                    float t3 = t2 * t;
                    points.Add(0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                                       (3f * p1 - p0 - 3f * p2 + p3) * t3));
                }
            }

            points.Add(profile[^1]);
            return points;
        }
    }
}
