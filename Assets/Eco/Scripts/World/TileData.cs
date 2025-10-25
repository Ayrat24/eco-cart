using UnityEngine.Serialization;

namespace Eco.Scripts.World
{
    [System.Serializable]
    public struct TileData
    {
        public int objectType;
        public int objectId;
        public int ground;
        public bool clean;
        public bool containedTrash;
    }
}