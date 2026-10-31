using System.Collections.Generic;
using UnityEngine;

namespace Eco.Scripts.World
{
    /// <summary>Circle reservations for any placeable object. Streaming must not release them.</summary>
    public sealed class PlacementSpace
    {
        private const float CellSize = 8f;
        private readonly Dictionary<Vector2Int, Dictionary<int, (Vector2 position, float radius)>> _cells = new();
        private readonly Dictionary<int, Vector2Int> _handles = new();
        private float _maxRadius;
        private int _nextHandle;

        public int Reserve(Vector3 position, float radius)
        {
            radius = Mathf.Max(0f, radius);
            var point = new Vector2(position.x, position.z);
            var cell = Cell(point);
            if (!_cells.TryGetValue(cell, out var entries))
            {
                entries = new Dictionary<int, (Vector2, float)>();
                _cells.Add(cell, entries);
            }
            int handle = ++_nextHandle;
            entries.Add(handle, (point, radius));
            _handles.Add(handle, cell);
            _maxRadius = Mathf.Max(_maxRadius, radius);
            return handle;
        }

        public void Release(int handle)
        {
            if (!_handles.Remove(handle, out var cell)) return;
            _cells[cell].Remove(handle);
            if (_cells[cell].Count == 0) _cells.Remove(cell);
        }

        public bool CanPlace(Vector3 position, float radius)
        {
            var point = new Vector2(position.x, position.z);
            float reach = Mathf.Max(0f, radius) + _maxRadius;
            var min = Cell(point - Vector2.one * reach);
            var max = Cell(point + Vector2.one * reach);
            for (int z = min.y; z <= max.y; z++)
            {
                for (int x = min.x; x <= max.x; x++)
                {
                    if (!_cells.TryGetValue(new Vector2Int(x, z), out var entries)) continue;
                    foreach (var entry in entries.Values)
                    {
                        float separation = radius + entry.radius;
                        if ((point - entry.position).sqrMagnitude < separation * separation) return false;
                    }
                }
            }
            return true;
        }

        private static Vector2Int Cell(Vector2 point) =>
            new(Mathf.FloorToInt(point.x / CellSize), Mathf.FloorToInt(point.y / CellSize));
    }
}
