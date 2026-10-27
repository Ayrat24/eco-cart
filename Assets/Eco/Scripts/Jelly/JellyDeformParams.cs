using UnityEngine;

namespace Eco.Scripts.Jelly
{
    /// <summary>Per-frame body deformation state, shared by the shader and <see cref="JellyDeformation"/>.</summary>
    public struct JellyDeformParams
    {
        public float Height;
        public Vector3 Wobble;
        public float Squash;
        public float Sink;
        public float GroundSpread;
        public Vector3 MouthCenter;
        public float MouthOpen;
        public Vector4 MouthShape;
    }
}
