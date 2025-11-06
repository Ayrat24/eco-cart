using System;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Hats;
using Eco.Scripts.ItemCollecting;
using Eco.Scripts.Tools;
using Eco.Scripts.Upgrades;
using UnityEngine;
using VContainer;
using R3;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using Eco.Scripts.Utils;

namespace Eco.Scripts
{
    public class Player : MonoBehaviour
    {
        private Vector2 _lastPointerPosition;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private ItemCollector itemCollector;
        [SerializeField] private Transform characterModel;
        [SerializeField] private ToolController toolController;
        [SerializeField] private Transform hatParent;
        
        private HatController _hatController;
        private CurrencyManager _currencyManager;
        private ScoreStats _scoreStats;
        private UpgradesCollection _upgrades;
        private Cart _cart;
        private IDisposable _subscription;
        private bool _changingCart;

        public readonly Subject<Cart> OnCartChanged = new();

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

            toolController.Init();
            _hatController = new HatController(_upgrades, hatParent);
            _hatController.Initialize(saveManager);
            
            PlayerClickMovement.OnLeftClicked += EmptyCart;
        }

        private void EmptyCart()
        {
            // Don't empty the cart if the click happened over UI
            if (IsPointerOverUI())
            {
                return;
            }

            _cart?.EmptyCart();
        }
        
        // Minimal UI pointer check using EventSystem.RaycastAll; supports the new Input System.
         private bool IsPointerOverUI()
         {
             if (EventSystem.current == null)
                 return false;

             // Read pointer position using the new Input System only (Pointer, Mouse, Touchscreen)
             Vector2 pointerPosition;
             if (Pointer.current != null)
             {
                 pointerPosition = Pointer.current.position.ReadValue();
             }
             else if (Mouse.current != null)
             {
                 pointerPosition = Mouse.current.position.ReadValue();
             }
             else if (Touchscreen.current != null && Touchscreen.current.primaryTouch != null)
             {
                 pointerPosition = Touchscreen.current.primaryTouch.position.ReadValue();
             }
             else
             {
                 // No input device available in this frame; use last known pointer position.
                 pointerPosition = _lastPointerPosition;
             }

             // Delegate the actual raycast/filtering to the runtime helper
             return FindUIBlockers.IsPointerOverUI(pointerPosition);
         }

        private void SpawnNewCart(CartBuyUpgrade.CartData cartData)
        {
            _cart = Instantiate(cartData.prefab, transform);
            _cart.transform.localPosition = cartData.offset;
            _cart.SetStats(cartData);

            agent.speed = cartData.moveSpeed;
            characterModel.localPosition = cartData.characterModelOffset;

            itemCollector.Init(_currencyManager, _scoreStats, _cart);
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

        public void Save(SaveManager saveManager)
        {
            saveManager.Progress.selectedCart = _cart.Id;
            saveManager.Progress.playerPosition = new SaveManager.Vector3Serializable(transform.position);
            _hatController.SaveHat(saveManager);
        }

        private void OnDestroy()
        {
            PlayerClickMovement.OnLeftClicked -= EmptyCart;

            _subscription?.Dispose();
            toolController?.Clear();
            _hatController?.Clear();
        }

        private void Update()
        {
            // update last known pointer position every frame using the new Input System
            if (Pointer.current != null)
            {
                _lastPointerPosition = Pointer.current.position.ReadValue();
            }
            else if (Mouse.current != null)
            {
                _lastPointerPosition = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch != null)
            {
                _lastPointerPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            // if no input device is available, keep the previous _lastPointerPosition unchanged
        }
    }
}