using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrade/DigPowerUpgrade")]
    public class DigPowerUpgrade : Upgrade
    {
        [SerializeField] private int basePower = 1;
        [SerializeField] private int powerPerLevel = 1;
        
        public int DigPower { get; private set; }
        
        protected override void Load(int level)
        {
            base.Load(level);
            ApplyUpgrade(level);
        }

        protected override void ApplyUpgrade(int level)
        {
            DigPower = basePower + level * powerPerLevel;
        }

        public override string GetDescription(string localizedString)
        {
            return string.Format(localizedString, DigPower);
        }
    }
}

