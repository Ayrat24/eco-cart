using UnityEngine;

namespace Eco.Scripts.World
{
    /// <summary>Reads the baked terrain the world stands on.</summary>
    public static class WorldTerrain
    {
        // Baked shorelines are smooth, so a few samples can't miss the water
        private const int DipRings = 2;
        private const int DipSamplesPerRing = 8;

        /// <summary>World height of the terrain surface under <paramref name="position"/> (0 without a terrain).</summary>
        public static float GroundHeight(Vector3 position)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                return 0f;
            }

            return terrain.SampleHeight(position) + terrain.GetPosition().y;
        }

        /// <summary><paramref name="position"/> moved onto the terrain surface.</summary>
        public static Vector3 OnGround(Vector3 position)
        {
            position.y = GroundHeight(position);
            return position;
        }

        /// <summary>
        /// Whether the terrain goes below <paramref name="height"/> at <paramref name="position"/> or anywhere within
        /// <paramref name="radius"/> metres of it.
        /// </summary>
        public static bool DipsBelow(Vector3 position, float height, float radius)
        {
            if (GroundHeight(position) < height)
            {
                return true;
            }

            for (int ring = 1; ring <= DipRings; ring++)
            {
                float distance = radius * ring / DipRings;
                for (int i = 0; i < DipSamplesPerRing; i++)
                {
                    float angle = (i + ring * 0.5f) / DipSamplesPerRing * 2f * Mathf.PI;
                    var sample = position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
                    if (GroundHeight(sample) < height)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
