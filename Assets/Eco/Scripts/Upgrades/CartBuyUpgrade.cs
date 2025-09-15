using System;
using Eco.Scripts.ItemCollecting;
using R3;
using UnityEngine;
using UnityEngine.Serialization;

namespace Eco.Scripts.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrade/CartBuyUpgrade")]
    public class CartBuyUpgrade : Upgrade
    {
        [SerializeField] private CartData cartData;
        
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
            return string.Format(localizedString, cartData.carryingCapacity, cartData.moveSpeed);
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
            public Vector3 playerPosition;
            public float pickupRadius;
            public float pickupRadiusDeadZone;
            public float menuCameraOffset;
            public Vector3 characterModelOffset;
        }
    }
}