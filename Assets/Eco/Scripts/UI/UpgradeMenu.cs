using System;
using System.Collections.Generic;
using Eco.Scripts.Upgrades;
using LargeNumbers;
using R3;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace Eco.Scripts.UI
{
    public class UpgradeMenu
    {
        private UIDocument _uiDocument;
        private VisualTreeAsset _upgradeItemTemplate;
        private VisualTreeAsset _upgradeGroupHolder;

        private UpgradesCollection _upgradesCollection;
        private CurrencyManager _currencyManager;

        private TabView _tabView;
        private VisualElement _upgradeMenu;
        private Button _openButton;
        private Label _currencyLabel;

        private readonly List<UpgradeButton> _buttons = new();
        private readonly List<VisualElement> _tabContents = new();

        private readonly Dictionary<UpgradesCollection.UpgradeTab, LocalizedString.ChangeHandler> _tabLocHandlers =
            new();

        private bool _menuOpen = true;
        private IDisposable _subscription;

        private const string UpgradeRootName = "Upgrade";
        private const string UpgradeMenuRootName = "UpgradeMenu";
        private const string UpgradeListName = "UpgradeList";
        private const string MoneyCounterLabelName = "MoneyCounter";
        private const string OpenButtonName = "OpenUpgradeMenuButton";
        private const string UpgradeTabsName = "UpgradeTabs";
        private const string PageClassName = "upgrade-page";
        private const string HiddenClassName = "Hidden";

        private const string LocTableName = "GameUI";
        private const string OpenLocString = "open-upgrade-menu";
        private const string CloseLocString = "close-upgrade-menu";

        private readonly LocalizedString _openLocString = new LocalizedString(LocTableName, OpenLocString);
        private readonly LocalizedString _closeLocString = new LocalizedString(LocTableName, CloseLocString);

        private string OpenText => _openLocString.GetLocalizedString();
        private string CloseText => _closeLocString.GetLocalizedString();

        public static Subject<bool> OnOpen { get; } = new();


        public UpgradeMenu(UIDocument uiDocument, VisualTreeAsset upgradeItemTemplate,
            VisualTreeAsset upgradeGroupHolder,
            UpgradesCollection upgradesCollection, CurrencyManager currencyManager)
        {
            _uiDocument = uiDocument;
            _upgradeItemTemplate = upgradeItemTemplate;
            _upgradeGroupHolder = upgradeGroupHolder;
            _upgradesCollection = upgradesCollection;
            _currencyManager = currencyManager;
        }

        public void Init()
        {
            var root = _uiDocument.rootVisualElement;

            _upgradeMenu = root.Q<VisualElement>(UpgradeMenuRootName);
            _currencyLabel = root.Q<Label>(MoneyCounterLabelName);

            _openButton = root.Q<Button>(OpenButtonName);
            _openButton.RegisterCallback<ClickEvent>(OnOpenUpgradeMenuButtonClicked);

            var scrollView = root.Q<DragScrollView>(UpgradeListName);
            scrollView.Init();
            scrollView.Interactable = true;

            _tabView = root.Q<TabView>(UpgradeTabsName);
            SpawnButtons(scrollView, _tabView);
            SetMenuState(false);
        }

        private void SpawnButtons(DragScrollView scrollView, TabView tabView)
        {
            var builder = new DisposableBuilder();

            _currencyManager.CurrentMoney.Subscribe(_ => UpdateButtons()).AddTo(ref builder);
            _currencyManager.CurrentMoney.Subscribe(UpdateCurrencyCounter).AddTo(ref builder);
            UnlockTracker.OnUnlocked.Subscribe(OnUpgradeUnlocked).AddTo(ref builder);

            foreach (var category in _upgradesCollection.upgrades)
            {
                var page = new VisualElement();
                page.AddToClassList(PageClassName);

                scrollView.Add(page);
                _tabContents.Add(page);

                foreach (var upgradeGroup in category.upgradeGroups)
                {
                    var group = _upgradeGroupHolder.Instantiate();
                    var container = group.Q<VisualElement>("UpgradeGroup");
                    
                    foreach (var upgrade in upgradeGroup.upgrades)
                    {
                        builder = SpawnUpgradeButton(container, upgrade, builder);
                    }
                    
                    page.Add(container);
                }

                var tab = new Tab();
                LocalizedString.ChangeHandler handler = tabName => OnTabNameChanged(tabName, tab);
                category.nameLoc.StringChanged += handler;
                _tabLocHandlers[category] = handler;

                _tabView.Add(tab);
            }


            tabView.RegisterCallback<ClickEvent>((_) => SetTab());
            SetTab();

            _subscription = builder.Build();
        }

        private void OnTabNameChanged(string tabName, Tab tab)
        {
            tab.label = tabName;
        }

        private void SetTab()
        {
            var tabIndex = _tabView.selectedTabIndex;
            for (int i = 0; i < _tabContents.Count; i++)
            {
                _tabContents[i].style.display = i == tabIndex ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private DisposableBuilder SpawnUpgradeButton(VisualElement page,
            Upgrade upgrade,
            DisposableBuilder builder)
        {
            var button = _upgradeItemTemplate.Instantiate();

            var b = button.Q<UpgradeButton>(UpgradeRootName);
            b.Init(upgrade);
            b.UpdatePurchaseAvailability(_currencyManager.CurrentMoney.Value);
            b.OnUpgradeClicked.Subscribe(OnUpgradePurchase).AddTo(ref builder);

            // Check if upgrade's prerequisite is met
            if (upgrade.NeedsUpgrade != UnlockableUpgradeType.None && 
                !UnlockTracker.IsUpgradeUnlocked(upgrade.NeedsUpgrade))
            {
                b.style.display = DisplayStyle.None;
            }

            page.Add(b);
            _buttons.Add(b);
            return builder;
        }

        private void OnUpgradePurchase(Upgrade upgrade)
        {
            _currencyManager.RemoveMoney(upgrade.Cost);
            upgrade.BuyUpgrade();
            UpdateButtons();
        }

        private void UpdateCurrencyCounter(AlphabeticNotation money)
        {
            _currencyLabel.text = money.ToString();
        }

        private void UpdateButtons()
        {
            if (!_menuOpen)
            {
                return;
            }

            foreach (var btn in _buttons)
            {
                btn.UpdatePurchaseAvailability(_currencyManager.CurrentMoney.Value);
            }
        }

        private void OnUpgradeUnlocked(UnlockableUpgradeType unlockedType)
        {
            // Show any upgrades that were waiting for this unlock
            foreach (var btn in _buttons)
            {
                var upgrade = btn.Upgrade;
                if (upgrade.NeedsUpgrade == unlockedType)
                {
                    btn.style.display = DisplayStyle.Flex;
                }
            }
        }

        private void OnOpenUpgradeMenuButtonClicked(ClickEvent evt)
        {
            _menuOpen = !_menuOpen;
            SetMenuState(_menuOpen);
        }

        private void SetMenuState(bool isOpen)
        {
            _menuOpen = isOpen;
            OnOpen.OnNext(_menuOpen);

            if (!_menuOpen)
            {
                _upgradeMenu.AddToClassList(HiddenClassName);
                _openButton.text = OpenText;
            }
            else
            {
                _upgradeMenu.RemoveFromClassList(HiddenClassName);
                _openButton.text = CloseText;

                UpdateButtons();
            }
        }

        public void Clear()
        {
            _subscription?.Dispose();

            foreach (var category in _upgradesCollection.upgrades)
            {
                if (_tabLocHandlers.TryGetValue(category, out var handler))
                {
                    category.nameLoc.StringChanged -= handler;
                }
            }

            foreach (var btn in _buttons)
            {
                btn.Clean();
            }
        }
    }
}