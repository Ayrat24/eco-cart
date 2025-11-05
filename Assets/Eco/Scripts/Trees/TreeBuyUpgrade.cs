using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;

namespace Eco.Scripts.Trees
{
    [CreateAssetMenu(menuName = "Upgrade/TreeBuyUpgrade")]
    public class TreeBuyUpgrade : Upgrade
    {
        [SerializeField] UnlockableUpgradeType upgradeType;
        [SerializeField] private int prefabId;
        [SerializeField] private TreeScoreUpgrade scoreUpgrade;
        [SerializeField] TreeIntervalUpgrade intervalUpgrade;
        
        public readonly Subject<int> OnPurchase = new();

        public TreeScoreUpgrade ScoreUpgrade => scoreUpgrade;
        public TreeIntervalUpgrade IntervalUpgrade => intervalUpgrade;

        protected override void ApplyUpgrade(int level)
        {
            if (level == 1)
            {
                UnlockTracker.UnlockUpgrade(upgradeType);
            }
            
            OnPurchase.OnNext(prefabId);
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