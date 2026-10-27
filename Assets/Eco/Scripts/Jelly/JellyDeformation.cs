using UnityEngine;

namespace Eco.Scripts.Jelly
{
    /// <summary>
    /// C# mirror of JellyDeform in Assets/Eco/Data/Shaders/JellyCommon.hlsl, minus the jiggle and ripples, so
    /// tentacles, the mouth and the hat can sit on the surface the shader draws. Keep the two in sync.
    /// Object space of the body mesh: y up, the ground at y = 0, the mouth facing +z.
    /// </summary>
    public static class JellyDeformation
    {
        private const float NeighbourOffset = 0.02f;

        public static Vector3 Deform(Vector3 p, Vector3 n, in JellyDeformParams d)
        {
            var rest = p;
            float h01 = Mathf.Clamp01(p.y / d.Height);

            p -= n * (d.MouthShape.z * d.MouthOpen * MouthDent(MouthMask(rest, d)));

            float stretch = Mathf.Max(0.2f, 1f + d.Squash);
            float spread = 1f / Mathf.Sqrt(stretch);
            p.y *= stretch;
            p.x *= spread;
            p.z *= spread;
            p.y -= d.Sink;

            p += d.Wobble * (h01 * h01 * (3f - 2f * h01));

            float under = Mathf.Max(0f, -p.y);
            p.y += under;
            float radial = Mathf.Sqrt(p.x * p.x + p.z * p.z);
            if (radial > 1e-4f)
            {
                float push = under * d.GroundSpread / radial;
                p.x += p.x * push;
                p.z += p.z * push;
            }

            return p;
        }

        /// <summary>Deforms a surface point and rebuilds its normal the way the vertex shader does.</summary>
        public static void DeformSurface(Vector3 p, Vector3 n, Vector3 tangent, in JellyDeformParams d,
            out Vector3 position, out Vector3 normal)
        {
            var bitangent = Vector3.Cross(n, tangent);
            position = Deform(p, n, d);
            var alongTangent = Deform(p + tangent * NeighbourOffset, n, d) - position;
            var alongBitangent = Deform(p + bitangent * NeighbourOffset, n, d) - position;
            var cross = Vector3.Cross(alongTangent, alongBitangent);
            normal = cross.sqrMagnitude > 1e-12f ? cross.normalized : n;
        }

        public static float MouthDent(float mask)
        {
            float t = Mathf.Clamp01(mask / 0.6f);
            return t * t * (3f - 2f * t);
        }

        public static float MouthMask(Vector3 rest, in JellyDeformParams d)
        {
            // Turn the body so the mouth faces +z
            float sin = Mathf.Sin(d.MouthAngle);
            float cos = Mathf.Cos(d.MouthAngle);
            float x = rest.x * cos - rest.z * sin;
            float z = rest.z * cos + rest.x * sin;

            float u = (x - d.MouthCenter.x) / d.MouthShape.x;
            float v = (rest.y - d.MouthCenter.y) / d.MouthShape.y - d.MouthShape.w * u * u;
            return Mathf.Clamp01(1f - (u * u + v * v)) * Mathf.Clamp01((z - d.MouthCenter.z * 0.5f) * 8f);
        }
    }
}
