using System;
using Eco.Scripts.Hats;
using Eco.Scripts.ItemCollecting;
using Eco.Scripts.Tools;
using Eco.Scripts.Upgrades;
using UnityEngine;
using VContainer;
using R3;
using UnityEngine.AI;

namespace Eco.Scripts
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private ItemCollector itemCollector;
        [SerializeField] private Stomach stomach;
        [SerializeField] private ToolController toolController;
        [SerializeField] private Transform hatParent;

        private HatController _hatController;
        private CurrencyManager _currencyManager;
        private ScoreStats _scoreStats;
        private UpgradesCollection _upgrades;
        private IDisposable _subscription;

        public Stomach Stomach => stomach;

        // Fires with the stomach whenever its capacity/speed upgrade changes
        public readonly Subject<Stomach> OnStomachChanged = new();

        [Inject]
        private void Init(CurrencyManager currencyManager, UpgradesCollection upgrades, ScoreStats scoreStats)
        {
            _currencyManager = currencyManager;
            _upgrades = upgrades;
            _scoreStats = scoreStats;
        }

        public void Spawn(SaveManager saveManager)
        {
            transform.position = saveManager.Progress.playerPosition.ToVector3();
            agent.enabled = true;

            itemCollector.Init(_currencyManager, _scoreStats, stomach);

            // The cart upgrades now size the stomach and set the speed
            var cartUpgrades = _upgrades.GetUpgradeTypes<CartBuyUpgrade>();
            var cartUpgrade = cartUpgrades.Find(x => x.upgradeId == saveManager.Progress.selectedCart);
            if (cartUpgrade == null)
            {
                cartUpgrade = cartUpgrades[0];
            }

            ApplyStomachStats(cartUpgrade.GetCartData());

            var builder = new DisposableBuilder();
            foreach (var cart in cartUpgrades)
            {
                cart.OnCartSelected.Subscribe(ApplyStomachStats).AddTo(ref builder);
            }

            _subscription = builder.Build();

            toolController.Init();
            _hatController = new HatController(_upgrades, hatParent);
            _hatController.Initialize(saveManager);

            PlayerClickMovement.OnLeftClicked += Recycle;
        }

        private void Recycle()
        {
            stomach.Recycle();
        }

        private void ApplyStomachStats(CartBuyUpgrade.CartData stats)
        {
            stomach.SetStats(stats);
            agent.speed = stats.moveSpeed;
            OnStomachChanged.OnNext(stomach);
        }

        public void Save(SaveManager saveManager)
        {
            saveManager.Progress.selectedCart = stomach.Id;
            saveManager.Progress.playerPosition = new SaveManager.Vector3Serializable(transform.position);
            _hatController.SaveHat(saveManager);
        }

        private void OnDestroy()
        {
            PlayerClickMovement.OnLeftClicked -= Recycle;

            _subscription?.Dispose();
            toolController?.Clear();
            _hatController?.Clear();
        }
    }
}
