using System.Linq;
using UnityEngine;

namespace Eco.Scripts.World
{
    [CreateAssetMenu(menuName = "Eco/ChunkGenerationRules", fileName = "ChunkGenerationRules")]
    public class ChunkGenerationRules : ScriptableObject
    {
        [System.Serializable]
        public class ChunkRule
        {
            public ChunkType type;
            public ChunkType[] allowedAdjacent; // if null or empty, allow all
            public int minCount;
            public int maxCount = int.MaxValue;
            public int weight = 1;
        }

        [SerializeField] private ChunkRule[] rules;

        public ChunkRule[] Rules => rules;

        public ChunkRule GetRule(ChunkType t)
        {
            if (rules == null || rules.Length == 0)
            {
                return new ChunkRule
                    { type = t, allowedAdjacent = null, minCount = 0, maxCount = int.MaxValue, weight = 1 };
            }

            var r = rules.FirstOrDefault(x => x.type == t);
            if (r == null)
            {
                return new ChunkRule
                    { type = t, allowedAdjacent = null, minCount = 0, maxCount = int.MaxValue, weight = 1 };
            }

            return r;
        }
    }
}