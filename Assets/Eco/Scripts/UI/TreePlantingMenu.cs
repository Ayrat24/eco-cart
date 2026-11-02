using System;
using System.Collections.Generic;
using Eco.Scripts.Trees;
using Eco.Scripts.Utils;
using Eco.Scripts.Profiling;
using Eco.Scripts.Upgrades;
using Eco.Scripts.World;
using R3;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace Eco.Scripts.UI
{
    public sealed class TreePlantingMenu
    {
        private readonly UIDocument _document;
        private readonly WorldController _world;
        private readonly CurrencyManager _currency;
        private readonly List<(TreeDefinition tree, Button button)> _buttons = new();
        private VisualElement _root;
        private VisualElement _menu;
        private ScrollView _options;
        private readonly TreePlacementPreview _preview;
        private IDisposable _subscription;
        private IVisualElementScheduledItem _refresh;
        private TreeDefinition _pressed;
        private Button _captured;
        private int _pointerId;
        private Vector2 _start;
        private Vector2 _pointer;
        private bool _dragging;
        public static bool IsDragging { get; private set; }

        public TreePlantingMenu(UIDocument document, WorldController world, CurrencyManager currency, Material previewMaterial)
        {
            _document = document;
            _world = world;
            _currency = currency;
            _preview = new TreePlacementPreview(previewMaterial);
        }

        public void Init()
        {
            _root = _document.rootVisualElement;
            _menu = _root.Q("TreePlantingMenu");
            _options = _menu.Q<ScrollView>("TreeOptions");
            _options.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _options.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _options.RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            _root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _root.RegisterCallback<DetachFromPanelEvent>(OnDetach);
            var builder = new DisposableBuilder();
            UnlockTracker.OnUnlocked.Subscribe(_ => Rebuild()).AddTo(ref builder);
            UnlockTracker.OnLocked.Subscribe(_ => Rebuild()).AddTo(ref builder);
            _currency.CurrentMoney.Subscribe(_ => RefreshPrices()).AddTo(ref builder);
            _subscription = builder.Build();
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            _refresh = _root.schedule.Execute(UpdateDrag).Every(16);
            Rebuild();
        }

        private void OnLocaleChanged(Locale locale) => Rebuild();

        private void Rebuild()
        {
            Cancel();
            _options.Clear();
            _buttons.Clear();
            foreach (var tree in _world.Trees)
            {
                if (tree == null || !tree.Unlocked) continue;
                var button = new Button(() => Buy(tree, null)) { name = "PlantTree-" + tree.Id };
                button.AddToClassList("light-green");
                button.style.width = 96;
                button.style.height = 100;
                button.style.flexShrink = 0;
                button.style.flexDirection = FlexDirection.Column;
                button.style.alignItems = Align.Center;
                var icon = new Image { sprite = tree.Icon, pickingMode = PickingMode.Ignore };
                icon.style.width = 64;
                icon.style.height = 64;
                button.Add(icon);
                var label = new Label { name = "TreePrice", pickingMode = PickingMode.Ignore };
                label.style.whiteSpace = WhiteSpace.NoWrap;
                label.style.fontSize = 18;
                label.style.height = 22;
                label.style.flexShrink = 0;
                button.Add(label);
                button.RegisterCallback<PointerDownEvent>(evt => Press(evt, tree, button), TrickleDown.TrickleDown);
                button.RegisterCallback<PointerMoveEvent>(Move);
                button.RegisterCallback<PointerUpEvent>(Release, TrickleDown.TrickleDown);
                button.RegisterCallback<PointerCancelEvent>(_ => Cancel());
                button.RegisterCallback<PointerCaptureOutEvent>(_ => Cancel());
                _buttons.Add((tree, button));
                _options.Add(button);
            }
            _menu.style.display = _buttons.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            RefreshPrices();
        }

        private void RefreshPrices()
        {
            foreach (var entry in _buttons)
            {
                entry.button.Q<Label>("TreePrice").text = entry.tree.Price.ToString();
                entry.button.style.opacity = _currency.CurrentMoney.Value >= entry.tree.Price ? 1f : 0.5f;
            }
        }

        private void Press(PointerDownEvent evt, TreeDefinition tree, Button button)
        {
            if (evt.button != 0) return;
            Cancel();
            _pressed = tree;
            _captured = button;
            _pointerId = evt.pointerId;
            _start = _pointer = evt.position;
            IsDragging = true;
            button.Focus();
            button.CapturePointer(evt.pointerId);
            evt.StopImmediatePropagation();
        }

        private void Move(PointerMoveEvent evt)
        {
            if (_pressed == null || evt.pointerId != _pointerId) return;
            var previous = _pointer;
            _pointer = evt.position;
            _dragging |= (_pointer - _start).sqrMagnitude > 64f;
            if (_dragging && _options.worldBound.Contains(_pointer) &&
                Mathf.Abs(_pointer.x - _start.x) > Mathf.Abs(_pointer.y - _start.y))
            {
                _options.scrollOffset -= new Vector2(_pointer.x - previous.x, 0f);
            }
            UpdateDrag();
        }

        private void UpdateDrag()
        {
            if (!_dragging || _pressed == null) return;
            using var marker = EcoProfilerMarkers.PlantPreview.Auto();
            if (!TryPoint(_pointer, out var point))
            {
                _preview.Hide();
                return;
            }
            bool valid = _world.TreePlanter.TryGetSpot(_pressed.PrefabId, point, out _, out _) &&
                         _pressed.Unlocked && _currency.CurrentMoney.Value >= _pressed.Price;
            _preview.Show(_pressed, point, valid);
        }

        private void OnWheel(WheelEvent evt)
        {
            _options.scrollOffset += new Vector2((evt.delta.x + evt.delta.y) * 32f, 0f);
            evt.StopImmediatePropagation();
        }

        private void Release(PointerUpEvent evt)
        {
            if (_pressed == null || evt.pointerId != _pointerId || evt.button != 0) return;
            var tree = _pressed;
            bool drag = _dragging;
            bool onButton = _captured.worldBound.Contains(evt.position);
            bool hit = TryPoint(evt.position, out var point);
            Cancel();
            if (drag)
            {
                if (hit) Buy(tree, point);
            }
            else if (onButton) Buy(tree, null);
            evt.StopImmediatePropagation();
        }

        private bool TryPoint(Vector2 panelPoint, out Vector3 point)
        {
            point = default;
            if (!_root.worldBound.Contains(panelPoint)) return false;
            var picked = _root.panel.Pick(panelPoint);
            // Picking ignores the drag ghost. Any visible HUD/menu element blocks a world drop.
            if (picked != null && picked != _root && picked != _root.panel.visualTree) return false;
            var camera = Camera.main;
            if (camera == null) return false;
            var screen = new Vector2(panelPoint.x / _root.resolvedStyle.width * Screen.width,
                (1f - panelPoint.y / _root.resolvedStyle.height) * Screen.height);
            // PanelRaycaster reports the captured button even over the world; panel.Pick above
            // already checks Toolkit geometry. Keep uGUI (including the map) blocking drops.
            if (FindUIBlockers.IsPointerOverUI(screen, ignoreToolkit: true)) return false;
            var ray = camera.ScreenPointToRay(screen);
            int groundMask = 1 << Terrain.activeTerrain.gameObject.layer;
            RaycastHit hit;
            var curved = CurvedWorldController.Active;
            bool found = curved != null
                ? curved.RaycastGround(ray, out hit, 100f, groundMask)
                : Physics.Raycast(ray, out hit, 100f, groundMask, QueryTriggerInteraction.Ignore);
            if (!found || hit.collider is not TerrainCollider) return false;
            point = hit.point;
            return true;
        }

        private void Buy(TreeDefinition tree, Vector3? point)
        {
            if (!tree.Unlocked) return;
            if (_currency.CurrentMoney.Value < tree.Price)
            {
                return;
            }
            bool planted = point.HasValue ? _world.TreePlanter.PlantTreeAt(tree.PrefabId, point.Value)
                : _world.TreePlanter.PlantTree(tree.PrefabId);
            if (planted) _currency.RemoveMoney(tree.Price);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape || _pressed == null) return;
            Cancel();
            evt.StopPropagation();
        }

        private void OnDetach(DetachFromPanelEvent evt) => Cancel();

        private void Cancel()
        {
            var captured = _captured;
            _captured = null;
            _pressed = null;
            _dragging = false;
            IsDragging = false;
            _preview.Hide();
            if (captured != null && captured.HasPointerCapture(_pointerId)) captured.ReleasePointer(_pointerId);
        }

        public void Clear()
        {
            Cancel();
            _refresh?.Pause();
            _subscription?.Dispose();
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            _root.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _root.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
            _options.Clear();
            _buttons.Clear();
            _options.UnregisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            _preview.Dispose();
        }
    }
}
