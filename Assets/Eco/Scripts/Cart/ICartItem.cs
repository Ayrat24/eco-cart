using UnityEngine;

namespace Eco.Scripts.Cart
{
    public interface ICartItem
    {
        public void OnPickUp(Transform parent);
        public void OnFallenOut();
        public void Recycle();
        public void SetInCartState(bool inCart);
        public void MakeKinematic(bool isKinematic);
        public bool IsBeingPickedUp();
        public void SetPickedUpStatus(bool status);
    }
}