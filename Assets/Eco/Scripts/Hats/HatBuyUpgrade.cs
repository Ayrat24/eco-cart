using System;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;

namespace Eco.Scripts.Hats
{
    [CreateAssetMenu(menuName = "Upgrade/HatBuyUpgrade")]
    public class HatBuyUpgrade : SelectableUpgrade
    {
        [SerializeField] private HatData hatData;
        public readonly Subject<HatData> OnSelected = new();
        
        protected override void ApplyUpgrade(int level)
        {
            OnSelected.OnNext(GetHatData());
        }

        protected override string SelectableGroupId => "hats";

        public HatData GetHatData()
        {
            return hatData;
        }

        [Serializable]
        public class HatData
        {
            public Hat prefab;
            public Vector3 positionOffset;
        }
    }
}
