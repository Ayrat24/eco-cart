using UnityEngine;

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
}