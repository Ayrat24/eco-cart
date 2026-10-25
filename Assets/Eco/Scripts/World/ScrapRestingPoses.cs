using System.Collections.Generic;
using UnityEngine;

namespace Eco.Scripts.World
{
    /// <summary>
    /// Where each scrap item came to rest, by chunk and tile, kept for this session only. When a chunk respawns its
    /// scrap goes back to that pose asleep instead of dropping and settling again, which wakes every rigidbody.
    /// </summary>
    public class ScrapRestingPoses
    {
        private readonly Dictionary<(Vector2Int chunk, int tile), Pose> _poses = new();

        public void Store(Vector2Int chunk, int tile, Pose localPose)
        {
            _poses[(chunk, tile)] = localPose;
        }

        public bool TryTake(Vector2Int chunk, int tile, out Pose localPose)
        {
            return _poses.Remove((chunk, tile), out localPose);
        }
    }
}
