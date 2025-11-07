using Eco.Scripts.Upgrades;
using Eco.Scripts.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.UI
{
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private VisualTreeAsset upgradeItemTemplate;
        [SerializeField] private VisualTreeAsset upgradeGroupItemTemplate;
        [SerializeField] private VisualTreeAsset cartItemTemplate;
        [SerializeField] private Settings settings;

        private UpgradeMenu _upgradeMenu;
        private CartStorageDisplay _cartStorageDisplay;
        private TutorialMenu _tutorialMenu;
        private ToolSelector _toolSelector;
        private ProgressDisplay _progress;
        private NewWorldPopup _newWorldPopup;
        private SettingsMenu _settingsMenu;

        public void Init(UpgradesCollection upgradesCollection, CurrencyManager currencyManager, Player player,
            ProgressTracker progressTracker)
        {
            _upgradeMenu = new UpgradeMenu(uiDocument, upgradeItemTemplate, upgradeGroupItemTemplate,
                upgradesCollection, currencyManager);
            _upgradeMenu.Init();

            _cartStorageDisplay = new CartStorageDisplay(uiDocument, player, cartItemTemplate);
            _cartStorageDisplay.Init();

            _toolSelector = new ToolSelector(uiDocument);
            _toolSelector.Init();

            _tutorialMenu = new TutorialMenu();
            _tutorialMenu.Init(uiDocument);

            _settingsMenu = new SettingsMenu(uiDocument, settings);
            _settingsMenu.Init();

            _newWorldPopup = new NewWorldPopup(uiDocument);
            _newWorldPopup.Init();
            _newWorldPopup.OnAccept += OnNewWorldAccepted;

            _progress = new ProgressDisplay(uiDocument, progressTracker, _newWorldPopup);
            _progress.Init();
        }

        private void OnNewWorldAccepted()
        {
            // Open the world selector to let player choose a new world
            WorldSelector.Instance.Open();
        }

        public void Clear()
        {
            _upgradeMenu.Clear();
            _cartStorageDisplay.Clear();
            _tutorialMenu.Clear();
            _toolSelector.Clear();
            _progress.Clear();
            _newWorldPopup?.Clear();
            _settingsMenu?.Clear();
            
            if (_newWorldPopup != null)
                _newWorldPopup.OnAccept -= OnNewWorldAccepted;
        }
    }
}