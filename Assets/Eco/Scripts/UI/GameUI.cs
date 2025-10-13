using Eco.Scripts.Upgrades;
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
        
        public void Init(UpgradesCollection upgradesCollection, CurrencyManager currencyManager, Player player)
        {
            _upgradeMenu = new UpgradeMenu(uiDocument, upgradeItemTemplate, upgradesCollection, currencyManager);
            _upgradeMenu.Init();

            _cartStorageDisplay = new CartStorageDisplay(uiDocument, player, cartItemTemplate);
            _cartStorageDisplay.Init();

            _toolSelector = new ToolSelector(uiDocument);
            _toolSelector.Init();

            _tutorialMenu = new TutorialMenu();
            _tutorialMenu.Init(uiDocument);
        }

        public void Clear()
        {
            _upgradeMenu.Clear();
            _cartStorageDisplay.Clear();
            _tutorialMenu.Clear();
            _toolSelector.Clear();
        }
    }
}