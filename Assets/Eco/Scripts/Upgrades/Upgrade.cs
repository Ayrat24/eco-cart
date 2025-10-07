using System;
using LargeNumbers;
using R3;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades
{
    public abstract class Upgrade : ScriptableObject
    {
        [SerializeField] protected int baseCost;
        [SerializeField] protected float costGrowth = 1.15f;
        [SerializeField] private UnlockableUpgradeType needsUpgrade = UnlockableUpgradeType.None;
        
        public string upgradeId;
        public LocalizedString upgradeLocalizedName;
        public LocalizedString upgradeLocalizedDescription;
        public Sprite icon;
    
        public AlphabeticNotation Cost { get; private set; }
        public bool Available { get; protected set; }
        
        public readonly ReactiveProperty<int> CurrentLevel = new(1);
    
        public void Init(int level)
        {
            Load(level);
        }
    
        public virtual void BuyUpgrade()
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
            Available = true;
        }

        public virtual string GetDescription(string localizedString)
        {
            return string.Format(localizedString, CurrentLevel.Value);
        }

        public virtual string GetButtonText()
        {
            return CalculateCost().ToString();
        }

        public virtual void Clear() { }
    }
}