using System;
using Eco.Scripts.Upgrades;
using Eco.Scripts.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.UI
{
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private VisualTreeAsset cartItemTemplate;
        [SerializeField] private Settings settings;
        [SerializeField] private Material plantingPreviewMaterial;

        private UpgradeTreeMenu _upgradeMenu;
        private CartStorageDisplay _cartStorageDisplay;
        private WetnessDisplay _wetnessDisplay;
        private TutorialMenu _tutorialMenu;
        private TreePlantingMenu _treeMenu;
        private SettingsMenu _settingsMenu;

        public void Init(UpgradeManager upgradeManager, CurrencyManager currencyManager, Player player,
            Action resetProgress, WorldController world)
        {
            _upgradeMenu = new UpgradeTreeMenu(uiDocument, upgradeManager, currencyManager);
            _upgradeMenu.Init();

            _cartStorageDisplay = new CartStorageDisplay(uiDocument, player, cartItemTemplate);
            _cartStorageDisplay.Init();

            _wetnessDisplay = new WetnessDisplay(uiDocument, player);
            _wetnessDisplay.Init();

            _treeMenu = new TreePlantingMenu(uiDocument, world, currencyManager, plantingPreviewMaterial);
            _treeMenu.Init();

            _tutorialMenu = new TutorialMenu();
            _tutorialMenu.Init(uiDocument);

            _settingsMenu = new SettingsMenu(uiDocument, settings, resetProgress);
            _settingsMenu.Init();
        }

        public void Clear()
        {
            _upgradeMenu.Clear();
            _cartStorageDisplay.Clear();
            _wetnessDisplay.Clear();
            _tutorialMenu.Clear();
            _treeMenu.Clear();
            _settingsMenu?.Clear();
        }
    }
}