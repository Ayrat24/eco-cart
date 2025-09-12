using System;
using Cysharp.Threading.Tasks;
using Eco.Scripts.ItemCollecting;
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
        
        private CurrencyManager _currencyManager;
        private UpgradesCollection _upgrades;
        private Cart _cart;
        private IDisposable _subscription;
        private bool _changingCart;

        public Subject<Cart> OnCartChanged = new();

        [Inject]
        private void Init(CurrencyManager currencyManager, UpgradesCollection upgrades)
        {
            _currencyManager = currencyManager;
            _upgrades = upgrades;
        }

        public void Spawn(SaveManager saveManager)
        {
            transform.position = saveManager.Progress.playerPosition.ToVector3();
            agent.enabled = true;

            var cartUpgrades = _upgrades.GetUpgradeTypes<CartBuyUpgrade>();
            var cartUpgrade = cartUpgrades.Find(x => x.upgradeId == saveManager.Progress.selectedCart);
            if (cartUpgrade == null)
            {
                cartUpgrade = cartUpgrades[0];
            }

            SpawnNewCart(cartUpgrade.GetCartData());

            var builder = new DisposableBuilder();
            foreach (var cart in cartUpgrades)
            {
                cart.OnCartSelected.Subscribe(ChangeCart).AddTo(ref builder);
            }
            _subscription = builder.Build();
        }

        private void SpawnNewCart(CartBuyUpgrade.CartData cartData)
        {
            _cart = Instantiate(cartData.prefab, transform);
            _cart.transform.localPosition = cartData.offset;
            _cart.SetStats(cartData);
            
            agent.speed = cartData.moveSpeed;
            
            itemCollector.Init(_currencyManager, _upgrades, _cart);
            OnCartChanged.OnNext(_cart);
        }

        private void ChangeCart(CartBuyUpgrade.CartData cart)
        {
            if (_changingCart)
            {
                return;
            }
            
            ChangeCartAsync(cart).Forget();
        }

        private async UniTask ChangeCartAsync(CartBuyUpgrade.CartData cart)
        {
            _changingCart = true;
            _cart.EmptyCart();
            await UniTask.WaitWhile(() => _cart.IsEmptying);
            Destroy(_cart?.gameObject);
            await UniTask.NextFrame();

            SpawnNewCart(cart);
            
            _changingCart = false;
        }

        public void SavePosition(SaveManager saveManager)
        {
            saveManager.Progress.selectedCart = _cart.Id;
            saveManager.Progress.playerPosition = new SaveManager.Vector3Serializable(transform.position);
        }

        private void OnDestroy()
        {
            _subscription.Dispose();
        }
    }
}