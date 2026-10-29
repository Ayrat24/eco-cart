using System.Collections.Generic;
using UnityEngine;

namespace Eco.Scripts.World
{
    /// <summary>
    /// The tiles of one hex chunk: the 1 m cells of a square grid centred on the hex whose centres lie inside it.
    /// Every chunk of a given hex radius has the same layout, so tile indices (and saved <see cref="TileData"/>
    /// arrays) line up between chunks. The grid is <see cref="Size"/>² cells; the map draws it as a texture.
    /// </summary>
    public class HexTileLayout
    {
        // Tiles stay this far inside the hex edge so objects on them don't poke into the neighbour
        private const float EdgeMargin = 0.3f;

        private static readonly Dictionary<float, HexTileLayout> Cache = new();

        private readonly Vector2[] _localPositions;
        private readonly Vector2Int[] _cells;
        private readonly int[] _indexByCell;

        public float Radius { get; }

        /// <summary>Width and height of the tile grid in cells (and metres).</summary>
        public int Size { get; }

        public int Count => _localPositions.Length;

        /// <summary>Tile closest to the hex centre.</summary>
        public int CenterIndex { get; }

        private HexTileLayout(float radius)
        {
            Radius = radius;
            int half = Mathf.CeilToInt(radius);
            Size = half * 2;

            var positions = new List<Vector2>();
            var cells = new List<Vector2Int>();
            _indexByCell = new int[Size * Size];

            for (int z = 0; z < Size; z++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var local = new Vector2(x - half + 0.5f, z - half + 0.5f);
                    if (Hex.SignedDistance(local, radius) > -EdgeMargin)
                    {
                        _indexByCell[z * Size + x] = -1;
                        continue;
                    }

                    _indexByCell[z * Size + x] = positions.Count;
                    positions.Add(local);
                    cells.Add(new Vector2Int(x, z));
                }
            }

            _localPositions = positions.ToArray();
            _cells = cells.ToArray();
            CenterIndex = IndexAt(Vector2.zero);
        }

        public static HexTileLayout Get(float radius)
        {
            if (!Cache.TryGetValue(radius, out var layout))
            {
                layout = new HexTileLayout(radius);
                Cache[radius] = layout;
            }

            return layout;
        }

        /// <summary>Tile position relative to the chunk centre, on the XZ plane.</summary>
        public Vector2 LocalPosition(int index) => _localPositions[index];

        /// <summary>Grid cell of a tile; (0, 0) is the cell at the chunk's -X/-Z corner.</summary>
        public Vector2Int Cell(int index) => _cells[index];

        /// <summary>Tile in the grid cell, or -1 when the cell is outside the hex.</summary>
        public int IndexAtCell(int x, int z)
        {
            if (x < 0 || z < 0 || x >= Size || z >= Size)
            {
                return -1;
            }

            return _indexByCell[z * Size + x];
        }

        /// <summary>Tile under a position relative to the chunk centre, or -1 outside the tiles.</summary>
        public int IndexAt(Vector2 local)
        {
            int half = Size / 2;
            return IndexAtCell(Mathf.FloorToInt(local.x + half), Mathf.FloorToInt(local.y + half));
        }
    }
}
