using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

public class Cart : MonoBehaviour
{
    [SerializeField] private int maxItems = 10;
    [SerializeField] float dropOffsetMaxValue = 0.2f;
    public Transform dropPoint;
    private readonly HashSet<ICartItem> _cartItems = new ();

    private bool _isEmptying;
    private CancellationTokenSource _cancellationTokenSource;
    private TrashStats _trashStats;
    private MoneyController _moneyController;
    
    [Inject]
    public void Initialize(TrashStats trashStats, MoneyController moneyController)
    {
        _trashStats = trashStats;
        _moneyController = moneyController;
    }
    
    private void Start()
    {
        PlayerClickMovement.OnLeftClicked += EmptyCart;
    }

    private void OnDestroy()
    {
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        
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
        
        _cartItems.Clear();
        _isEmptying = false;
        
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
    }

    public void PickUpItem(ICartItem cartItem)
    {
        if (_isEmptying || _cartItems.Contains(cartItem) || _cartItems.Count >= maxItems)
        {
            return;
        }

        Vector3 offset = new Vector3(Random.Range(0, dropOffsetMaxValue), 0, Random.Range(0, dropOffsetMaxValue));
        cartItem.OnPickUp(dropPoint, offset);
        _cartItems.Add(cartItem);
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out ICartItem cartItem))
        {
            return;
        }
        
        cartItem.OnFallenOut();
        _cartItems.Remove(cartItem);
    }
}
