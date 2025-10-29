using LargeNumbers;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrade/UnlockableUpgrade")]
    public class UnlockableUpgrade : Upgrade
    {
        [SerializeField] private LocalizedString alreadyPurchasedString;
        [SerializeField] private UnlockableUpgradeType upgradeType;
        
        public bool Purchased => CurrentLevel.Value > 0;

        protected override void Load(int level)
        {
            base.Load(level);

            if (Purchased)
            {
                Available = false;
            }
            
            UnlockTracker.AddUnlockableUpgrade(upgradeType, Purchased);
        }

        protected override void ApplyUpgrade(int level)
        {
            Available = false;
            UnlockTracker.UnlockUpgrade(upgradeType);
        }

        protected override AlphabeticNotation CalculateCost()
        {
            return new AlphabeticNotation(baseCost);
        }

        public override string GetButtonText()
        {
            return CurrentLevel.Value == 0 ? base.GetButtonText() : alreadyPurchasedString.GetLocalizedString();
        }
    }
}