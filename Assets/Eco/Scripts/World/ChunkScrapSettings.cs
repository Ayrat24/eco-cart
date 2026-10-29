using System.Collections.Generic;
using Eco.Scripts.Scrap;
using UnityEngine;

namespace Eco.Scripts.World
{
    /// <summary>What scrap a fresh land chunk starts with.</summary>
    [System.Serializable]
    public class ChunkScrapSettings
    {
        [Tooltip("Loose scrap items spawned on a fresh chunk, picked between min and max (inclusive).")]
        [SerializeField] private Vector2Int count = new(2, 3);

        [Tooltip("Scrap prefabs this chunk spawns. Empty = every scrap item of the world.")]
        [SerializeField] private List<ScrapItem> items = new();

        [Tooltip("Size of the scrap pile in the chunk's centre (dig steps at dig power 1). 0 = no pile. " +
                 "The chunk prefab needs a ScrapPileSite; the World Editor adds and removes it.")]
        [SerializeField, Min(0)] private int pileSize;

        public Vector2Int Count => count;
        public IReadOnlyList<ScrapItem> Items => items;
        public int PileSize => pileSize;
        public bool HasPile => pileSize > 0;

        public int RollCount()
        {
            int min = Mathf.Max(0, Mathf.Min(count.x, count.y));
            int max = Mathf.Max(count.x, count.y);
            return Random.Range(min, max + 1);
        }
    }
}
