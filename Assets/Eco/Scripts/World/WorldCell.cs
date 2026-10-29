using UnityEngine;

namespace Eco.Scripts.World
{
    /// <summary>A painted hex of <see cref="WorldData"/>.</summary>
    [System.Serializable]
    public struct WorldCell
    {
        public Vector2Int coord;
        public ChunkDefinition chunk;

        public WorldCell(Vector2Int coord, ChunkDefinition chunk)
        {
            this.coord = coord;
            this.chunk = chunk;
        }
    }
}
