using Eco.Scripts.Cart;
using UnityEngine;
using VContainer;

namespace Eco.Scripts
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private ItemCollector itemCollector;
        private MoneyController _moneyController;
        private UpgradesCollection _upgrades;

        [Inject]
        private void Init(MoneyController moneyController, UpgradesCollection upgrades)
        {
            _moneyController = moneyController;
            _upgrades = upgrades;
        }
        
        public void Spawn()
        {
            itemCollector.Init(_moneyController, _upgrades);
        }

        public void SavePosition(SaveManager saveManager)
        {
            saveManager.Progress.playerPosition = transform.position;
        }
    }
}