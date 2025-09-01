using System;
using System.Collections.Generic;
using Eco.Scripts.Upgrades;
using LargeNumbers;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Eco.Scripts.UI
{
    public class UpgradeMenu : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private VisualTreeAsset upgradeItemTemplate;

        private UpgradesCollection _upgradesCollection;
        private CurrencyManager _currencyManager;

        private TabView _tabView;
        private VisualElement _upgradeMenu;
        private Button _openButton;
        private Label _currencyLabel;

        private readonly List<UpgradeButton> _buttons = new();
        private readonly List<VisualElement> _tabContents = new();
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


        [Inject]
        public void Initialize(UpgradesCollection upgradesCollection, CurrencyManager currencyManager)
        {
            _upgradesCollection = upgradesCollection;
            _currencyManager = currencyManager;
        }

        public void Init()
        {
            var root = uiDocument.rootVisualElement;

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

            foreach (var category in _upgradesCollection.upgrades)
            {
                var page = new VisualElement();
                page.AddToClassList(PageClassName);

                scrollView.Add(page);
                _tabContents.Add(page);

                foreach (var upgrade in category.upgrades)
                {
                    builder = SpawnUpgradeButton(page, upgrade, builder);
                }

                var tab = new Tab
                {
                    label = category.name
                };

                _tabView.Add(tab);
            }


            tabView.RegisterCallback<ClickEvent>((_) => SetTab());
            SetTab();
            
            _subscription = builder.Build();
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
            var button = upgradeItemTemplate.Instantiate();

            var b = button.Q<UpgradeButton>(UpgradeRootName);
            b.Init(upgrade);
            b.UpdatePurchaseAvailability(_currencyManager.CurrentMoney.Value);
            b.OnUpgradeClicked.Subscribe(OnUpgradePurchase).AddTo(ref builder);

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

        private void OnOpenUpgradeMenuButtonClicked(ClickEvent evt)
        {
            _menuOpen = !_menuOpen;
            SetMenuState(_menuOpen);
        }

        private void SetMenuState(bool isOpen)
        {
            _menuOpen = isOpen;
            if (!_menuOpen)
            {
                _upgradeMenu.AddToClassList(HiddenClassName);
                _openButton.text = "Upgrades";
            }
            else
            {
                _upgradeMenu.RemoveFromClassList(HiddenClassName);
                _openButton.text = "Close";

                UpdateButtons();
            }
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
        }
    }
}