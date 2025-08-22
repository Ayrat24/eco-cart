using System;
using System.Collections.Generic;
using Eco.Scripts.Upgrades;
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
        private bool _spawnedButtons;
        private readonly List<UpgradeButton> _buttons = new();
        private CurrencyManager _currencyManager;
        private VisualElement _upgradeMenu;
        private Button _button;
        private bool _menuOpen;
        private IDisposable _subscription;

        [Inject]
        public void Initialize(UpgradesCollection upgradesCollection, CurrencyManager currencyManager)
        {
            _upgradesCollection = upgradesCollection;
            _currencyManager = currencyManager;
        }

        public void Init()
        {
            var root = uiDocument.rootVisualElement;

            _upgradeMenu = root.Q<VisualElement>("UpgradeMenu");

            _button = root.Q<Button>("OpenUpgradeMenuButton");
            _button.RegisterCallback<ClickEvent>(OnOpenUpgradeMenuButtonClicked);

            var scrollView = root.Q<DragScrollView>("UpgradeList");
            scrollView.Interactable = true;
            SpawnButtons(scrollView);
        }

        private void SpawnButtons(DragScrollView scrollView)
        {
            var builder = new DisposableBuilder();

            _currencyManager.CurrentMoney.Subscribe((x) => UpdateButtons()).AddTo(ref builder);
            
            foreach (var upgrade in _upgradesCollection.trashScoreUpgrades)
            {
                builder = SpawnUpgradeButton(scrollView, upgrade, builder);
            }
            
            foreach (var upgrade in _upgradesCollection.trashScoreUpgrades)
            {
                builder = SpawnUpgradeButton(scrollView, upgrade, builder);
            }
            
            foreach (var upgrade in _upgradesCollection.trashScoreUpgrades)
            {
                builder = SpawnUpgradeButton(scrollView, upgrade, builder);
            }

            // foreach (var upgrade in _upgradesCollection.treeBuyUpgrades)
            // {
            //     var button = Instantiate(upgradeButtonPrefab, upgradeButtonsParent);
            //     button.Init(upgrade, _currencyManager);
            //     _buttons.Add(button);
            // }
            //
            // foreach (var upgrade in _upgradesCollection.helperBuyUpgrades)
            // {
            //     var button = Instantiate(upgradeButtonPrefab, upgradeButtonsParent);
            //     button.Init(upgrade, _currencyManager);
            //     _buttons.Add(button);
            // }
            //
            // foreach (var upgrade in _upgradesCollection.cartBuyUpgrades)
            // {
            //     var button = Instantiate(upgradeButtonPrefab, upgradeButtonsParent);
            //     button.Init(upgrade, _currencyManager);
            //     _buttons.Add(button);
            // }
            
            _subscription = builder.Build();
        }

        private DisposableBuilder SpawnUpgradeButton(DragScrollView scrollView, TrashScoreUpgrade upgrade,
            DisposableBuilder builder)
        {
            var button = upgradeItemTemplate.Instantiate();

            var b = button.Q<UpgradeButton>("Upgrade");
            b.Init(upgrade);
            b.UpdatePurchaseAvailability(_currencyManager.CurrentMoney.Value);
            b.OnUpgradeClicked.Subscribe(OnUpgradePurchase).AddTo(ref builder);

            scrollView.Add(button);
            _buttons.Add(b);
            return builder;
        }

        private void OnUpgradePurchase(Upgrade upgrade)
        {
            _currencyManager.AddMoney(-upgrade.Cost);
            upgrade.BuyUpgrade();
            UpdateButtons();
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
            const string className = "Hidden";
            if (_menuOpen)
            {
                _upgradeMenu.AddToClassList(className);
                _button.text = "Menu";
            }
            else
            {
                _upgradeMenu.RemoveFromClassList(className);
                _button.text = "Close";
                
                UpdateButtons();
            }

            _menuOpen = !_menuOpen;
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
        }
    }
}