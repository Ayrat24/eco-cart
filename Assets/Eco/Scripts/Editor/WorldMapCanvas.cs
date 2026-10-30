using System;
using System.Collections.Generic;
using Eco.Scripts.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.EditorTools
{
    /// <summary>
    /// The hex map of the World Editor, drawn top-down with +Z up. Left-drag paints the selected chunk, right-drag
    /// erases back to ocean, Ctrl+click picks the chunk under the cursor. Middle-drag or Alt-drag pans, the wheel
    /// zooms, F frames the map.
    /// </summary>
    internal class WorldMapCanvas : VisualElement
    {
        private const float MinZoom = 0.25f;
        private const float MaxZoom = 40f;
        private const float ZoomStep = 1.15f;

        // Hexes are drawn this much smaller than they are, leaving a grid line between them
        private const float HexInset = 0.92f;

        // UI Toolkit meshes hold at most 65535 vertices; 7 per hex
        private const int HexesPerMesh = 9000;

        private static readonly Color Background = new(0.13f, 0.13f, 0.13f);
        private static readonly Color DefaultOcean = new(0.15f, 0.3f, 0.5f);
        private static readonly Color BrushOutline = new(1f, 1f, 1f, 0.9f);
        private static readonly Color SpawnMarker = new(1f, 0.85f, 0.2f);

        private enum Mode
        {
            None,
            Pan,
            Paint,
            Erase
        }

        private readonly VisualElement _content;
        private readonly VisualElement _hexLayer;
        private readonly VisualElement _overlay;
        private readonly List<Vector2Int> _mapHexes = new();
        private readonly List<Vector2Int> _brushHexes = new();

        private WorldData _world;
        private int _mapHexesRadius = -1;
        private Vector2 _pan;
        private float _zoom = 2f;
        private bool _framed;

        private Mode _mode;
        private int _pointerId = -1;
        private Vector2 _panStart;
        private Vector2 _pointerStart;
        private int _undoGroup;
        private Vector2Int _hover;
        private Vector2 _hoverPoint;
        private bool _hasHover;
        private Vector2Int _lastPainted;

        /// <summary>Chunk painted with the left mouse button.</summary>
        public ChunkDefinition Brush { get; set; }

        /// <summary>Radius of the round brush in hexes; 0 paints the hex under the cursor.</summary>
        public int BrushSize { get; set; }

        /// <summary>Hexes were painted or erased.</summary>
        public event Action Painted;

        /// <summary>Ctrl+click picked the chunk under the cursor (null for ocean).</summary>
        public event Action<ChunkDefinition> Picked;

        /// <summary>The hex under the cursor changed (null when the cursor left the map).</summary>
        public event Action<Vector2Int?> HoverChanged;

        public WorldMapCanvas()
        {
            focusable = true;
            style.flexGrow = 1;
            style.overflow = Overflow.Hidden;
            style.backgroundColor = Background;

            // Drawn in world metres (X right, Z up); the content's transform pans and zooms
            _content = new VisualElement { pickingMode = PickingMode.Ignore };
            _content.style.position = Position.Absolute;
            _content.style.left = 0;
            _content.style.top = 0;
            _content.style.transformOrigin = new TransformOrigin(0, 0);
            Add(_content);

            _hexLayer = new VisualElement { pickingMode = PickingMode.Ignore };
            _hexLayer.generateVisualContent += DrawHexes;
            _content.Add(_hexLayer);

            _overlay = new VisualElement { pickingMode = PickingMode.Ignore };
            _overlay.generateVisualContent += DrawOverlay;
            _content.Add(_overlay);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerLeaveEvent>(_ => SetHover(null));
            RegisterCallback<PointerCaptureOutEvent>(_ => EndInteraction());
            RegisterCallback<WheelEvent>(OnWheel);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (!_framed && _world != null && contentRect.width > 0)
                {
                    FrameAll();
                }

                ApplyTransform();
            });
        }

        public void SetWorld(WorldData world)
        {
            bool changed = world != _world;
            _world = world;
            Refresh();

            if (changed)
            {
                _framed = false;
                if (contentRect.width > 0)
                {
                    FrameAll();
                }
            }
        }

        /// <summary>Redraws the map, e.g. after the world or a chunk's colour changed.</summary>
        public void Refresh()
        {
            _hexLayer.MarkDirtyRepaint();
            _overlay.MarkDirtyRepaint();
        }

        public void FrameAll()
        {
            if (_world == null || contentRect.width <= 0)
            {
                return;
            }

            float extent = (_world.MapRadius + 1) * Hex.Spacing(_world.HexRadius);
            float size = Mathf.Min(contentRect.width, contentRect.height);
            _zoom = Mathf.Clamp(size / (extent * 2f), MinZoom, MaxZoom);
            _pan = Vector2.zero;
            _framed = true;
            ApplyTransform();
        }

        private void ApplyTransform()
        {
            var center = contentRect.size / 2f;
            _content.style.translate = new Translate(center.x + _pan.x, center.y + _pan.y);
            _content.style.scale = new Scale(new Vector3(_zoom, _zoom, 1f));
        }

        private Vector2 LocalToPlane(Vector2 local)
        {
            var p = (local - contentRect.size / 2f - _pan) / _zoom;
            return new Vector2(p.x, -p.y);
        }

        private static Vector2 PlaneToContent(Vector2 plane) => new(plane.x, -plane.y);

        private void DrawHexes(MeshGenerationContext context)
        {
            if (_world == null)
            {
                return;
            }

            if (_mapHexesRadius != _world.MapRadius)
            {
                _mapHexesRadius = _world.MapRadius;
                Hex.Range(Vector2Int.zero, _world.MapRadius, _mapHexes);
            }

            float radius = _world.HexRadius;
            var corners = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                corners[i] = PlaneToContent(Hex.Corner(i, radius * HexInset));
            }

            var ocean = _world.Ocean != null ? _world.Ocean.Color : DefaultOcean;
            ocean = Color.Lerp(ocean, Background, 0.45f);

            for (int start = 0; start < _mapHexes.Count; start += HexesPerMesh)
            {
                int count = Mathf.Min(HexesPerMesh, _mapHexes.Count - start);
                var mesh = context.Allocate(count * 7, count * 18);
                for (int h = 0; h < count; h++)
                {
                    var coord = _mapHexes[start + h];
                    var chunk = _world.GetPainted(coord);
                    var color = chunk != null ? chunk.Color : ocean;
                    color.a = 1f;
                    var center = PlaneToContent(Hex.ToPlane(coord, radius));

                    int first = h * 7;
                    mesh.SetNextVertex(new Vertex { position = center, tint = color });
                    for (int i = 0; i < 6; i++)
                    {
                        mesh.SetNextVertex(new Vertex { position = center + corners[i], tint = color });
                    }

                    // Clockwise on screen (Y down)
                    for (int i = 0; i < 6; i++)
                    {
                        mesh.SetNextIndex((ushort)first);
                        mesh.SetNextIndex((ushort)(first + 1 + (i + 1) % 6));
                        mesh.SetNextIndex((ushort)(first + 1 + i));
                    }
                }
            }
        }

        private void DrawOverlay(MeshGenerationContext context)
        {
            if (_world == null)
            {
                return;
            }

            var painter = context.painter2D;
            float radius = _world.HexRadius;

            // Where a new game starts
            var spawn = PlaneToContent(Hex.ToPlane(_world.GetSpawnCoord(), radius));
            painter.fillColor = SpawnMarker;
            painter.BeginPath();
            painter.Arc(spawn, radius * 0.3f, 0f, 360f);
            painter.Fill();

            if (!_hasHover)
            {
                return;
            }

            painter.strokeColor = BrushOutline;
            painter.lineWidth = Mathf.Max(0.15f, 1.5f / _zoom);
            if (BrushSize > 0)
            {
                painter.BeginPath();
                painter.Arc(PlaneToContent(_hoverPoint), BrushRadius(), 0f, 360f);
                painter.Stroke();
            }

            CollectBrush(_hoverPoint);
            foreach (var coord in _brushHexes)
            {
                var center = PlaneToContent(Hex.ToPlane(coord, radius));
                painter.BeginPath();
                painter.MoveTo(center + PlaneToContent(Hex.Corner(0, radius)));
                for (int i = 1; i < 6; i++)
                {
                    painter.LineTo(center + PlaneToContent(Hex.Corner(i, radius)));
                }

                painter.ClosePath();
                painter.Stroke();
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (_world == null || _mode != Mode.None)
            {
                return;
            }

            Focus();
            var local = (Vector2)evt.localPosition;
            bool pan = evt.button == 2 || (evt.button == 0 && evt.altKey);

            if (pan)
            {
                _mode = Mode.Pan;
                _panStart = _pan;
                _pointerStart = local;
            }
            else if (evt.button == 0 && evt.actionKey)
            {
                var coord = Hex.FromPlane(LocalToPlane(local), _world.HexRadius);
                Picked?.Invoke(_world.InMap(coord) ? _world.GetPainted(coord) : null);
                evt.StopPropagation();
                return;
            }
            else if (evt.button == 0 || evt.button == 1)
            {
                _mode = evt.button == 0 ? Mode.Paint : Mode.Erase;
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName(_mode == Mode.Paint ? "Paint World" : "Erase World");
                _undoGroup = Undo.GetCurrentGroup();
                _lastPainted = new Vector2Int(int.MinValue, int.MinValue);
                PaintAt(local);
            }
            else
            {
                return;
            }

            _pointerId = evt.pointerId;
            this.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (_world == null)
            {
                return;
            }

            var local = (Vector2)evt.localPosition;
            _hoverPoint = LocalToPlane(local);
            var coord = Hex.FromPlane(_hoverPoint, _world.HexRadius);
            SetHover(_world.InMap(coord) ? coord : null);

            // The round brush moves smoothly, not hex by hex
            if (BrushSize > 0)
            {
                _overlay.MarkDirtyRepaint();
            }

            if (evt.pointerId != _pointerId)
            {
                return;
            }

            switch (_mode)
            {
                case Mode.Pan:
                    _pan = _panStart + (local - _pointerStart);
                    ApplyTransform();
                    break;
                case Mode.Paint:
                case Mode.Erase:
                    PaintAt(local);
                    break;
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _pointerId)
            {
                return;
            }

            this.ReleasePointer(evt.pointerId);
            EndInteraction();
        }

        private void EndInteraction()
        {
            if (_mode is Mode.Paint or Mode.Erase)
            {
                Undo.CollapseUndoOperations(_undoGroup);
            }

            _mode = Mode.None;
            _pointerId = -1;
        }

        private void PaintAt(Vector2 local)
        {
            var center = Hex.FromPlane(LocalToPlane(local), _world.HexRadius);
            if (center == _lastPainted)
            {
                return;
            }

            _lastPainted = center;
            var chunk = _mode == Mode.Paint ? Brush : null;
            if (_mode == Mode.Paint && chunk == null)
            {
                return;
            }

            bool recorded = false;
            CollectBrush(LocalToPlane(local));
            foreach (var coord in _brushHexes)
            {
                if (_world.GetPainted(coord) == chunk)
                {
                    continue;
                }

                if (!recorded)
                {
                    Undo.RecordObject(_world, _mode == Mode.Paint ? "Paint World" : "Erase World");
                    recorded = true;
                }

                _world.SetCell(coord, chunk);
            }

            if (recorded)
            {
                EditorUtility.SetDirty(_world);
                Refresh();
                Painted?.Invoke();
            }
        }

        private float BrushRadius() => (BrushSize + 0.5f) * Hex.Spacing(_world.HexRadius);

        // The hexes in the map whose centres are inside the round brush at a point, or the hex under it at size 0
        private void CollectBrush(Vector2 point)
        {
            var center = Hex.FromPlane(point, _world.HexRadius);
            Hex.Range(center, BrushSize + 1, _brushHexes);
            float radius = BrushRadius();
            _brushHexes.RemoveAll(coord => !_world.InMap(coord) || (BrushSize > 0 &&
                (Hex.ToPlane(coord, _world.HexRadius) - point).sqrMagnitude > radius * radius) ||
                (BrushSize == 0 && coord != center));
        }

        private void SetHover(Vector2Int? coord)
        {
            bool has = coord.HasValue;
            if (has == _hasHover && (!has || coord.Value == _hover))
            {
                return;
            }

            _hasHover = has;
            if (has)
            {
                _hover = coord.Value;
            }

            _overlay.MarkDirtyRepaint();
            HoverChanged?.Invoke(coord);
        }

        private void OnWheel(WheelEvent evt)
        {
            var local = (Vector2)evt.localMousePosition;
            var before = LocalToPlane(local);
            _zoom = Mathf.Clamp(_zoom * (evt.delta.y < 0 ? ZoomStep : 1f / ZoomStep), MinZoom, MaxZoom);

            // Keep the point under the cursor where it is
            var after = PlaneToContent(before) * _zoom + contentRect.size / 2f + _pan;
            _pan += local - after;
            ApplyTransform();
            _overlay.MarkDirtyRepaint();
            evt.StopPropagation();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.F)
            {
                FrameAll();
                evt.StopPropagation();
            }
        }
    }
}
