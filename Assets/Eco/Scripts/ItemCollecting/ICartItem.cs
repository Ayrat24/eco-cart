using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.ItemCollecting
{
    public interface ICartItem
    {
        void OnPickUp(Transform parent);
        void Recycle();
        UniTask RecycleAsync();
        void Dissolve();
        // Moves the item's renderers to a layer until it next spawns from its pool
        void SetRendererLayer(int layer);
        void SetInCartState(bool inCart);
        void MakeKinematic(bool isKinematic);
        bool IsBeingPickedUp { get; set; }
        void SetPickedUpStatus(bool status);
        StyleColor GetColor();
        string GetName();
        int GetWeight();
    }
}