using System;
using LargeNumbers;
using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrade/TrashScoreUpgrade")]
    public class TrashScoreUpgrade : Upgrade
    {
        [SerializeField] private int baseScore;
        [SerializeField] private float scoreGrowth = 1.1f;
        [SerializeField] private Color color;
        public TrashType trashType;
        public AlphabeticNotation ScoreForCurrentUpgrade { get; private set; }
        public Color Color => color;
        
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
    }
}