using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    [CreateAssetMenu(fileName = "ComboMultiplierUpgrade", menuName = "Upgrade/ComboMultiplierUpgrade")]
    public class ComboMultiplierUpgrade : Upgrade
    {
        public float Multiplier { get; private set; }
        
        protected override void ApplyUpgrade(int level)
        {
            Multiplier += level * 0.10f;
        }

        protected override int CalculateCost()
        {
            return Mathf.FloorToInt(baseCost + Mathf.Pow(CurrentLevel.Value, CurrentLevel.Value) * baseCost);
        }

        protected override void Load(int level)
        {
            base.Load(level);
            Multiplier = 1;
            ApplyUpgrade(level);
        }
    }
}
