using System;
using LargeNumbers;
using UnityEngine;
using R3;

namespace Eco.Scripts.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrade/PileScoreUpgrade")]
    public class PileScoreUpgrade : Upgrade
    {
        [SerializeField] private int baseScore = 10;
        [SerializeField] private float scoreGrowth = 1.15f;
        
        public AlphabeticNotation ScoreForCurrentUpgrade { get; private set; }
        
        private IDisposable _subscription;
        
        protected override void Load(int level)
        {
            base.Load(level);
            ApplyUpgrade(level);
            
            _subscription = UnlockTracker.OnUnlocked.Subscribe(OnUnlock);
        }

        private void OnUnlock(UnlockableUpgradeType unlockString)
        {
            if(unlockString is UnlockableUpgradeType.Flowers or UnlockableUpgradeType.Butterflies)
            {
                ApplyUpgrade(CurrentLevel.Value);
            }
        }

        protected override void ApplyUpgrade(int level)
        {
            var power = new AlphabeticNotation(scoreGrowth);
            for (int i = 0; i < CurrentLevel.Value; i++)
            {
                power *= scoreGrowth;
            }
            
            var score = baseScore + power;

            var flowersBonus = new AlphabeticNotation(0);
            if (UnlockTracker.IsUpgradeUnlocked(UnlockableUpgradeType.Flowers))
            {
                flowersBonus = score * 0.5f;
            }

            var butterfliesBonus = new AlphabeticNotation(0);
            if (UnlockTracker.IsUpgradeUnlocked(UnlockableUpgradeType.Butterflies))
            {
                butterfliesBonus = score;
            }
            
            score += flowersBonus;
            score += butterfliesBonus;
            
            ScoreForCurrentUpgrade = score;
        }

        public override string GetDescription(string localizedString)
        {
            return string.Format(localizedString, ScoreForCurrentUpgrade);
        }

        public override void Clear()
        {
            _subscription.Dispose();
            base.Clear();
        }
    }
}

