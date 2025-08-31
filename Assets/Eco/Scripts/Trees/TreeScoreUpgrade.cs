using Eco.Scripts.Upgrades;
using UnityEngine;

namespace Eco.Scripts.Trees
{
    [CreateAssetMenu(menuName = "Upgrade/Tree Score Upgrade")]
    public class TreeScoreUpgrade : Upgrade
    {
        [SerializeField] private int baseScore = 1;
        [SerializeField] private float growth = 1.1f;
    
        public int Score { get; private set; }
    
        protected override void ApplyUpgrade(int level)
        {
            Score = Mathf.FloorToInt(baseScore * Mathf.Pow(growth, level)) + level;
        }

        protected override void Load(int level)
        {
            base.Load(level);
            ApplyUpgrade(level);
        }

        public override string GetDescription()
        {
            return $"{base.GetDescription()} Score: {Score}";
        }
    }
}
