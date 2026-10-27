using System;
using UnityEngine;

namespace Eco.Scripts.Jelly
{
    /// <summary>A patch of the jelly body that tentacles may grow from.</summary>
    [Serializable]
    public struct JellyHandZone
    {
        [Tooltip("0 = ground, 1 = top of the body.")]
        [Range(0f, 1f)] public float minHeight;
        [Range(0f, 1f)] public float maxHeight;

        [Tooltip("Degrees around the body, 0 = front (mouth side), positive = to the creature's right. " +
                 "A zone with min > max wraps around the back.")]
        [Range(-180f, 180f)] public float minAngle;
        [Range(-180f, 180f)] public float maxAngle;

        public bool Contains(float height01, float angleDegrees)
        {
            if (height01 < minHeight || height01 > maxHeight)
            {
                return false;
            }

            return minAngle <= maxAngle
                ? angleDegrees >= minAngle && angleDegrees <= maxAngle
                : angleDegrees >= minAngle || angleDegrees <= maxAngle;
        }
    }
}
