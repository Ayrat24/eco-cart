using Eco.Scripts.Upgrades;
using LargeNumbers;
using UnityEngine;

namespace Eco.Scripts.Trees
{
    [CreateAssetMenu(menuName = "Upgrade/Tree Score Upgrade")]
    public class TreeScoreUpgrade : Upgrade
    {
        [SerializeField] private int baseScore = 1;
        [SerializeField] private float growth = 1.1f;
    
        public LargeNumber Score { get; private set; }
    
        protected override void ApplyUpgrade(int level)
        {
            var power = new LargeNumber(costGrowth);
            for (int i = 0; i < CurrentLevel.Value; i++)
            {
                power *= costGrowth;
            }
            
            Score = baseScore * power + level;
        }

        protected override void Load(int level)
        {
            base.Load(level);
            ApplyUpgrade(level);
        }

        public override string GetDescription(string locString)
        {
            return $"{locString} {Score}";
        }
    }
}
