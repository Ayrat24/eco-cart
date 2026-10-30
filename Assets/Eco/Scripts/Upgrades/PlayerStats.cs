using System.Collections.Generic;
using Eco.Scripts.Scrap;
using LargeNumbers;
using R3;
using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    /// <summary>
    /// The jelly's current stats: <see cref="BaseStats"/> plus every bought upgrade. <see cref="UpgradeManager"/>
    /// recalculates them on load and after each purchase; gameplay reads or subscribes to them here.
    /// </summary>
    public class PlayerStats
    {
        private readonly BaseStats _baseStats;

        private readonly ReactiveProperty<int> _maxWeight = new();
        private readonly ReactiveProperty<int> _limbCount = new();
        private readonly ReactiveProperty<float> _limbRange = new();
        private readonly ReactiveProperty<float> _moveSpeed = new();
        private readonly ReactiveProperty<float> _wetnessCapacity = new();
        private readonly ReactiveProperty<float> _wetnessFillSpeed = new();
        private readonly ReactiveProperty<float> _trailWidthScale = new();
        private readonly ReactiveProperty<bool> _comboUnlocked = new();
        private readonly ReactiveProperty<float> _comboMultiplier = new();
        private readonly Dictionary<ScrapType, AlphabeticNotation> _scrapScores = new();
        private readonly Dictionary<ScrapType, Color> _scrapColors = new();
        // Unlocks that came from upgrades, so a refund can take them back
        private readonly HashSet<UnlockableUpgradeType> _grantedUnlocks = new();

        public ReadOnlyReactiveProperty<int> MaxWeight => _maxWeight;
        public ReadOnlyReactiveProperty<int> LimbCount => _limbCount;
        public ReadOnlyReactiveProperty<float> LimbRange => _limbRange;
        public ReadOnlyReactiveProperty<float> MoveSpeed => _moveSpeed;
        public ReadOnlyReactiveProperty<float> WetnessCapacity => _wetnessCapacity;
        public ReadOnlyReactiveProperty<float> WetnessFillSpeed => _wetnessFillSpeed;
        public ReadOnlyReactiveProperty<float> TrailWidthScale => _trailWidthScale;
        public ReadOnlyReactiveProperty<bool> ComboUnlocked => _comboUnlocked;
        public ReadOnlyReactiveProperty<float> ComboMultiplier => _comboMultiplier;

        public int DigPower => _baseStats.DigPower;

        // Unlock bonuses are read on access: they can be bought after the pile chunk loads
        public AlphabeticNotation PileScore
        {
            get
            {
                float score = _baseStats.PileScore;
                float bonus = 0f;
                if (UnlockTracker.IsUpgradeUnlocked(UnlockableUpgradeType.Flowers))
                {
                    bonus += _baseStats.FlowersBonus;
                }

                if (UnlockTracker.IsUpgradeUnlocked(UnlockableUpgradeType.Butterflies))
                {
                    bonus += _baseStats.ButterfliesBonus;
                }

                return new AlphabeticNotation(score * (1f + bonus));
            }
        }

        public PlayerStats(BaseStats baseStats)
        {
            _baseStats = baseStats;

            foreach (var scrap in baseStats.ScrapScores)
            {
                _scrapColors[scrap.type] = scrap.color;
            }

            Apply(new StatSheet(baseStats), false);
        }

        public StatSheet CreateBaseSheet()
        {
            return new StatSheet(_baseStats);
        }

        /// <summary>
        /// Publishes worked-out stats. New unlocks go to <see cref="UnlockTracker"/>; with
        /// <paramref name="announceUnlocks"/> they also fire <see cref="UnlockTracker.OnUnlocked"/>.
        /// Unlocks no longer in the sheet (the upgrade was refunded) are locked again.
        /// </summary>
        public void Apply(StatSheet sheet, bool announceUnlocks)
        {
            _maxWeight.Value = sheet.MaxWeight;
            _limbCount.Value = sheet.LimbCount;
            _limbRange.Value = sheet.LimbRange;
            _moveSpeed.Value = sheet.MoveSpeed;
            _wetnessCapacity.Value = sheet.WetnessCapacity;
            _wetnessFillSpeed.Value = sheet.WetnessFillSpeed;
            _trailWidthScale.Value = sheet.TrailWidthScale;
            _comboUnlocked.Value = sheet.ComboUnlocked;
            _comboMultiplier.Value = sheet.ComboMultiplier;

            _scrapScores.Clear();
            foreach (var pair in sheet.ScrapScores)
            {
                _scrapScores[pair.Key] = new AlphabeticNotation(pair.Value);
            }

            foreach (var unlock in _grantedUnlocks)
            {
                if (!sheet.Unlocks.Contains(unlock))
                {
                    UnlockTracker.LockUpgrade(unlock);
                }
            }

            _grantedUnlocks.Clear();
            _grantedUnlocks.UnionWith(sheet.Unlocks);

            foreach (var unlock in sheet.Unlocks)
            {
                if (UnlockTracker.IsUpgradeUnlocked(unlock))
                {
                    continue;
                }

                if (announceUnlocks)
                {
                    UnlockTracker.UnlockUpgrade(unlock);
                }
                else
                {
                    UnlockTracker.AddUnlockableUpgrade(unlock, true);
                }
            }
        }

        public AlphabeticNotation GetScoreForScrap(ScrapType type)
        {
            return _scrapScores.TryGetValue(type, out var score) ? score : new AlphabeticNotation(0);
        }

        /// <summary>Combo bonus per earlier item of the same type in a load; 0 until the combo is unlocked.</summary>
        public float GetComboMultiplier()
        {
            return _comboUnlocked.Value ? _comboMultiplier.Value : 0f;
        }

        public Color GetColor(ScrapType type)
        {
            return _scrapColors.GetValueOrDefault(type, Color.white);
        }
    }
}
