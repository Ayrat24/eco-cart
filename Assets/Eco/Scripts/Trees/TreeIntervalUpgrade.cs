using Eco.Scripts.Upgrades;
using UnityEngine;

namespace Eco.Scripts.Trees
{
    [CreateAssetMenu(menuName = "Upgrade/Tree Interval Upgrade")]
    public class TreeIntervalUpgrade : Upgrade
    {
        [SerializeField] private float baseInterval = 5;
        [SerializeField] private float minInterval = 0.5f;
        [SerializeField] private float scale = 0.1f;
    
        public float Interval { get; private set; }
    
        protected override void ApplyUpgrade(int level)
        {
            Interval = minInterval + (baseInterval - minInterval) / (1f + level * scale); 
        }

        protected override void Load(int level)
        {
            base.Load(level);
            ApplyUpgrade(level);
        }

        public override string GetDescription()
        {
            return $"{base.GetDescription()} Interval: {Interval}";
        }
    }
}
