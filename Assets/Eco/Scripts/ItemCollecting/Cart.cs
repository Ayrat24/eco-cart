using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Upgrades;
using R3;
using Unity.Mathematics;
using UnityEngine;

namespace Eco.Scripts.ItemCollecting
{
    public class Cart : MonoBehaviour
    {
        [SerializeField] private BoxCollider boxCollider;
        [SerializeField] private LayerMask itemLayers;
        [SerializeField] private Transform dropPoint;

        public bool IsFull
        {
            get
            {
                int weight = 0;
                foreach (var item in _itemsInStorage)
                {
                    weight += item.GetWeight();
                    if (weight >= _maxCargoWeight)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private int CurrentCargoWeight
        {
            get
            {
                int weight = 0;
                foreach (var item in _itemsInStorage)
                {
                    weight += item.GetWeight();
                }

                return weight;
            }
        }

        public bool CanAddItems => !IsFull && !_isEmptying;
        public bool IsEmptying => _isEmptying;
        public Transform DropPoint => dropPoint;
        public CartBuyUpgrade.CartData CartData => _cartData;

        private readonly List<ICartItem> _itemsInStorage = new();
        private readonly Dictionary<ICartItem, Collider> _itemColliderDictionary = new();
        private readonly Dictionary<Collider, ICartItem> _colliderItemDictionary = new();
        private readonly Dictionary<ICartItem, DateTime> _cartItemAddTime = new();

        private bool _isEmptying;
        private CancellationTokenSource _cancellationTokenSource;
        private ItemRecycler _itemRecycler;
        private IDisposable _subscription;

        private readonly Collider[] _colliders = new Collider[100];
        private readonly HashSet<Collider> _itemsInPhysicalBox = new();
        private ItemCollector _itemCollector;

        private string _id;
        private int _maxCargoWeight;
        private CartBuyUpgrade.CartData _cartData;
        public String Id => _id;

        public readonly Subject<ICartItem> OnItemAdded = new();
        public readonly Subject<ICartItem> OnItemRemoved = new();
        public readonly Subject<CartState> OnStatusChanged = new();

        public int StorageSize => _maxCargoWeight;

        private void Start()
        {
            PlayerClickMovement.OnLeftClicked += EmptyCart;
        }

        public void Init(ItemRecycler recycler, ItemCollector itemCollector)
        {
            _itemRecycler = recycler;
            _itemCollector = itemCollector;
            _subscription = Observable.EveryUpdate().Subscribe(x => RemoveFallenOutItems());
        }

        public void SetStats(CartBuyUpgrade.CartData cartData)
        {
            _cartData = cartData;
            _id = cartData.id;
            _maxCargoWeight = cartData.carryingCapacity;
        }

        private void AddItemToCollections(ICartItem item, Collider col)
        {
            if (_itemColliderDictionary.ContainsKey(item))
            {
                return;
            }

            _cartItemAddTime[item] = DateTime.Now;
            _itemsInStorage.Add(item);
            _itemColliderDictionary.Add(item, col);
            _colliderItemDictionary.Add(col, item);

            OnItemAdded.OnNext(item);

            if (IsFull)
            {
                OnStatusChanged.OnNext(CartState.Full);
            }
            else
            {
                OnStatusChanged.OnNext(CartState.HasItems);
            }
        }

        private void RemoveItemFromCollections(ICartItem item, Collider col)
        {
            _itemsInStorage.Remove(item);
            _itemColliderDictionary.Remove(item);
            _colliderItemDictionary.Remove(col);
            _cartItemAddTime.Remove(item);

            OnItemRemoved.OnNext(item);

            if (!IsFull && _itemsInStorage.Count > 0)
            {
                OnStatusChanged.OnNext(CartState.HasItems);
            }
            else if (!IsFull)
            {
                OnStatusChanged.OnNext(CartState.Empty);
            }
        }

        private void ClearItems()
        {
            _itemsInStorage.Clear();
            _colliderItemDictionary.Clear();
            _itemColliderDictionary.Clear();
            _cartItemAddTime.Clear();

            OnItemRemoved.OnNext(null);
            OnStatusChanged.OnNext(CartState.Empty);
        }

        public void AddToCart(ICartItem item, Collider coll)
        {
            AddItemToCollections(item, coll);
        }
        
        private void RemoveFallenOutItems()
        {
            if (_isEmptying)
            {
                return;
            }

            Vector3 center = boxCollider.transform.TransformPoint(boxCollider.center);
            Vector3 halfExtents = Vector3.Scale(boxCollider.size * 0.5f, boxCollider.transform.lossyScale);
            int count = Physics.OverlapBoxNonAlloc(center, halfExtents, _colliders, quaternion.identity, itemLayers);

            _itemsInPhysicalBox.Clear();
            for (int i = 0; i < count; i++)
            {
                _itemsInPhysicalBox.Add(_colliders[i]);
            }

            foreach (var coll in _itemsInPhysicalBox)
            {
                if (_colliderItemDictionary.ContainsKey(coll))
                {
                    continue;
                }
            
                var item = coll.GetComponent<ICartItem>();
            
                if (item == null)
                {
                    continue;
                }
            
                AddItemToCollections(item, coll);
            }

            for (int i = _itemsInStorage.Count - 1; i >= 0; i--)
            {
                var item = _itemsInStorage[i];
                var col = _itemColliderDictionary[item];
                bool toRemove = !_itemsInPhysicalBox.Contains(col) &&
                                (DateTime.Now - _cartItemAddTime[item]).TotalSeconds > 1 && !item.IsBeingPickedUp;
            
                if (toRemove)
                {
                    item.OnFallenOut();
                    RemoveItemFromCollections(item, col);
                }
            }
        }

        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _subscription?.Dispose();

            PlayerClickMovement.OnLeftClicked -= EmptyCart;
        }

        public void EmptyCart()
        {
            if (_isEmptying || _itemsInStorage.Count == 0)
            {
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            EmptyAsync(_cancellationTokenSource.Token).Forget();
        }

        public bool CanFitItem(ICartItem item)
        {
            if (CurrentCargoWeight + item.GetWeight() <= _maxCargoWeight)
            {
                return true;
            }

            return false;
        }

        private async UniTask EmptyAsync(CancellationToken token)
        {
            _isEmptying = true;
            OnStatusChanged.OnNext(CartState.Recycling);

            await UniTask.WaitUntil(() => _itemCollector.AllHandsAreFree(), cancellationToken: token);
            await UniTask.WaitForSeconds(0.1f, cancellationToken: token);

            await _itemRecycler.EmptyAsync(_itemsInStorage.ToList(), token);

            ClearItems();

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _isEmptying = false;
            OnStatusChanged.OnNext(CartState.Empty);
        }

        void OnDrawGizmos()
        {
            if (boxCollider == null) return;

            Gizmos.color = Color.cyan;
            Vector3 center = boxCollider.transform.TransformPoint(boxCollider.center);
            Vector3 size = Vector3.Scale(boxCollider.size, boxCollider.transform.lossyScale);
            Gizmos.matrix = Matrix4x4.TRS(center, boxCollider.transform.rotation, size);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }

        public enum CartState
        {
            Empty,
            HasItems,
            Full,
            Recycling
        }
    }
}