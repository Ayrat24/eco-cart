using LargeNumbers;
using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrade/PileScoreUpgrade")]
    public class PileScoreUpgrade : Upgrade
    {
        [SerializeField] private int baseScore = 10;
        [SerializeField] private float scoreGrowth = 1.15f;
        [SerializeField] private float flowersBonus = 0.5f;
        [SerializeField] private float butterfliesBonus = 1f;

        private AlphabeticNotation _score;

        // Unlock bonuses are read on access: the unlocks may load after this upgrade or be bought later
        public AlphabeticNotation ScoreForCurrentUpgrade
        {
            get
            {
                var score = _score;
                if (UnlockTracker.IsUpgradeUnlocked(UnlockableUpgradeType.Flowers))
                {
                    score += _score * flowersBonus;
                }

                if (UnlockTracker.IsUpgradeUnlocked(UnlockableUpgradeType.Butterflies))
                {
                    score += _score * butterfliesBonus;
                }

                return score;
            }
        }

        protected override void Load(int level)
        {
            base.Load(level);
            ApplyUpgrade(level);
        }

        protected override void ApplyUpgrade(int level)
        {
            var power = new AlphabeticNotation(scoreGrowth);
            for (int i = 0; i < CurrentLevel.Value; i++)
            {
                power *= scoreGrowth;
            }

            _score = baseScore * power;
        }

        public override string GetDescription(string localizedString)
        {
            return string.Format(localizedString, ScoreForCurrentUpgrade);
        }
    }
}
