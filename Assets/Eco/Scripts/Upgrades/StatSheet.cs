using System.Collections.Generic;
using Eco.Scripts.Scrap;
using Eco.Scripts.Trees;

namespace Eco.Scripts.Upgrades
{
    /// <summary>
    /// Stat values while they are being worked out: starts at <see cref="BaseStats"/>, then every bought upgrade
    /// effect adds to it. <see cref="PlayerStats"/> publishes the result.
    /// </summary>
    public class StatSheet
    {
        public int MaxWeight;
        public int LimbCount;
        public float LimbRange;
        public float MoveSpeed;
        public float WetnessCapacity;
        public float WetnessFillSpeed;
        // Share of SlimeTrailSettings.Width, the trail's width before upgrades
        public float TrailWidthScale = 1f;
        public bool ComboUnlocked;
        public float ComboMultiplier;
        public readonly Dictionary<ScrapType, double> ScrapScores = new();
        public readonly HashSet<UnlockableUpgradeType> Unlocks = new();
        // Tree types whose fruit is unlocked, and how much faster it grows: 1 means twice as fast
        public readonly HashSet<TreeDefinition> FruitTrees = new();
        public readonly Dictionary<TreeDefinition, float> FruitSpeedBonus = new();

        public StatSheet(BaseStats baseStats)
        {
            MaxWeight = baseStats.MaxWeight;
            LimbCount = baseStats.LimbCount;
            LimbRange = baseStats.LimbRange;
            MoveSpeed = baseStats.MoveSpeed;
            WetnessCapacity = baseStats.WetnessCapacity;
            WetnessFillSpeed = baseStats.WetnessFillSpeed;
            ComboMultiplier = baseStats.ComboMultiplier;

            foreach (var scrap in baseStats.ScrapScores)
            {
                ScrapScores[scrap.type] = scrap.score;
            }
        }

        public double GetScrapScore(ScrapType type)
        {
            return ScrapScores.GetValueOrDefault(type, 0d);
        }

        /// <summary>Seconds to grow one fruit on this tree type.</summary>
        public float GetFruitInterval(TreeDefinition tree)
        {
            return tree.FruitInterval / (1f + FruitSpeedBonus.GetValueOrDefault(tree, 0f));
        }
    }
}
