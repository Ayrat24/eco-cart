using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer;

public class Cart : MonoBehaviour
{
    [SerializeField] private int maxItems = 10;
    [SerializeField] BoxCollider boxCollider;
    
    public Transform dropPoint;
    public float dropOffsetMaxValue = 0.2f;
    public bool IsFull => _cartItems.Count >= maxItems && !_isEmptying;
    
    private readonly HashSet<ICartItem> _cartItems = new();
    private readonly Dictionary<ICartItem, Collider> _cartItemColliders = new();
    private readonly Dictionary<Collider, ICartItem> _cartColliderItems = new();
    private readonly Dictionary<ICartItem, DateTime> _cartItemAddTime = new();

    private bool _isEmptying;
    private CancellationTokenSource _cancellationTokenSource;
    private TrashStats _trashStats;
    private MoneyController _moneyController;
    private IDisposable _subscription;

    private Collider[] _colliders = new Collider[100];
    private HashSet<Collider> _itemsInPhysicalBox = new ();

    [Inject]
    public void Initialize(TrashStats trashStats, MoneyController moneyController)
    {
        _trashStats = trashStats;
        _moneyController = moneyController;
    }

    private void Start()
    {
        PlayerClickMovement.OnLeftClicked += EmptyCart;
        _subscription = Observable.EveryUpdate().Subscribe(x => RemoveFallenOutItems());
    }

    private void AddItemToCollections(ICartItem item, Collider col)
    {
        if (!_cartItems.Contains(item))
        {
            _cartItemAddTime[item] = DateTime.Now;
        }
        _cartItems.Add(item);
        _cartItemColliders.Add(item, col);
        _cartColliderItems.Add(col, item);
    }

    private void RemoveItemFromCollections(ICartItem item, Collider col)
    {
        _cartItemColliders.Remove(item);
        _cartColliderItems.Remove(col);
        _cartItemAddTime.Remove(item);
    }

    private void ClearItems()
    {
        _cartItems.Clear();
        _cartColliderItems.Clear();
        _cartItemColliders.Clear();
        _cartItemAddTime.Clear();
    }
    
    private void RemoveFallenOutItems()
    {
        if (_isEmptying)
        {
            return;
        }
        
        Vector3 center = boxCollider.transform.TransformPoint(boxCollider.center);
        Vector3 halfExtents = Vector3.Scale(boxCollider.size * 0.5f, boxCollider.transform.lossyScale);
        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, _colliders);

        _itemsInPhysicalBox.Clear();
        for (int i = 0; i < count; i++)
        {
            var coll = _colliders[i];
            if (_cartColliderItems.ContainsKey(coll))
            {
                _itemsInPhysicalBox.Add(coll);
            }
        }

        foreach (var col in _itemsInPhysicalBox)
        {
            if (_cartColliderItems.ContainsKey(col))
            {
                continue;
            }

            var item = col.GetComponentInParent<ICartItem>();
            AddItemToCollections(item, col);
        }
        
        _cartItems.RemoveWhere(item =>
        {
            var col = _cartItemColliders[item];
            bool toRemove = !_itemsInPhysicalBox.Contains(col) && (DateTime.Now - _cartItemAddTime[item]).TotalSeconds > 1;
            if (toRemove)
            {
                item.OnFallenOut();
                RemoveItemFromCollections(item, col);
            }

            return toRemove;
        });
    }

    private void OnDestroy()
    {
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        _subscription?.Dispose();
        
        PlayerClickMovement.OnLeftClicked -= EmptyCart;
    }

    private void EmptyCart()
    {
        if (_isEmptying)
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        EmptyAsync(_cancellationTokenSource.Token).Forget();
    }

    private async UniTask EmptyAsync(CancellationToken token)
    {
        _isEmptying = true;
        foreach (var item in _cartItems)
        {
            await UniTask.WaitForSeconds(0.1f, cancellationToken: token);
            item.Recycle();

            if (item is Trash trash)
            {
                _moneyController.AddMoney(_trashStats.Upgrades[trash.TrashType].ScoreForCurrentUpgrade);
            }
        }

        ClearItems();
        
        _isEmptying = false;

        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
    }

    public void PickUpItem(ICartItem cartItem, Collider col)
    {
        if (_isEmptying || _cartItems.Contains(cartItem) || _cartItems.Count >= maxItems)
        {
            return;
        }

        AddItemToCollections(cartItem, col);
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
}