using UnityEngine;

namespace Eco.Scripts.World
{
    /// <summary>Reads the baked terrain the world stands on.</summary>
    public static class WorldTerrain
    {
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
    }
}
