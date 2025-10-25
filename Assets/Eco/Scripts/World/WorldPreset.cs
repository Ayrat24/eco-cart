using System.Collections.Generic;
using UnityEngine;

namespace Eco.Scripts.World
{
    [CreateAssetMenu(menuName = "Eco/WorldPreset", fileName = "WorldPreset")]
    public class WorldPreset : ScriptableObject
    {
        [SerializeField] private string worldId;
        [SerializeField] private int worldSideSize = 10;
        [SerializeField] private int seed = 5;
        [SerializeField] private int trashPerChunk = 3;
        [SerializeField] private ChunkType[] chunkTypes = new[] { ChunkType.Water };

        private Dictionary<Vector2Int, ChunkType> _map = new();

        public int WorldSideSize => worldSideSize;
        public ChunkType[] ChunkTypes => chunkTypes;
        public string WorldId => worldId;
        public int TrashPerChunk => trashPerChunk;
        
        public void GenerateMap()
        {
            _map = new Dictionary<Vector2Int, ChunkType>();

            Random.InitState(seed);

            var allTypes = (chunkTypes != null && chunkTypes.Length > 0) ? chunkTypes : new[] { ChunkType.Water };

            for (int x = -worldSideSize; x <= worldSideSize; x++)
            {
                for (int y = -worldSideSize; y <= worldSideSize; y++)
                {
                    var type = allTypes[0];

                    _map[new Vector2Int(x, y)] = type;
                }
            }
        }
        
        public ChunkType GetChunkType(Vector2Int coord)
        {
            return _map.GetValueOrDefault(coord, ChunkType.Water);
        }
    }
}
