using System;
using Eco.Scripts.ItemCollecting;
using R3;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrade/CartBuyUpgrade")]
    public class CartBuyUpgrade : SelectableUpgrade
    {
        [SerializeField] private LocalizedString descriptionStats = new("CartUpgrades", "cart-tab-stats");
        [SerializeField] private CartData cartData;
        
        protected override string SelectableGroupId => "cart-buy-upgrade";
        public readonly Subject<CartData> OnCartSelected = new();

        protected override void ApplyUpgrade(int level)
        {
            OnCartSelected.OnNext(GetCartData());
        }

        public CartData GetCartData()
        {
            cartData.id = upgradeId;
            return cartData;
        }

        public override string GetDescription(string localizedString)
        {
            return $"{localizedString}\n{string.Format(descriptionStats.GetLocalizedString(), cartData.carryingCapacity, cartData.moveSpeed)}";
        }

        [Serializable]
        public struct CartData
        {
            public Cart prefab;
            public Vector3 offset;
            [HideInInspector] public string id;
            public int carryingCapacity;
            public int moveSpeed;
            public Vector3 cameraOffset;
            public float menuCameraOffset;
            public Vector3 characterModelOffset;
        }
    }
}