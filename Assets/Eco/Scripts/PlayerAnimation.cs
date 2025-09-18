using System;
using Eco.Scripts;
using Eco.Scripts.ItemCollecting;
using UnityEngine;
using R3;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Animator animator;

    private IDisposable _playerSubscription;
    private IDisposable _cartSubscription;

    private static readonly int Recycle = Animator.StringToHash("recycling");
    
    private void Start()
    {
        _playerSubscription = player.OnCartChanged.Subscribe(OnCartChanged);
    }
    
    private void OnCartChanged(Cart cart)
    {
        _cartSubscription?.Dispose();
        _cartSubscription = cart.OnStatusChanged.Subscribe(OnCartStatusChanged);
    }

    private void OnCartStatusChanged(Cart.CartState state)
    {
        animator.SetBool(Recycle, state == Cart.CartState.Recycling);
    }

    private void OnDestroy()
    {
        _cartSubscription?.Dispose();
        _playerSubscription?.Dispose();
    }
}
