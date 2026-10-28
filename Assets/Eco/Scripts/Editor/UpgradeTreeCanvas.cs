using System;
using System.Collections.Generic;
using Eco.Scripts.Upgrades;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.EditorTools
{
    /// <summary>
    /// The graph area of the upgrade tree editor. Coordinates are the game menu's: a node drawn at (x, y) here
    /// sits at (x, y) in the menu at zoom 1.
    /// <para>
    /// Left-drag a node to move it (snaps to the grid unless Ctrl is held), drag from the dot under a node onto
    /// another node to make it a requirement, drag on empty space to box-select. Middle-drag or Alt-drag pans,
    /// the wheel zooms. Delete removes the selection, Ctrl+D duplicates, F frames. Right-click for more.
    /// </para>
    /// </summary>
    internal class UpgradeTreeCanvas : VisualElement
    {
        public const float GridSize = 20f;

        private const float MinZoom = 0.15f;
        private const float MaxZoom = 2f;
        private const float ZoomStep = 1.1f;
        private const float DragThreshold = 4f;
        private const float EdgePickDistance = 8f;
        private const float ChildSpacing = 220f;

        private static readonly Color Background = new(0.16f, 0.16f, 0.16f);
        private static readonly Color MinorGrid = new(1f, 1f, 1f, 0.04f);
        private static readonly Color MajorGrid = new(1f, 1f, 1f, 0.09f);
        private static readonly Color Axis = new(0.65f, 0.79f, 0.34f, 0.35f);

        private enum Mode
        {
            None,
            Pan,
            Move,
            Connect,
            BoxSelect
        }

        private readonly VisualElement _content;
        private readonly UpgradeTreeEditorEdges _edges;
        private readonly VisualElement _selectionBox;
        private readonly Dictionary<UpgradeNode, UpgradeTreeNodeElement> _elements = new();
        private readonly List<UpgradeNode> _selection = new();
        private readonly Dictionary<UpgradeNode, Vector2> _moveStart = new();

        private UpgradeTree _tree;
        private Dictionary<UpgradeNode, string> _problems = new();
        private Vector2 _pan;
        private float _zoom = 1f;

        private Mode _mode;
        private int _pointerId = -1;
        private Vector2 _pointerStart;
        private Vector2 _panStart;
        private bool _dragged;
        private UpgradeNode _connectFrom;
        private UpgradeNode _pressedNode;
        private bool _pressedWasSelected;

        public bool Snap { get; set; } = true;
        public IReadOnlyList<UpgradeNode> Selection => _selection;
        public (UpgradeNode parent, UpgradeNode child)? SelectedEdge => _edges.Selected;
        public IReadOnlyDictionary<UpgradeNode, string> Problems => _problems;

        /// <summary>Node or edge selection changed.</summary>
        public event Action SelectionChanged;

        /// <summary>The canvas edited the tree (moved, added, connected or deleted something).</summary>
        public event Action TreeEdited;

        public UpgradeTreeCanvas()
        {
            focusable = true;
            style.flexGrow = 1;
            style.overflow = Overflow.Hidden;
            style.backgroundColor = Background;
            generateVisualContent += DrawGrid;

            _content = new VisualElement { pickingMode = PickingMode.Ignore };
            _content.style.position = Position.Absolute;
            _content.style.left = 0;
            _content.style.top = 0;
            _content.style.transformOrigin = new TransformOrigin(0, 0);
            Add(_content);

            _edges = new UpgradeTreeEditorEdges();
            _content.Add(_edges);

            _selectionBox = new VisualElement { pickingMode = PickingMode.Ignore };
            _selectionBox.style.position = Position.Absolute;
            _selectionBox.style.backgroundColor = new Color(0.25f, 0.6f, 1f, 0.12f);
            SetBorder(_selectionBox, 1f, new Color(0.25f, 0.6f, 1f, 0.8f));
            _selectionBox.style.display = DisplayStyle.None;
            Add(_selectionBox);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(_ => CancelInteraction());
            RegisterCallback<WheelEvent>(OnWheel);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            this.AddManipulator(new ContextualMenuManipulator(OnContextMenu));
        }

        public void SetTree(UpgradeTree tree)
        {
            bool changed = tree != _tree;
            _tree = tree;
            _selection.Clear();
            _edges.Selected = null;
            Rebuild();

            if (changed)
            {
                schedule.Execute(() => FrameAll());
            }
        }

        /// <summary>Recreates the node elements, e.g. after nodes were added, removed or undone.</summary>
        public void Rebuild()
        {
            foreach (var element in _elements.Values)
            {
                element.RemoveFromHierarchy();
            }

            _elements.Clear();
            _edges.SetTree(_tree);

            if (_tree == null)
            {
                return;
            }

            // Drop selection entries whose node no longer exists
            _selection.RemoveAll(node => !_tree.EditorNodes.Contains(node));
            if (_edges.Selected.HasValue && (!_tree.EditorNodes.Contains(_edges.Selected.Value.parent) ||
                                             !_tree.EditorNodes.Contains(_edges.Selected.Value.child)))
            {
                _edges.Selected = null;
            }

            foreach (var node in _tree.Nodes)
            {
                var element = new UpgradeTreeNodeElement(node);
                _elements[node] = element;
                _content.Add(element);
            }

            Refresh();
        }

        /// <summary>Redraws nodes and lines from the current data.</summary>
        public void Refresh()
        {
            if (_tree == null)
            {
                return;
            }

            _problems = UpgradeTreeValidator.Validate(_tree);
            foreach (var (node, element) in _elements)
            {
                element.Refresh(_selection.Contains(node), _problems.GetValueOrDefault(node), IsRoot(node));
            }

            _edges.MarkDirtyRepaint();
        }

        public void Select(UpgradeNode node)
        {
            _selection.Clear();
            if (node != null)
            {
                _selection.Add(node);
            }

            _edges.Selected = null;
            OnSelectionChanged();
        }

        public void FrameAll()
        {
            if (_tree == null || _tree.Nodes.Count == 0)
            {
                _zoom = 1f;
                _pan = layout.size / 2f;
                ApplyTransform();
                return;
            }

            var nodes = _selection.Count > 0 ? (IEnumerable<UpgradeNode>)_selection : _tree.Nodes;
            var min = Vector2.positiveInfinity;
            var max = Vector2.negativeInfinity;
            foreach (var node in nodes)
            {
                min = Vector2.Min(min, node.Position);
                max = Vector2.Max(max, node.Position);
            }

            // Room for the circles and the summary cards under them
            min -= new Vector2(UpgradeTree.NodeSize, UpgradeTree.NodeSize);
            max += new Vector2(UpgradeTree.NodeSize, UpgradeTree.NodeSize * 2f);

            var size = layout.size;
            if (float.IsNaN(size.x) || size.x <= 0f)
            {
                return;
            }

            var extent = max - min;
            _zoom = Mathf.Clamp(Mathf.Min(size.x / extent.x, size.y / extent.y), MinZoom, 1f);
            _pan = size / 2f - (min + max) / 2f * _zoom;
            ApplyTransform();
        }

        /// <summary>Adds a node at <paramref name="treePosition"/>, optionally with one effect.</summary>
        public UpgradeNode AddNode(Vector2 treePosition, Type effectType, UpgradeNode parent = null)
        {
            if (_tree == null)
            {
                return null;
            }

            Undo.RecordObject(_tree, "Add Upgrade Node");
            string prefix = effectType != null ? IdPrefix(effectType) : "upgrade";
            var node = new UpgradeNode(_tree.MakeUniqueId(prefix), SnapPosition(treePosition, false));
            if (effectType != null)
            {
                node.EditorEffects.Add((UpgradeEffect)Activator.CreateInstance(effectType));
            }

            if (parent != null)
            {
                node.EditorRequires.Add(parent.Id);
            }

            _tree.EditorNodes.Add(node);
            EditorUtility.SetDirty(_tree);

            Rebuild();
            Select(node);
            TreeEdited?.Invoke();
            return node;
        }

        public void DeleteSelection()
        {
            if (_tree == null)
            {
                return;
            }

            if (_selection.Count == 0 && _edges.Selected.HasValue)
            {
                var (parent, child) = _edges.Selected.Value;
                Undo.RecordObject(_tree, "Remove Upgrade Requirement");
                child.EditorRequires.Remove(parent.Id);
                _edges.Selected = null;
                Edited();
                OnSelectionChanged();
                return;
            }

            if (_selection.Count == 0)
            {
                return;
            }

            Undo.RecordObject(_tree, "Delete Upgrade Nodes");
            foreach (var node in _selection)
            {
                _tree.EditorNodes.Remove(node);
                foreach (var other in _tree.EditorNodes)
                {
                    other.EditorRequires.Remove(node.Id);
                }
            }

            _selection.Clear();
            EditorUtility.SetDirty(_tree);
            Rebuild();
            OnSelectionChanged();
            TreeEdited?.Invoke();
        }

        public void DuplicateSelection()
        {
            if (_tree == null || _selection.Count == 0)
            {
                return;
            }

            Undo.RecordObject(_tree, "Duplicate Upgrade Nodes");
            var copies = new List<UpgradeNode>();
            foreach (var node in _selection)
            {
                copies.Add(UpgradeNodeCopier.Copy(_tree, node, node.Position + new Vector2(GridSize * 3, GridSize * 3)));
            }

            _tree.EditorNodes.AddRange(copies);
            EditorUtility.SetDirty(_tree);
            Rebuild();

            _selection.Clear();
            _selection.AddRange(copies);
            OnSelectionChanged();
            TreeEdited?.Invoke();
        }

        /// <summary>Makes <paramref name="parent"/> a requirement of <paramref name="child"/>, unless it would loop.</summary>
        public bool Connect(UpgradeNode parent, UpgradeNode child)
        {
            if (parent == null || child == null || child.EditorRequires.Contains(parent.Id) ||
                _tree.WouldCreateCycle(parent, child))
            {
                return false;
            }

            Undo.RecordObject(_tree, "Add Upgrade Requirement");
            child.EditorRequires.Add(parent.Id);
            Edited();
            return true;
        }

        private void Edited()
        {
            EditorUtility.SetDirty(_tree);
            Refresh();
            TreeEdited?.Invoke();
        }

        private void OnSelectionChanged()
        {
            Refresh();
            SelectionChanged?.Invoke();
        }

        private bool IsRoot(UpgradeNode node)
        {
            foreach (var id in node.Requires)
            {
                if (_tree.Find(id) != null)
                {
                    return false;
                }
            }

            return true;
        }

        private Vector2 ToTree(Vector2 canvasPosition)
        {
            return (canvasPosition - _pan) / _zoom;
        }

        private Vector2 ToCanvas(Vector2 treePosition)
        {
            return treePosition * _zoom + _pan;
        }

        private Vector2 SnapPosition(Vector2 position, bool invert)
        {
            if (Snap == invert)
            {
                return position;
            }

            return new Vector2(Mathf.Round(position.x / GridSize) * GridSize, Mathf.Round(position.y / GridSize) * GridSize);
        }

        private void ApplyTransform()
        {
            _content.style.translate = new Translate(_pan.x, _pan.y);
            _content.style.scale = new Scale(new Vector3(_zoom, _zoom, 1f));
            MarkDirtyRepaint();
        }

        // Picking

        private UpgradeNode PickNode(Vector2 treePosition)
        {
            if (_tree == null)
            {
                return null;
            }

            float radius = UpgradeTree.NodeSize / 2f;
            // Last drawn is on top
            for (int i = _tree.Nodes.Count - 1; i >= 0; i--)
            {
                var node = _tree.Nodes[i];
                if ((node.Position - treePosition).sqrMagnitude <= radius * radius)
                {
                    return node;
                }
            }

            return null;
        }

        private UpgradeNode PickPort(Vector2 treePosition)
        {
            if (_tree == null)
            {
                return null;
            }

            float radius = UpgradeTreeNodeElement.PortRadius + 3f;
            foreach (var (node, element) in _elements)
            {
                if ((element.PortPosition - treePosition).sqrMagnitude <= radius * radius)
                {
                    return node;
                }
            }

            return null;
        }

        private (UpgradeNode parent, UpgradeNode child)? PickEdge(Vector2 treePosition)
        {
            if (_tree == null)
            {
                return null;
            }

            float maxDistance = EdgePickDistance / _zoom;
            foreach (var edge in UpgradeTreeEditorEdges.GetEdges(_tree))
            {
                if (DistanceToSegment(treePosition, edge.parent.Position, edge.child.Position) <= maxDistance)
                {
                    return edge;
                }
            }

            return null;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector2.Distance(point, a + ab * t);
        }

        // Input

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (_tree == null || _pointerId != -1)
            {
                return;
            }

            Focus();
            Vector2 local = evt.localPosition;
            var treePosition = ToTree(local);
            _pointerStart = local;
            _dragged = false;

            if (evt.button == 2 || (evt.button == 0 && evt.altKey))
            {
                _mode = Mode.Pan;
                _panStart = _pan;
            }
            else if (evt.button == 0)
            {
                var portNode = PickPort(treePosition);
                var node = portNode == null ? PickNode(treePosition) : null;
                if (portNode != null)
                {
                    _mode = Mode.Connect;
                    _connectFrom = portNode;
                }
                else if (node != null)
                {
                    PressNode(node, evt.shiftKey || evt.actionKey);
                }
                else
                {
                    var edge = PickEdge(treePosition);
                    if (edge.HasValue)
                    {
                        _selection.Clear();
                        _edges.Selected = edge;
                        OnSelectionChanged();
                        _mode = Mode.None;
                    }
                    else
                    {
                        if (!evt.shiftKey && !evt.actionKey && (_selection.Count > 0 || _edges.Selected.HasValue))
                        {
                            _selection.Clear();
                            _edges.Selected = null;
                            OnSelectionChanged();
                        }

                        _mode = Mode.BoxSelect;
                    }
                }
            }
            else
            {
                return;
            }

            if (_mode != Mode.None)
            {
                _pointerId = evt.pointerId;
                this.CapturePointer(evt.pointerId);
            }

            evt.StopPropagation();
        }

        private void PressNode(UpgradeNode node, bool additive)
        {
            _pressedNode = node;
            _pressedWasSelected = _selection.Contains(node);
            _edges.Selected = null;

            if (additive)
            {
                if (!_pressedWasSelected)
                {
                    _selection.Add(node);
                }
            }
            else if (!_pressedWasSelected)
            {
                _selection.Clear();
                _selection.Add(node);
            }

            OnSelectionChanged();
            _mode = Mode.Move;
            _moveStart.Clear();
            foreach (var selected in _selection)
            {
                _moveStart[selected] = selected.Position;
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != _pointerId)
            {
                return;
            }

            Vector2 local = evt.localPosition;
            var delta = local - _pointerStart;
            if (!_dragged && delta.magnitude < DragThreshold)
            {
                return;
            }

            switch (_mode)
            {
                case Mode.Pan:
                    _pan = _panStart + delta;
                    ApplyTransform();
                    break;

                case Mode.Move:
                    if (!_dragged)
                    {
                        Undo.RecordObject(_tree, "Move Upgrade Nodes");
                    }

                    foreach (var (node, start) in _moveStart)
                    {
                        node.EditorPosition = SnapPosition(start + delta / _zoom, evt.actionKey);
                    }

                    Refresh();
                    break;

                case Mode.Connect:
                    _edges.Pending = (_elements[_connectFrom].PortPosition, ToTree(local));
                    _edges.MarkDirtyRepaint();
                    break;

                case Mode.BoxSelect:
                    var min = Vector2.Min(_pointerStart, local);
                    var size = Vector2.Max(_pointerStart, local) - min;
                    _selectionBox.style.left = min.x;
                    _selectionBox.style.top = min.y;
                    _selectionBox.style.width = size.x;
                    _selectionBox.style.height = size.y;
                    _selectionBox.style.display = DisplayStyle.Flex;
                    break;
            }

            _dragged = true;
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _pointerId)
            {
                return;
            }

            Vector2 local = evt.localPosition;
            switch (_mode)
            {
                case Mode.Move when _dragged:
                    Edited();
                    break;

                case Mode.Move when _pressedWasSelected && (evt.shiftKey || evt.actionKey):
                    // A click on a selected node with Shift/Ctrl deselects it
                    _selection.Remove(_pressedNode);
                    OnSelectionChanged();
                    break;

                case Mode.Move when _pressedWasSelected && _selection.Count > 1:
                    // A plain click inside a multi-selection keeps just that node
                    Select(_pressedNode);
                    break;

                case Mode.Connect:
                    var target = PickNode(ToTree(local));
                    if (target != null && target != _connectFrom && !Connect(_connectFrom, target))
                    {
                        Debug.LogWarning($"Can't make '{_connectFrom.Id}' a requirement of '{target.Id}': " +
                                         "it already is, or it would make a loop.");
                    }

                    break;

                case Mode.BoxSelect when _dragged:
                    var a = ToTree(_pointerStart);
                    var b = ToTree(local);
                    var rect = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x),
                        Mathf.Max(a.y, b.y));
                    foreach (var node in _tree.Nodes)
                    {
                        if (rect.Contains(node.Position) && !_selection.Contains(node))
                        {
                            _selection.Add(node);
                        }
                    }

                    OnSelectionChanged();
                    break;
            }

            this.ReleasePointer(evt.pointerId);
            CancelInteraction();
            evt.StopPropagation();
        }

        private void CancelInteraction()
        {
            _pointerId = -1;
            _mode = Mode.None;
            _dragged = false;
            _connectFrom = null;
            _pressedNode = null;
            _edges.Pending = null;
            _edges.MarkDirtyRepaint();
            _selectionBox.style.display = DisplayStyle.None;
        }

        private void OnWheel(WheelEvent evt)
        {
            Vector2 pivot = evt.localMousePosition;
            float zoom = Mathf.Clamp(_zoom * Mathf.Pow(ZoomStep, -Mathf.Sign(evt.delta.y)), MinZoom, MaxZoom);
            var treePoint = ToTree(pivot);
            _zoom = zoom;
            _pan = pivot - treePoint * _zoom;
            ApplyTransform();
            evt.StopPropagation();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Delete:
                case KeyCode.Backspace:
                    DeleteSelection();
                    break;
                case KeyCode.D when evt.actionKey:
                    DuplicateSelection();
                    break;
                case KeyCode.A when evt.actionKey:
                    _selection.Clear();
                    _selection.AddRange(_tree.Nodes);
                    OnSelectionChanged();
                    break;
                case KeyCode.F:
                    FrameAll();
                    break;
                default:
                    return;
            }

            evt.StopPropagation();
        }

        private void OnContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (_tree == null)
            {
                return;
            }

            var treePosition = ToTree(evt.localMousePosition);
            var node = PickNode(treePosition);
            var edge = node == null ? PickEdge(treePosition) : null;

            if (node != null)
            {
                if (!_selection.Contains(node))
                {
                    Select(node);
                }

                foreach (var type in UpgradeEffectTypes.All)
                {
                    var effectType = type;
                    evt.menu.AppendAction($"Add Child Node/{UpgradeEffectTypes.GetMenuName(type)}",
                        _ => AddNode(node.Position + new Vector2(0f, ChildSpacing), effectType, node));
                }

                evt.menu.AppendAction("Add Child Node/Empty", _ => AddNode(node.Position + new Vector2(0f, ChildSpacing), null, node));
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Duplicate", _ => DuplicateSelection());
                evt.menu.AppendAction("Remove Requirements", _ =>
                {
                    Undo.RecordObject(_tree, "Remove Upgrade Requirements");
                    node.EditorRequires.Clear();
                    Edited();
                }, node.Requires.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction("Delete", _ => DeleteSelection());
            }
            else if (edge.HasValue)
            {
                evt.menu.AppendAction("Delete Requirement", _ =>
                {
                    _selection.Clear();
                    _edges.Selected = edge;
                    DeleteSelection();
                });
            }
            else
            {
                foreach (var type in UpgradeEffectTypes.All)
                {
                    var effectType = type;
                    evt.menu.AppendAction($"Add Node/{UpgradeEffectTypes.GetMenuName(type)}",
                        _ => AddNode(treePosition, effectType));
                }

                evt.menu.AppendAction("Add Node/Empty", _ => AddNode(treePosition, null));
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Frame All", _ =>
                {
                    Select(null);
                    FrameAll();
                });
            }

            evt.StopPropagation();
        }

        // Grid

        private void DrawGrid(MeshGenerationContext context)
        {
            var size = contentRect.size;
            var painter = context.painter2D;
            painter.lineWidth = 1f;

            float minor = GridSize * _zoom;
            if (minor >= 6f)
            {
                DrawLines(painter, size, minor, MinorGrid);
            }

            DrawLines(painter, size, GridSize * 5f * _zoom, MajorGrid);

            // The tree origin
            var origin = ToCanvas(Vector2.zero);
            painter.strokeColor = Axis;
            painter.BeginPath();
            painter.MoveTo(new Vector2(origin.x, 0f));
            painter.LineTo(new Vector2(origin.x, size.y));
            painter.MoveTo(new Vector2(0f, origin.y));
            painter.LineTo(new Vector2(size.x, origin.y));
            painter.Stroke();
        }

        private void DrawLines(Painter2D painter, Vector2 size, float step, Color color)
        {
            painter.strokeColor = color;
            painter.BeginPath();

            float startX = Mathf.Repeat(_pan.x, step);
            for (float x = startX; x < size.x; x += step)
            {
                painter.MoveTo(new Vector2(x, 0f));
                painter.LineTo(new Vector2(x, size.y));
            }

            float startY = Mathf.Repeat(_pan.y, step);
            for (float y = startY; y < size.y; y += step)
            {
                painter.MoveTo(new Vector2(0f, y));
                painter.LineTo(new Vector2(size.x, y));
            }

            painter.Stroke();
        }

        private static string IdPrefix(Type effectType)
        {
            string name = effectType.Name.EndsWith("Effect") ? effectType.Name[..^"Effect".Length] : effectType.Name;
            return ObjectNames.NicifyVariableName(name).ToLowerInvariant().Replace(' ', '-');
        }

        private static void SetBorder(VisualElement element, float width, Color color)
        {
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
        }
    }
}
