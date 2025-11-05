using System.Collections.Generic;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;

namespace Eco.Scripts.Helpers
{
    [CreateAssetMenu(fileName = "Helper Buy", menuName = "Helper/Buy")]
    public class HelperBuyUpgrade : SelectableUpgrade
    {
        [SerializeField] UnlockableUpgradeType upgradeType;
        [SerializeField] private Helper prefab;
        [SerializeField] List<HelperUpgrade> upgrades;
        
        public readonly Subject<HelperBuyUpgrade> OnPurchase = new();
        
        public List<HelperUpgrade> Upgrades => upgrades;
        public Helper GetPrefab() => prefab;

        protected override string SelectableGroupId => "helpers";

        protected override void ApplyUpgrade(int level)
        {
            if (level == 1)
            {
                UnlockTracker.UnlockUpgrade(upgradeType);
            }
            
            OnPurchase.OnNext(this);
        }
        
        protected override void Load(int level)
        {
            base.Load(level);
            
            if (level > 0)
            {
                UnlockTracker.UnlockUpgrade(upgradeType);
            }
        }
    }
}