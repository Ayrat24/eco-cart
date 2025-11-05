using System;
using LargeNumbers;
using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrade/PileScoreUpgrade")]
    public class PileScoreUpgrade : Upgrade
    {
        [SerializeField] private int baseScore = 10;
        [SerializeField] private float scoreGrowth = 1.15f;
        
        public AlphabeticNotation ScoreForCurrentUpgrade { get; private set; }
        
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
            
            var score = baseScore + power;
            if (score.magnitude == 0)
            {
                score.coefficient = Math.Floor(score.coefficient);
            }
            
            ScoreForCurrentUpgrade = score;
        }

        public override string GetDescription(string localizedString)
        {
            return string.Format(localizedString, ScoreForCurrentUpgrade);
        }
    }
}

