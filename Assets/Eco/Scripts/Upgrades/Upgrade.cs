using R3;
using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    public abstract class Upgrade : ScriptableObject
    {
        [SerializeField] protected int baseCost;
        [SerializeField] protected float costGrowth = 1.15f;
        [SerializeField] protected string description;
        
        public string upgradeName;
    
        public Sprite icon;
    
        private int cost;
        public int Cost => cost;
    
        public ReactiveProperty<int> CurrentLevel = new(1);
    
        public void Init(int level)
        {
            Load(level);
        }
    
        public void BuyUpgrade()
        {
            CurrentLevel.Value += 1;
            cost = CalculateCost();
            ApplyUpgrade(CurrentLevel.Value);
        }

        protected virtual int CalculateCost()
        {
            return Mathf.FloorToInt(baseCost * Mathf.Pow(costGrowth, CurrentLevel.Value));
        }

        protected abstract void ApplyUpgrade(int level);
        
        protected virtual void Load(int level)
        {
            CurrentLevel.Value = level;
            cost = CalculateCost();
        }

        public virtual string GetDescription()
        {
            return $"{description} Level: {CurrentLevel.Value}.";
        }
    }
}