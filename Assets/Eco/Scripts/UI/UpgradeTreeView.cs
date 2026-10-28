using System.Collections.Generic;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.UI
{
    /// <summary>
    /// The upgrade tree canvas: drag to pan, wheel to zoom around the pointer, click a node to select it.
    /// Nodes sit at their <see cref="UpgradeNode.Position"/> in tree coordinates (pixels at zoom 1). On top, in
    /// screen space so they keep their size at any zoom: a <see cref="Tooltip"/> next to the hovered node (or the
    /// selected one when nothing is hovered) and − / + buttons beside the selected node.
    /// </summary>
    [UxmlElement]
    public partial class UpgradeTreeView : VisualElement
    {
        private const float MinZoom = 0.35f;
        private const float MaxZoom = 2f;
        private const float ZoomStep = 1.15f;
        private const float DragThreshold = 6f;
        // Focusing on a few nodes doesn't zoom out past this, so names stay readable
        private const float MinFocusZoom = 0.7f;
        // Room around the tree when framing, and how much of it must stay on screen when panning
        private const float FrameMargin = 60f;
        private const float KeepVisible = 120f;
        // Node captions hang below the circles
        private const float CaptionHeight = 70f;
        private const float ActionSize = 48f;
        private const float ActionGap = 6f;
        private const float TooltipGap = 12f;
        private const float EdgePadding = 8f;

        private readonly VisualElement _content;
        private readonly UpgradeEdgeLayer _edges;
        private readonly VisualElement _overlay;
        private readonly Button _refundButton;
        private readonly Button _buyButton;
        private readonly Dictionary<UpgradeNode, UpgradeNodeView> _views = new();

        private UpgradeManager _manager;
        private Rect _bounds;
        private Vector2 _pan;
        private float _zoom = 1f;
        private bool _focusPending;
        private List<UpgradeNode> _focusTargets;

        private int _pointerId = -1;
        private Vector2 _pointerStart;
        private Vector2 _panStart;
        private bool _dragging;
        private UpgradeNodeView _hovered;
        private UpgradeNodeView _selected;
        private UpgradeNode _infoNode;

        public readonly Subject<UpgradeNode> OnNodeClicked = new();
        /// <summary>The node the tooltip describes changed (null hides it).</summary>
        public readonly Subject<UpgradeNode> OnInfoNodeChanged = new();
        public readonly Subject<UpgradeNode> OnBuyClicked = new();
        public readonly Subject<UpgradeNode> OnRefundClicked = new();

        public UpgradeTooltip Tooltip { get; }
        public UpgradeNode InfoNode => _infoNode;

        public UpgradeTreeView()
        {
            AddToClassList("upgrade-tree-view");
            style.overflow = Overflow.Hidden;

            _content = new VisualElement { name = "TreeContent", pickingMode = PickingMode.Ignore };
            _content.AddToClassList("upgrade-tree-view__content");
            _content.style.position = Position.Absolute;
            _content.style.left = 0;
            _content.style.top = 0;
            _content.style.transformOrigin = new TransformOrigin(0, 0);
            Add(_content);

            _edges = new UpgradeEdgeLayer();
            _content.Add(_edges);

            _overlay = new VisualElement { name = "TreeOverlay", pickingMode = PickingMode.Ignore };
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.top = 0;
            _overlay.style.right = 0;
            _overlay.style.bottom = 0;
            Add(_overlay);

            _refundButton = CreateAction("−", "upgrade-action--refund", () => OnRefundClicked.OnNext(_selected?.Node));
            _buyButton = CreateAction("+", "upgrade-action--buy", () => OnBuyClicked.OnNext(_selected?.Node));

            Tooltip = new UpgradeTooltip();
            Tooltip.style.display = DisplayStyle.None;
            Tooltip.RegisterCallback<GeometryChangedEvent>(_ => PlaceOverlay());
            _overlay.Add(Tooltip);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(_ => EndDrag());
            RegisterCallback<PointerLeaveEvent>(_ => SetHovered(null));
            RegisterCallback<WheelEvent>(OnWheel);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        public void Build(UpgradeManager manager)
        {
            _manager = manager;

            foreach (var view in _views.Values)
            {
                view.RemoveFromHierarchy();
            }

            _views.Clear();
            _hovered = null;
            _selected = null;

            foreach (var node in manager.Tree.Nodes)
            {
                var view = new UpgradeNodeView(node);
                _views[node] = view;
                _content.Add(view);
            }

            var bounds = manager.Tree.GetBounds();
            bounds.yMax += CaptionHeight;
            _bounds = bounds;
            _edges.SetBounds(bounds);

            Refresh();
            UpdateInfoNode();
        }

        /// <summary>Updates every node and line from the manager's current levels and money.</summary>
        public void Refresh()
        {
            if (_manager == null)
            {
                return;
            }

            foreach (var (node, view) in _views)
            {
                var state = _manager.GetState(node);
                view.Refresh(state, _manager.GetLevel(node), _manager.CanAfford(node), _manager.GetCost(node).ToString());
            }

            _edges.ClearEdges();
            foreach (var node in _manager.Tree.Nodes)
            {
                bool childOwned = _manager.GetLevel(node) > 0;
                foreach (var id in node.Requires)
                {
                    var parent = _manager.Tree.Find(id);
                    if (parent == null)
                    {
                        continue;
                    }

                    var state = _manager.GetLevel(parent) == 0 ? UpgradeEdgeLayer.EdgeState.Locked
                        : childOwned ? UpgradeEdgeLayer.EdgeState.Owned
                        : UpgradeEdgeLayer.EdgeState.Open;
                    _edges.AddEdge(parent.Position, node.Position, state);
                }
            }

            _edges.Repaint();

            if (_selected != null)
            {
                var node = _selected.Node;
                _buyButton.SetEnabled(_manager.CanBuy(node));
                _refundButton.SetEnabled(_manager.CanRefund(node));
            }
        }

        public void SetNodeName(UpgradeNode node, string text)
        {
            if (_views.TryGetValue(node, out var view))
            {
                view.SetName(text);
            }
        }

        public void SetSelected(UpgradeNode node)
        {
            _selected?.SetSelected(false);
            _selected = node != null && _views.TryGetValue(node, out var view) ? view : null;
            _selected?.SetSelected(true);

            var display = _selected != null ? DisplayStyle.Flex : DisplayStyle.None;
            _buyButton.style.display = display;
            _refundButton.style.display = display;

            Refresh();
            UpdateInfoNode();
            PlaceOverlay();
        }

        public void PlayBought(UpgradeNode node)
        {
            if (_views.TryGetValue(node, out var view))
            {
                view.PlayBought();
            }
        }

        /// <summary>Zooms and centers so the whole tree fits, no closer than zoom 1.</summary>
        public void FrameAll()
        {
            Focus(null);
        }

        /// <summary>
        /// Centers on <paramref name="nodes"/> (all of the tree if null or empty), zoomed to fit them but no
        /// further out than readable, unless that's the whole tree.
        /// </summary>
        public void Focus(IReadOnlyCollection<UpgradeNode> nodes)
        {
            _focusTargets = nodes != null && nodes.Count > 0 ? new List<UpgradeNode>(nodes) : null;

            var size = layout.size;
            if (float.IsNaN(size.x) || size.x <= 0f || size.y <= 0f)
            {
                _focusPending = true;
                return;
            }

            _focusPending = false;
            var target = _focusTargets == null ? _bounds : GetBounds(_focusTargets);
            var framed = new Rect(target.x - FrameMargin, target.y - FrameMargin,
                target.width + FrameMargin * 2f, target.height + FrameMargin * 2f);
            float minZoom = _focusTargets == null ? MinZoom : MinFocusZoom;
            _zoom = Mathf.Clamp(Mathf.Min(size.x / framed.width, size.y / framed.height), minZoom, 1f);
            _pan = size / 2f - framed.center * _zoom;
            ApplyTransform();
        }

        /// <summary>Zooms by <paramref name="steps"/> wheel notches around the view's center.</summary>
        public void ZoomBy(float steps)
        {
            ZoomAround(layout.size / 2f, steps);
        }

        private static Rect GetBounds(List<UpgradeNode> nodes)
        {
            var min = nodes[0].Position;
            var max = nodes[0].Position;
            foreach (var node in nodes)
            {
                min = Vector2.Min(min, node.Position);
                max = Vector2.Max(max, node.Position);
            }

            float half = UpgradeTree.NodeSize / 2f;
            return Rect.MinMaxRect(min.x - half, min.y - half, max.x + half, max.y + half + CaptionHeight);
        }

        private void ZoomAround(Vector2 pivot, float steps)
        {
            float zoom = Mathf.Clamp(_zoom * Mathf.Pow(ZoomStep, steps), MinZoom, MaxZoom);
            if (Mathf.Approximately(zoom, _zoom))
            {
                return;
            }

            // Keep the tree point under the pivot where it is
            var treePoint = (pivot - _pan) / _zoom;
            _zoom = zoom;
            _pan = pivot - treePoint * _zoom;
            ApplyTransform();
        }

        private void ApplyTransform()
        {
            ClampPan();
            _content.style.translate = new Translate(_pan.x, _pan.y);
            _content.style.scale = new Scale(new Vector3(_zoom, _zoom, 1f));
            PlaceOverlay();
        }

        // Some of the tree always stays in view
        private void ClampPan()
        {
            var size = layout.size;
            if (float.IsNaN(size.x))
            {
                return;
            }

            var min = _bounds.min * _zoom + _pan;
            var max = _bounds.max * _zoom + _pan;
            float keepX = Mathf.Min(KeepVisible, size.x / 2f);
            float keepY = Mathf.Min(KeepVisible, size.y / 2f);

            if (max.x < keepX) _pan.x += keepX - max.x;
            if (min.x > size.x - keepX) _pan.x -= min.x - (size.x - keepX);
            if (max.y < keepY) _pan.y += keepY - max.y;
            if (min.y > size.y - keepY) _pan.y -= min.y - (size.y - keepY);
        }

        // Buttons either side of the selected node; the tooltip beside its node, past the + button, kept in view
        private void PlaceOverlay()
        {
            var size = layout.size;
            if (float.IsNaN(size.x))
            {
                return;
            }

            float radius = UpgradeTree.NodeSize / 2f * _zoom;
            if (_selected != null)
            {
                var center = ToView(_selected.Node.Position);
                SetPosition(_refundButton, new Vector2(center.x - radius - ActionGap - ActionSize, center.y - ActionSize / 2f));
                SetPosition(_buyButton, new Vector2(center.x + radius + ActionGap, center.y - ActionSize / 2f));
            }

            if (_infoNode == null)
            {
                return;
            }

            var tooltipSize = Tooltip.layout.size;
            if (float.IsNaN(tooltipSize.x))
            {
                return;
            }

            // Right of the node, else left, else above, else below its caption: never over the − / + buttons
            var nodeCenter = ToView(_infoNode.Position);
            float side = radius + TooltipGap + (_selected != null && _selected.Node == _infoNode ? ActionSize + ActionGap : 0f);
            float maxX = Mathf.Max(EdgePadding, size.x - tooltipSize.x - EdgePadding);
            float maxY = Mathf.Max(EdgePadding, size.y - tooltipSize.y - EdgePadding);
            Vector2 position;

            if (nodeCenter.x + side + tooltipSize.x <= size.x - EdgePadding)
            {
                position = new Vector2(nodeCenter.x + side, nodeCenter.y - tooltipSize.y / 2f);
            }
            else if (nodeCenter.x - side - tooltipSize.x >= EdgePadding)
            {
                position = new Vector2(nodeCenter.x - side - tooltipSize.x, nodeCenter.y - tooltipSize.y / 2f);
            }
            else
            {
                float above = nodeCenter.y - radius - TooltipGap - tooltipSize.y;
                float below = nodeCenter.y + radius + CaptionHeight * _zoom + TooltipGap;
                position = new Vector2(nodeCenter.x - tooltipSize.x / 2f, above >= EdgePadding ? above : below);
            }

            position.x = Mathf.Clamp(position.x, EdgePadding, maxX);
            position.y = Mathf.Clamp(position.y, EdgePadding, maxY);
            SetPosition(Tooltip, position);
        }

        private Vector2 ToView(Vector2 treePosition)
        {
            return treePosition * _zoom + _pan;
        }

        private UpgradeNodeView HitTest(Vector2 localPosition)
        {
            var treePoint = (localPosition - _pan) / _zoom;
            float radius = UpgradeTree.NodeSize * 0.55f;
            UpgradeNodeView best = null;
            float bestDistance = radius * radius;

            foreach (var (node, view) in _views)
            {
                float distance = (node.Position - treePoint).sqrMagnitude;
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = view;
                }
            }

            return best;
        }

        private void SetHovered(UpgradeNodeView view)
        {
            if (_hovered == view)
            {
                return;
            }

            _hovered?.SetHovered(false);
            _hovered = view;
            _hovered?.SetHovered(true);
            UpdateInfoNode();
        }

        // The tooltip follows the hovered node, falling back to the selected one
        private void UpdateInfoNode()
        {
            var node = (_hovered ?? _selected)?.Node;
            if (node == _infoNode)
            {
                return;
            }

            _infoNode = node;
            Tooltip.style.display = node != null ? DisplayStyle.Flex : DisplayStyle.None;
            OnInfoNodeChanged.OnNext(node);
            PlaceOverlay();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            // The − / + buttons handle their own clicks
            if (_pointerId != -1 || (evt.target is VisualElement target && _overlay.Contains(target)))
            {
                return;
            }

            _pointerId = evt.pointerId;
            _pointerStart = evt.localPosition;
            _panStart = _pan;
            _dragging = false;
            this.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != _pointerId)
            {
                bool overButton = evt.target is VisualElement target && target != this && _overlay.Contains(target);
                SetHovered(overButton ? null : HitTest(evt.localPosition));
                return;
            }

            var delta = (Vector2)evt.localPosition - _pointerStart;
            if (!_dragging && delta.magnitude > DragThreshold)
            {
                _dragging = true;
                SetHovered(null);
            }

            if (_dragging)
            {
                _pan = _panStart + delta;
                ApplyTransform();
            }

            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _pointerId)
            {
                return;
            }

            bool click = !_dragging && evt.button == 0;
            this.ReleasePointer(evt.pointerId);
            EndDrag();

            if (click)
            {
                var view = HitTest(evt.localPosition);
                OnNodeClicked.OnNext(view?.Node);
                SetHovered(view);
            }

            evt.StopPropagation();
        }

        private void EndDrag()
        {
            _pointerId = -1;
            _dragging = false;
        }

        private void OnWheel(WheelEvent evt)
        {
            // Wheel deltas differ by device and platform: a notch is one step, trackpads give fractions
            float steps = -Mathf.Clamp(evt.delta.y, -1f, 1f);
            ZoomAround(evt.localMousePosition, steps);
            evt.StopPropagation();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (_focusPending)
            {
                Focus(_focusTargets);
            }
            else
            {
                ApplyTransform();
            }
        }

        private Button CreateAction(string text, string className, System.Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.AddToClassList("light-green");
            button.AddToClassList("Button");
            button.AddToClassList("upgrade-action");
            button.AddToClassList(className);
            button.style.position = Position.Absolute;
            button.style.width = ActionSize;
            button.style.height = ActionSize;
            button.style.display = DisplayStyle.None;
            _overlay.Add(button);
            return button;
        }

        private static void SetPosition(VisualElement element, Vector2 position)
        {
            element.style.left = position.x;
            element.style.top = position.y;
        }
    }
}
