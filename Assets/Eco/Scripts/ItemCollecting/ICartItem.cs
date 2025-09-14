using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.ItemCollecting
{
    public interface ICartItem
    {
        void OnPickUp(Transform parent);
        void OnFallenOut();
        void Recycle();
        void SetInCartState(bool inCart);
        void MakeKinematic(bool isKinematic);
        bool IsBeingPickedUp { get; set; }
        void SetPickedUpStatus(bool status);
        StyleColor GetColor();
        string GetName();
        int GetWeight();
    }
}