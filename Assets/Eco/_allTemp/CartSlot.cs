using System.Collections.Generic;
using UnityEngine;

public class CartSlot : MonoBehaviour
{
    [SerializeField] private int maxStackCount = 1;
    private readonly List<ICartItem> _cartItems = new();

    public bool HasSpace => _cartItems.Count < maxStackCount;
    
    public void PlaceItem(ICartItem cartItem)
    {
        if (!HasSpace)
        {
            Debug.LogError("Can't place a cart item. Slot is full");
            return;
        }
        
        _cartItems.Add(cartItem);
    }
}