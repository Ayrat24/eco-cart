using System;
using LargeNumbers;
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
    
        public AlphabeticNotation Cost { get; private set; }
    
        public ReactiveProperty<int> CurrentLevel = new(1);
    
        public void Init(int level)
        {
            Load(level);
        }
    
        public void BuyUpgrade()
        {
            CurrentLevel.Value += 1;
            Cost = CalculateCost();
            ApplyUpgrade(CurrentLevel.Value);
        }

        protected virtual AlphabeticNotation CalculateCost()
        {
            var power = new AlphabeticNotation(costGrowth);
            for (int i = 0; i < CurrentLevel.Value; i++)
            {
                power *= costGrowth;
            }

            var cost = baseCost * power;
            if (cost.magnitude == 0)
            {
                cost.coefficient = Math.Ceiling(cost.coefficient);
            }
            
            return cost;
        }

        protected abstract void ApplyUpgrade(int level);
        
        protected virtual void Load(int level)
        {
            CurrentLevel.Value = level;
            Cost = CalculateCost();
        }

        public virtual string GetDescription()
        {
            return $"{description} Level: {CurrentLevel.Value}.";
        }
    }
}