using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemCollector : MonoBehaviour
{
    [SerializeField] Cart cart;


    private void OnTriggerEnter(Collider other)
    {
        var item = other.GetComponent<ICartItem>();
        if (item != null)
        {
            cart.PickUpItem(item);
        }
    }

    // private List<ICartItem> cartItems = new List<ICartItem>();
    //
    // private void OnTriggerEnter(Collider other)
    // {
    //     if (!other.TryGetComponent(out ICartItem cartItem))
    //     {
    //         return;
    //     }
    //     
    //     cartItems.Add(cartItem);
    //     cartItem.OnPickUp();
    // }
}