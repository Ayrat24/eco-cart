using System.Collections.Generic;
using Eco.Scripts.Scrap;
using LargeNumbers;
using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    public class ScoreStats
    {
        private readonly UpgradesCollection _upgradesCollection;
        
        private Dictionary<ScrapType, ScrapScoreUpgrade> _scrapUpgrades;
        private ComboMultiplierUpgrade _combo;
        
        public ScoreStats(UpgradesCollection upgradesCollection)
        {
            _upgradesCollection = upgradesCollection;
        }

        public void Init()
        {
            _scrapUpgrades = _upgradesCollection.ScrapScoreUpgrades;
            _combo = _upgradesCollection.GetUpgradeType<ComboMultiplierUpgrade>();
        }

        public AlphabeticNotation GetScoreForScrap(ScrapType type)
        {
            if (_scrapUpgrades.TryGetValue(type, out var value))
            {
                return value.ScoreForCurrentUpgrade;
            }
            
            return new AlphabeticNotation(0);
        }
        
        public float GetComboMultiplier()
        {
            return _combo.Multiplier;
        }

        public Color GetColor(ScrapType type)
        {
            if (_scrapUpgrades.TryGetValue(type, out var t))
                return t.Color;
            return Color.white;
        }
    }
}
