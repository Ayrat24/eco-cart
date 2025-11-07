using LargeNumbers;
using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    [CreateAssetMenu(fileName = "ComboMultiplierUpgrade", menuName = "Upgrade/ComboMultiplierUpgrade")]
    public class ComboMultiplierUpgrade : Upgrade
    {
        public float Multiplier { get; private set; }
        
        protected override void ApplyUpgrade(int level)
        {
            Multiplier += level * 0.50f;
        }

        // protected override AlphabeticNotation CalculateCost()
        // {
        //     var power = new AlphabeticNotation(costGrowth);
        //     for (int i = 0; i < CurrentLevel.Value; i++)
        //     {
        //         power *= costGrowth;
        //     }
        //     
        //     return baseCost * power;
        // }

        protected override void Load(int level)
        {
            base.Load(level);
            Multiplier = 0;
            ApplyUpgrade(level);
        }

        public override string GetDescription(string localizedString)
        {
            return string.Format(localizedString, Multiplier);
        }
    }
}
