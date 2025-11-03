using Eco.Scripts.Upgrades;
using UnityEngine;

namespace Eco.Scripts.Helpers
{
    [CreateAssetMenu(fileName = "Helper Upgrade", menuName = "Helper/Upgrade")]
    public class HelperUpgrade : Upgrade
    {
        [SerializeField] private float baseValue;
        [SerializeField] private float growth = 1.1f;
    
        public float Value {get; private set;}
    
        protected override void ApplyUpgrade(int level)
        {
            Value = baseValue + growth * level;
        }

        protected override void Load(int level)
        {
            base.Load(level);
            ApplyUpgrade(level);
        }
    
        public override string GetDescription(string locString)
        {
            return string.Format(locString, Value);
        }
    }
}
