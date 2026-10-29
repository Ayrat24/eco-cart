using System.Collections.Generic;
using Eco.Scripts.Scrap;
using UnityEngine;

namespace Eco.Scripts.World
{
    /// <summary>
    /// The hand-painted world: a palette of <see cref="ChunkDefinition"/>s and the hex cells painted with them.
    /// Every hex that isn't painted is <see cref="Ocean"/>. Painted in <b>Eco → World Editor</b>, whose Save button
    /// also bakes the cells into the scene's terrain (heights, textures) — the game reads the baked terrain.
    /// </summary>
    [CreateAssetMenu(menuName = "Eco/World/World Data", fileName = "WorldData")]
    public class WorldData : ScriptableObject
    {
        [Header("Grid")]
        [Tooltip("Centre-to-corner size of a hex chunk in metres. Changing it moves every chunk and discards saves.")]
        [SerializeField, Min(2)] private float hexRadius = 6f;
        [Tooltip("How many hexes from the centre can be painted.")]
        [SerializeField, Min(1)] private int mapRadius = 20;

        [Header("Terrain")]
        [Tooltip("World height of the water surface. Ground level is 0.")]
        [SerializeField] private float waterLevel = -1f;
        [Tooltip("Metres on each side of a hex edge over which heights blend into the neighbour.")]
        [SerializeField, Min(0.1f)] private float heightBlend = 2.5f;
        [Tooltip("Metres on each side of a hex edge over which textures blend into the neighbour.")]
        [SerializeField, Min(0.1f)] private float textureBlend = 1.5f;
        [Tooltip("Size of the roughness bumps in metres.")]
        [SerializeField, Min(0.5f)] private float roughnessSize = 8f;
        [Tooltip("Layer painted around planted trees and cleared piles in the game.")]
        [SerializeField] private TerrainLayer grassLayer;

        [Header("Content")]
        [Tooltip("Every scrap item of the world, pooled when the game starts. Chunks pick from these.")]
        [SerializeField] private List<ScrapItem> scrapItems = new();
        [SerializeField] private List<ChunkDefinition> palette = new();
        [Tooltip("Chunk used for every unpainted hex.")]
        [SerializeField] private ChunkDefinition ocean;

        [SerializeField, HideInInspector] private List<WorldCell> cells = new();

        private Dictionary<Vector2Int, int> _lookup;

        public float HexRadius => hexRadius;
        public int MapRadius => mapRadius;
        public float WaterLevel => waterLevel;
        public float HeightBlend => heightBlend;
        public float TextureBlend => textureBlend;
        public float RoughnessSize => roughnessSize;
        public TerrainLayer GrassLayer => grassLayer;
        public List<ScrapItem> ScrapItems => scrapItems;
        public List<ChunkDefinition> Palette => palette;
        public ChunkDefinition Ocean => ocean;
        public IReadOnlyList<WorldCell> Cells => cells;

        /// <summary>The painted chunk at the hex, or <see cref="Ocean"/>.</summary>
        public ChunkDefinition GetChunk(Vector2Int coord)
        {
            return GetPainted(coord) ?? ocean;
        }

        /// <summary>The painted chunk at the hex, or null.</summary>
        public ChunkDefinition GetPainted(Vector2Int coord)
        {
            EnsureLookup();
            return _lookup.TryGetValue(coord, out int index) ? cells[index].chunk : null;
        }

        public bool InMap(Vector2Int coord) => Hex.Distance(coord, Vector2Int.zero) <= mapRadius;

        /// <summary>The painted land hex closest to the centre, where a new game starts. Hexes with a pile only
        /// when there is nothing else.</summary>
        public Vector2Int GetSpawnCoord()
        {
            var best = Vector2Int.zero;
            int bestDistance = int.MaxValue;
            foreach (var cell in cells)
            {
                if (cell.chunk == null || cell.chunk.IsWater)
                {
                    continue;
                }

                int distance = Hex.Distance(cell.coord, Vector2Int.zero) +
                               (cell.chunk.Scrap.HasPile ? mapRadius * 4 : 0);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = cell.coord;
                }
            }

            return best;
        }

        /// <summary>World-space XZ rectangle around every painted land hex, or false when there is none.</summary>
        public bool TryGetLandBounds(out Rect bounds)
        {
            bool any = false;
            var min = Vector2.zero;
            var max = Vector2.zero;
            foreach (var cell in cells)
            {
                if (cell.chunk == null || cell.chunk.IsWater)
                {
                    continue;
                }

                var center = Hex.ToPlane(cell.coord, hexRadius);
                if (!any)
                {
                    min = max = center;
                    any = true;
                }

                min = Vector2.Min(min, center);
                max = Vector2.Max(max, center);
            }

            bounds = any ? Rect.MinMaxRect(min.x - hexRadius, min.y - hexRadius, max.x + hexRadius, max.y + hexRadius)
                : default;
            return any;
        }

        public void SetCell(Vector2Int coord, ChunkDefinition chunk)
        {
            if (chunk == null)
            {
                ClearCell(coord);
                return;
            }

            EnsureLookup();
            if (_lookup.TryGetValue(coord, out int index))
            {
                cells[index] = new WorldCell(coord, chunk);
                return;
            }

            _lookup[coord] = cells.Count;
            cells.Add(new WorldCell(coord, chunk));
        }

        public void ClearCell(Vector2Int coord)
        {
            EnsureLookup();
            if (!_lookup.Remove(coord, out int index))
            {
                return;
            }

            // Swap-remove; the moved cell gets the freed index
            int last = cells.Count - 1;
            if (index != last)
            {
                cells[index] = cells[last];
                _lookup[cells[index].coord] = index;
            }

            cells.RemoveAt(last);
        }

        public void ClearAllCells()
        {
            cells.Clear();
            _lookup = null;
        }

        /// <summary>Call after the cells changed behind the lookup's back (undo, asset reimport).</summary>
        public void RebuildLookup()
        {
            _lookup = null;
        }

        private void EnsureLookup()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new Dictionary<Vector2Int, int>(cells.Count);
            for (int i = 0; i < cells.Count; i++)
            {
                _lookup[cells[i].coord] = i;
            }
        }

        private void OnEnable()
        {
            _lookup = null;
        }

        private void OnValidate()
        {
            _lookup = null;
        }
    }
}
