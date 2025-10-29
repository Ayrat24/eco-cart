using System.Collections.Generic;
using LargeNumbers;
using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    public class ScoreStats
    {
        private readonly UpgradesCollection _upgradesCollection;
        
        private Dictionary<TrashType, TrashScoreUpgrade> _trashUpgrades;
        private ComboMultiplierUpgrade _combo;
        
        public ScoreStats(UpgradesCollection upgradesCollection)
        {
            _upgradesCollection = upgradesCollection;
        }

        public void Init()
        {
            _trashUpgrades = _upgradesCollection.TrashScoreUpgrades;
            _combo = _upgradesCollection.GetUpgradeType<ComboMultiplierUpgrade>();
        }

        public AlphabeticNotation GetScoreForTrash(TrashType type)
        {
            if (_trashUpgrades.TryGetValue(type, out var value))
            {
                return value.ScoreForCurrentUpgrade;
            }
            
            return new AlphabeticNotation(0);
        }
        
        public float GetComboMultiplier()
        {
            return _combo.Multiplier;
        }

        public Color GetColor(TrashType type)
        {
            if (_trashUpgrades.TryGetValue(type, out var t))
                return t.Color;
            return Color.white;
        }
    }
}
