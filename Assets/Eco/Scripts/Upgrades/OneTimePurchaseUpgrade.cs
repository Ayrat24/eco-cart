using LargeNumbers;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades
{
    public abstract class OneTimePurchaseUpgrade : Upgrade
    {
        [SerializeField] private LocalizedString alreadyPurchasedString;

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