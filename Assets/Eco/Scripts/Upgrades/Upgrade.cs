using R3;
using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    public abstract class Upgrade : ScriptableObject
    {
        [SerializeField] protected int baseCost;
        
        public string upgradeName;
    
        public Sprite icon;
        public string description;
    
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
            return baseCost + (CurrentLevel.Value * baseCost) * 2;
        }

        protected abstract void ApplyUpgrade(int level);
        
        protected virtual void Load(int level)
        {
            CurrentLevel.Value = level;
            cost = CalculateCost();
        }
    }
}