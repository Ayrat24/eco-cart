using System.Collections.Generic;
using UnityEngine;

namespace Eco.Scripts.World
{
    /// <summary>
    /// Pointy-top hex grid math in axial coordinates, stored as <see cref="Vector2Int"/> (x = q, y = r). Hex (0, 0)
    /// is centred on the world origin; q runs along +X, r along +Z tilted 60°. <c>radius</c> is the distance from a
    /// hex centre to its corners, so the flat sides are <see cref="Apothem"/> from the centre.
    /// </summary>
    public static class Hex
    {
        public static readonly float Sqrt3 = Mathf.Sqrt(3f);

        public static readonly Vector2Int[] Directions =
        {
            new(1, 0), new(1, -1), new(0, -1), new(-1, 0), new(-1, 1), new(0, 1)
        };

        public static float Apothem(float radius) => radius * Sqrt3 * 0.5f;

        /// <summary>Distance between the centres of two neighbouring hexes.</summary>
        public static float Spacing(float radius) => radius * Sqrt3;

        public static Vector2 ToPlane(Vector2Int coord, float radius)
        {
            return new Vector2(radius * Sqrt3 * (coord.x + coord.y * 0.5f), radius * 1.5f * coord.y);
        }

        public static Vector3 ToWorld(Vector2Int coord, float radius, float y = 0f)
        {
            var p = ToPlane(coord, radius);
            return new Vector3(p.x, y, p.y);
        }

        public static Vector2Int FromPlane(Vector2 position, float radius)
        {
            float q = (Sqrt3 / 3f * position.x - position.y / 3f) / radius;
            float r = 2f / 3f * position.y / radius;
            return Round(q, r);
        }

        public static Vector2Int FromWorld(Vector3 position, float radius)
        {
            return FromPlane(new Vector2(position.x, position.z), radius);
        }

        public static int Distance(Vector2Int a, Vector2Int b)
        {
            int dq = a.x - b.x;
            int dr = a.y - b.y;
            return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(dq + dr)) / 2;
        }

        /// <summary>Fills <paramref name="results"/> with every hex within <paramref name="range"/> of the centre.</summary>
        public static void Range(Vector2Int center, int range, List<Vector2Int> results)
        {
            results.Clear();
            for (int q = -range; q <= range; q++)
            {
                int rMin = Mathf.Max(-range, -q - range);
                int rMax = Mathf.Min(range, -q + range);
                for (int r = rMin; r <= rMax; r++)
                {
                    results.Add(new Vector2Int(center.x + q, center.y + r));
                }
            }
        }

        /// <summary>Number of hexes within <paramref name="range"/> of a hex, itself included.</summary>
        public static int CountInRange(int range) => 3 * range * (range + 1) + 1;

        /// <summary>
        /// Distance from <paramref name="local"/> (relative to the hex centre) to the hex outline: negative inside,
        /// zero on the edge. Measured along the edge normals, so outside the hex it grows like a scaled-up hexagon.
        /// </summary>
        public static float SignedDistance(Vector2 local, float radius)
        {
            float x = Mathf.Abs(local.x);
            float a = Mathf.Abs(local.x * 0.5f + local.y * Sqrt3 * 0.5f);
            float b = Mathf.Abs(-local.x * 0.5f + local.y * Sqrt3 * 0.5f);
            return Mathf.Max(x, Mathf.Max(a, b)) - Apothem(radius);
        }

        /// <summary>Corner <paramref name="index"/> (0-5) relative to the hex centre, counter-clockwise from +X-ish.</summary>
        public static Vector2 Corner(int index, float radius)
        {
            float angle = Mathf.Deg2Rad * (60f * index - 30f);
            return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }

        /// <summary>A fixed pseudo-random value in [0, 1) for a hex; different salts give unrelated values.</summary>
        public static float Random01(Vector2Int coord, int salt)
        {
            unchecked
            {
                uint h = (uint)(coord.x * 374761393 + coord.y * 668265263 + salt * 1274126177);
                h = (h ^ (h >> 13)) * 1103515245;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        private static Vector2Int Round(float q, float r)
        {
            float s = -q - r;
            int rq = Mathf.RoundToInt(q);
            int rr = Mathf.RoundToInt(r);
            int rs = Mathf.RoundToInt(s);

            float dq = Mathf.Abs(rq - q);
            float dr = Mathf.Abs(rr - r);
            float ds = Mathf.Abs(rs - s);

            if (dq > dr && dq > ds)
            {
                rq = -rr - rs;
            }
            else if (dr > ds)
            {
                rr = -rq - rs;
            }

            return new Vector2Int(rq, rr);
        }
    }
}
