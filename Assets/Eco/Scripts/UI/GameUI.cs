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
        [SerializeField] private VisualTreeAsset cartItemTemplate;

        private UpgradeMenu _upgradeMenu;
        private CartStorageDisplay _cartStorageDisplay;
        private TutorialMenu _tutorialMenu;
        private ToolSelector _toolSelector;
        private ProgressDisplay _progress;
        
        public void Init(UpgradesCollection upgradesCollection, CurrencyManager currencyManager, Player player, WorldProgress worldProgress)
        {
            _upgradeMenu = new UpgradeMenu(uiDocument, upgradeItemTemplate, upgradesCollection, currencyManager);
            _upgradeMenu.Init();

            _cartStorageDisplay = new CartStorageDisplay(uiDocument, player, cartItemTemplate);
            _cartStorageDisplay.Init();

            _toolSelector = new ToolSelector(uiDocument);
            _toolSelector.Init();

            _tutorialMenu = new TutorialMenu();
            _tutorialMenu.Init(uiDocument);

            _progress = new ProgressDisplay(uiDocument, worldProgress);
            _progress.Init();
        }

        public void Clear()
        {
            _upgradeMenu.Clear();
            _cartStorageDisplay.Clear();
            _tutorialMenu.Clear();
            _toolSelector.Clear();
            _progress.Clear();
        }
    }
}