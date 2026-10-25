using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    [CreateAssetMenu(fileName = "ComboMultiplierUpgrade", menuName = "Upgrade/ComboMultiplierUpgrade")]
    public class ComboMultiplierUpgrade : Upgrade
    {
        [SerializeField] private float multiplierPerLevel = 0.5f;

        public float Multiplier { get; private set; }

        protected override void ApplyUpgrade(int level)
        {
            Multiplier = level * multiplierPerLevel;
        }

        protected override void Load(int level)
        {
            base.Load(level);
            ApplyUpgrade(level);
        }

        public override string GetDescription(string localizedString)
        {
            return string.Format(localizedString, Multiplier);
        }
    }
}
