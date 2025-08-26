using Eco.Scripts.ItemCollecting;
using R3;
using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrade/CartBuyUpgrade")]
    public class CartBuyUpgrade : Upgrade
    {
        [SerializeField] private Cart cartPrefab;
        [SerializeField] private Vector3 offset;
        
        public readonly Subject<CartData> OnCartSelected = new();

        protected override void ApplyUpgrade(int level)
        {
            OnCartSelected.OnNext(GetCartData());
        }

        public CartData GetCartData()
        {
            return new CartData(upgradeName, cartPrefab, offset);
        }
        
        public struct CartData
        {
            public Cart Prefab;
            public Vector3 Offset;
            public string Id;

            public CartData(string id, Cart prefab, Vector3 offset)
            {
                Prefab = prefab;
                Offset = offset;
                Id = id;
            }
        }
    }
}