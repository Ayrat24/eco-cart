using System;
using System.Collections.Generic;
using Eco.Scripts.Upgrades;
using LargeNumbers;
using R3;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace Eco.Scripts.UI
{
    /// <summary>
    /// The upgrade menu: a side panel with the <see cref="UpgradeTreeView"/>. Hovering a node shows its tooltip;
    /// selecting it shows − / + to refund or buy a level. The game keeps running beside it. Also keeps the HUD
    /// money counter up to date.
    /// </summary>
    public class UpgradeTreeMenu
    {
        private readonly UIDocument _uiDocument;
        private readonly UpgradeManager _upgradeManager;
        private readonly CurrencyManager _currencyManager;

        private VisualElement _root;
        private VisualElement _menu;
        private UpgradeTreeView _treeView;
        private Button _openButton;
        private Button _closeButton;
        private Button _zoomInButton;
        private Button _zoomOutButton;
        private Button _focusButton;
        private Label _title;
        private Label _hint;
        private Label _currencyLabel;

        private UpgradeNode _selected;
        private bool _menuOpen;
        private bool _focused;
        private IDisposable _subscription;

        private const string MenuName = "UpgradeTreeMenu";
        private const string TreeViewName = "UpgradeTreeView";
        private const string OpenButtonName = "OpenUpgradeMenuButton";
        private const string MoneyCounterLabelName = "MoneyCounter";
        private const string ClosingClass = "upgrade-menu--closed";

        private const string LocTableName = "GameUI";

        private readonly LocalizedString _openText = new(LocTableName, "open-upgrade-menu");
        private readonly LocalizedString _closeText = new(LocTableName, "close-upgrade-menu");
        private readonly LocalizedString _titleText = new(LocTableName, "upgrade-tree-title");
        private readonly LocalizedString _hintText = new(LocTableName, "upgrade-tree-hint");
        private readonly LocalizedString _focusText = new(LocTableName, "upgrade-tree-center");
        private readonly LocalizedString _levelText = new(LocTableName, "upgrade-level");
        private readonly LocalizedString _priceText = new(LocTableName, "upgrade-price");
        private readonly LocalizedString _refundText = new(LocTableName, "upgrade-refund");
        private readonly LocalizedString _refundBlockedText = new(LocTableName, "upgrade-refund-blocked");
        private readonly LocalizedString _maxedText = new(LocTableName, "upgrade-maxed");
        private readonly LocalizedString _requiresAnyText = new(LocTableName, "upgrade-requires-any");
        private readonly LocalizedString _requiresAllText = new(LocTableName, "upgrade-requires-all");

        /// <summary>
        /// Fraction of the screen width the open menu covers from the left; 0 when closed. The camera keeps the
        /// jelly in the middle of the rest.
        /// </summary>
        public static Subject<float> OnScreenCoverChanged { get; } = new();

        public UpgradeTreeMenu(UIDocument uiDocument, UpgradeManager upgradeManager, CurrencyManager currencyManager)
        {
            _uiDocument = uiDocument;
            _upgradeManager = upgradeManager;
            _currencyManager = currencyManager;
        }

        public void Init()
        {
            _root = _uiDocument.rootVisualElement;

            _menu = _root.Q<VisualElement>(MenuName);
            _treeView = _menu.Q<UpgradeTreeView>(TreeViewName);
            _title = _menu.Q<Label>("UpgradeMenuTitle");
            _hint = _menu.Q<Label>("UpgradeTreeHint");
            _closeButton = _menu.Q<Button>("CloseUpgradeMenuButton");
            _zoomInButton = _menu.Q<Button>("ZoomInButton");
            _zoomOutButton = _menu.Q<Button>("ZoomOutButton");
            _focusButton = _menu.Q<Button>("FrameButton");

            _openButton = _root.Q<Button>(OpenButtonName);
            _currencyLabel = _root.Q<Label>(MoneyCounterLabelName);

            _openButton.clicked += ToggleMenu;
            _closeButton.clicked += CloseMenu;
            _zoomInButton.clicked += ZoomIn;
            _zoomOutButton.clicked += ZoomOut;
            _focusButton.clicked += FocusOnBuyable;
            _menu.RegisterCallback<GeometryChangedEvent>(OnMenuGeometryChanged);

            _treeView.Build(_upgradeManager);

            var builder = new DisposableBuilder();
            _treeView.OnNodeClicked.Subscribe(Select).AddTo(ref builder);
            _treeView.OnInfoNodeChanged.Subscribe(FillTooltip).AddTo(ref builder);
            _treeView.OnBuyClicked.Subscribe(Buy).AddTo(ref builder);
            _treeView.OnRefundClicked.Subscribe(Refund).AddTo(ref builder);
            _currencyManager.CurrentMoney.Subscribe(OnMoneyChanged).AddTo(ref builder);
            _upgradeManager.OnUpgradeBought.Subscribe(OnUpgradeBought).AddTo(ref builder);
            _upgradeManager.OnUpgradeRefunded.Subscribe(_ => Refresh()).AddTo(ref builder);
            _subscription = builder.Build();

            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            RefreshTexts();
            SetMenuState(false);
        }

        private void ToggleMenu()
        {
            SetMenuState(!_menuOpen);
        }

        private void CloseMenu()
        {
            SetMenuState(false);
        }

        private void ZoomIn()
        {
            _treeView.ZoomBy(1f);
        }

        private void ZoomOut()
        {
            _treeView.ZoomBy(-1f);
        }

        private void SetMenuState(bool isOpen)
        {
            _menuOpen = isOpen;
            _openButton.text = isOpen ? _closeText.GetLocalizedString() : _openText.GetLocalizedString();

            if (!isOpen)
            {
                _menu.style.display = DisplayStyle.None;
                Select(null);
                OnScreenCoverChanged.OnNext(0f);
                return;
            }

            // Fade in from the closed look
            _menu.AddToClassList(ClosingClass);
            _menu.style.display = DisplayStyle.Flex;
            _menu.schedule.Execute(() => _menu.RemoveFromClassList(ClosingClass));

            // The first time, look at what can be bought next; later the view stays where the player left it
            if (!_focused)
            {
                _focused = true;
                FocusOnBuyable();
            }

            Refresh();
        }

        private void FocusOnBuyable()
        {
            var buyable = new List<UpgradeNode>();
            foreach (var node in _upgradeManager.Tree.Nodes)
            {
                var state = _upgradeManager.GetState(node);
                if (state is UpgradeNodeState.Available or UpgradeNodeState.Owned)
                {
                    buyable.Add(node);
                }
            }

            _treeView.Focus(buyable);
        }

        // Layout rects, not world bounds: those include the slide-in transition
        private void OnMenuGeometryChanged(GeometryChangedEvent evt)
        {
            float width = _menu.parent.layout.width;
            if (_menuOpen && width > 0f)
            {
                OnScreenCoverChanged.OnNext(_menu.layout.xMax / width);
            }
        }

        private void Select(UpgradeNode node)
        {
            _selected = node;
            _treeView.SetSelected(node);
        }

        private void Buy(UpgradeNode node)
        {
            if (node != null)
            {
                _upgradeManager.TryBuy(node);
            }
        }

        private void Refund(UpgradeNode node)
        {
            if (node != null)
            {
                _upgradeManager.TryRefund(node);
            }
        }

        private void OnUpgradeBought(UpgradeNode node)
        {
            _treeView.PlayBought(node);
            Refresh();
        }

        private void OnMoneyChanged(AlphabeticNotation money)
        {
            _currencyLabel.text = money.ToString();

            if (_menuOpen)
            {
                Refresh();
            }
        }

        private void OnLocaleChanged(Locale locale)
        {
            RefreshTexts();
        }

        private void Refresh()
        {
            _treeView.Refresh();
            FillTooltip(_treeView.InfoNode);
        }

        private void RefreshTexts()
        {
            _title.text = _titleText.GetLocalizedString();
            _hint.text = _hintText.GetLocalizedString();
            _focusButton.text = _focusText.GetLocalizedString();
            _openButton.text = _menuOpen ? _closeText.GetLocalizedString() : _openText.GetLocalizedString();

            foreach (var node in _upgradeManager.Tree.Nodes)
            {
                _treeView.SetNodeName(node, GetName(node));
            }

            FillTooltip(_treeView.InfoNode);
        }

        private void FillTooltip(UpgradeNode node)
        {
            if (node == null)
            {
                return;
            }

            var state = _upgradeManager.GetState(node);
            int level = _upgradeManager.GetLevel(node);

            string description = node.Description.IsEmpty ? null : node.Description.GetLocalizedString();

            // Effects as "now → after buying"
            var effects = new List<string>();
            var current = _upgradeManager.BuildStats();
            var next = state == UpgradeNodeState.Maxed ? null : _upgradeManager.BuildStats(node);
            foreach (var effect in node.Effects)
            {
                if (effect != null)
                {
                    effects.Add(effect.Describe(current, next));
                }
            }

            string requirement = null;
            if (state == UpgradeNodeState.Locked)
            {
                var text = node.Requirement == UpgradeRequirement.All ? _requiresAllText : _requiresAnyText;
                requirement = string.Format(text.GetLocalizedString(), GetRequirementNames(node));
            }

            string price = state == UpgradeNodeState.Maxed
                ? _maxedText.GetLocalizedString()
                : string.Format(_priceText.GetLocalizedString(), _upgradeManager.GetCost(node));

            string refund = null;
            if (level > 0)
            {
                refund = _upgradeManager.CanRefund(node)
                    ? string.Format(_refundText.GetLocalizedString(), _upgradeManager.GetRefund(node))
                    : _refundBlockedText.GetLocalizedString();
            }

            _treeView.Tooltip.Set(GetName(node), string.Format(_levelText.GetLocalizedString(), level, node.MaxLevel),
                description, effects, requirement, price, refund);
        }

        private string GetRequirementNames(UpgradeNode node)
        {
            var names = new List<string>();
            foreach (var id in node.Requires)
            {
                var parent = _upgradeManager.Tree.Find(id);
                if (parent != null && _upgradeManager.GetLevel(parent) == 0)
                {
                    names.Add(GetName(parent));
                }
            }

            return string.Join(", ", names);
        }

        private static string GetName(UpgradeNode node)
        {
            return node.Title.IsEmpty ? node.Id : node.Title.GetLocalizedString();
        }

        public void Clear()
        {
            _subscription?.Dispose();
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            OnScreenCoverChanged.OnNext(0f);

            if (_openButton != null)
            {
                _openButton.clicked -= ToggleMenu;
                _closeButton.clicked -= CloseMenu;
                _zoomInButton.clicked -= ZoomIn;
                _zoomOutButton.clicked -= ZoomOut;
                _focusButton.clicked -= FocusOnBuyable;
                _menu.UnregisterCallback<GeometryChangedEvent>(OnMenuGeometryChanged);
            }
        }
    }
}
