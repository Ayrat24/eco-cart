using System.Collections.Generic;
using System.Linq;
using Eco.Scripts.Trash;
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

        [SerializeField] private List<TrashItem> allowedTrashItems;

        // Rules asset that controls adjacency, min/max and weights for chunk generation
        [SerializeField] private ChunkGenerationRules generationRules;

        public int WorldSideSize => worldSideSize;
        public ChunkType[] ChunkTypes => chunkTypes;
        public string WorldId => worldId;
        public int TrashPerChunk => trashPerChunk;
        public List<TrashItem> AllowedTrashItems => allowedTrashItems;
        public ChunkGenerationRules GenerationRules => generationRules;

        private Dictionary<Vector2Int, ChunkType> _map = new();

        public void GenerateMap()
        {
            _map = new Dictionary<Vector2Int, ChunkType>();

            Random.InitState(seed);

            // Build coordinate list and shuffle it to avoid directional bias
            var coords = new List<Vector2Int>();
            for (int x = -worldSideSize; x <= worldSideSize; x++)
            {
                for (int y = -worldSideSize; y <= worldSideSize; y++)
                {
                    coords.Add(new Vector2Int(x, y));
                }
            }

            // Fisher-Yates shuffle
            for (int i = coords.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = coords[i];
                coords[i] = coords[j];
                coords[j] = tmp;
            }

            // Counters for placed types
            var counters = new Dictionary<ChunkType, int>();
            foreach (var t in chunkTypes)
            {
                counters[t] = 0;
            }

            // Place tiles in shuffled order with respect to rules
            foreach (var coord in coords)
            {
                var candidates = GetValidCandidates(coord, counters);

                ChunkType chosen;
                if (candidates.Count == 0)
                {
                    // Relaxation: allow any chunk type from pool that hasn't hit its max
                    var fallback = chunkTypes.Where(t => !HasReachedMax(counters, t)).ToList();
                    if (fallback.Count == 0)
                    {
                        // Everything reached max or pool empty - use first available in pool (honor user's config)
                        chosen = chunkTypes.Length > 0 ? chunkTypes[0] : ChunkType.Water;
                    }
                    else
                    {
                        chosen = fallback[Random.Range(0, fallback.Count)];
                    }
                }
                else
                {
                    // Weighted random pick among candidates
                    int totalWeight = candidates.Sum(c => c.Rule.weight);
                    int pick = totalWeight <= 0 ? 0 : Random.Range(0, totalWeight);
                    int acc = 0;
                    chosen = candidates[0].Type; // default
                    foreach (var c in candidates)
                    {
                        acc += Mathf.Max(0, c.Rule.weight);
                        if (pick < acc)
                        {
                            chosen = c.Type;
                            break;
                        }
                    }
                }

                _map[coord] = chosen;
                if (counters.ContainsKey(chosen)) counters[chosen]++;
                else counters[chosen] = 1;
            }

            // Try to satisfy MinCount constraints by attempting to replace tiles where possible
            RepairMinimums(counters, coords);
        }

        private struct Candidate
        {
            public ChunkType Type;
            public ChunkGenerationRules.ChunkRule Rule;
        }

        private List<Candidate> GetValidCandidates(Vector2Int coord, Dictionary<ChunkType, int> counters)
        {
            var list = new List<Candidate>();
            foreach (var type in chunkTypes)
            {
                var rule = generationRules.GetRule(type);

                // Check max
                if (HasReachedMax(counters, type, rule))
                    continue;

                // Check adjacency against already placed neighbors
                if (!IsAllowedByNeighbors(coord, type))
                    continue;

                list.Add(new Candidate { Type = type, Rule = rule });
            }

            return list;
        }

        private bool HasReachedMax(Dictionary<ChunkType, int> counters, ChunkType type,
            ChunkGenerationRules.ChunkRule rule = null)
        {
            if (rule == null) rule = generationRules.GetRule(type);
            counters.TryGetValue(type, out var count);
            return count >= rule.maxCount;
        }

        private bool IsAllowedByNeighbors(Vector2Int coord, ChunkType candidate)
        {
            // check 4-neighbors only
            Vector2Int[] neighbors =
            {
                new Vector2Int(coord.x + 1, coord.y),
                new Vector2Int(coord.x - 1, coord.y),
                new Vector2Int(coord.x, coord.y + 1),
                new Vector2Int(coord.x, coord.y - 1),
            };

            var candidateRule = generationRules.GetRule(candidate);

            foreach (var n in neighbors)
            {
                if (!_map.TryGetValue(n, out var neighborType)) continue; // neighbor not placed yet - fine

                // If candidate has an AllowedAdjacent list, neighborType must be in it
                if (candidateRule.allowedAdjacent != null && candidateRule.allowedAdjacent.Length > 0)
                {
                    if (!candidateRule.allowedAdjacent.Contains(neighborType))
                        return false;
                }

                // Additionally, if neighbor has a rule, it should allow candidate (if it has restrictions)
                var neighborRule = generationRules.GetRule(neighborType);
                if (neighborRule.allowedAdjacent != null && neighborRule.allowedAdjacent.Length > 0)
                {
                    if (!neighborRule.allowedAdjacent.Contains(candidate))
                        return false;
                }
            }

            return true;
        }

        private void RepairMinimums(Dictionary<ChunkType, int> counters, List<Vector2Int> coords)
        {
            // For each rule that is below its min, attempt to replace some other tiles
            // Only consider rules for types that are present in this preset's chunkTypes array.
            foreach (var rule in generationRules.Rules.Where(r => chunkTypes.Contains(r.type)))
            {
                counters.TryGetValue(rule.type, out var current);
                if (current >= rule.minCount) continue;

                int need = rule.minCount - current;

                // Try to find replaceable coords (where changing the type to rule.Type won't violate neighbor rules and won't break max constraints)
                var replaceable = new List<Vector2Int>();

                foreach (var coord in coords)
                {
                    if (!_map.TryGetValue(coord, out var existing)) continue;
                    if (existing == rule.type) continue;

                    // Skip if target type reached its max
                    if (HasReachedMax(counters, rule.type, rule)) break;

                    // Temporarily remove existing to test adjacency
                    var prev = _map[coord];
                    _map.Remove(coord);

                    bool canPlace = IsAllowedByNeighbors(coord, rule.type);

                    // restore
                    _map[coord] = prev;

                    if (canPlace)
                        replaceable.Add(coord);

                    if (replaceable.Count >= need) break;
                }

                // Perform replacements up to 'need'
                int replaced = 0;
                foreach (var rc in replaceable)
                {
                    if (replaced >= need) break;

                    var old = _map[rc];
                    // Remove and test again for safety
                    _map.Remove(rc);
                    if (IsAllowedByNeighbors(rc, rule.type))
                    {
                        _map[rc] = rule.type;

                        // update counters
                        if (counters.ContainsKey(old)) counters[old] = MathMax(0, counters[old] - 1);
                        counters[rule.type] = counters.TryGetValue(rule.type, out var val) ? val + 1 : 1;

                        replaced++;
                    }
                    else
                    {
                        // restore original
                        _map[rc] = old;
                    }
                }

                // If we couldn't reach the minimum, we leave as-is (honor constraints best-effort)
            }
        }

        // small helper to avoid System.Math dependency
        private int MathMax(int a, int b) => a > b ? a : b;

        public ChunkType GetChunkType(Vector2Int coord)
        {
            return _map.GetValueOrDefault(coord, ChunkType.Water);
        }
    }
}