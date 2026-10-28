using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades.Effects
{
    /// <summary>More scrap weight fits in the stomach.</summary>
    [Serializable]
    public class MaxWeightEffect : UpgradeEffect
    {
        private static readonly LocalizedString Label = new(LocTableName, "effect-max-weight");

        [SerializeField, Min(1)] private int weightPerLevel = 5;

        public override void Apply(StatSheet stats, int level)
        {
            stats.MaxWeight += weightPerLevel * level;
        }

        public override string Describe(StatSheet current, StatSheet next)
        {
            return Change(Label, current.MaxWeight.ToString(), next?.MaxWeight.ToString());
        }

        public override string EditorSummary => $"Max weight +{weightPerLevel}/lvl";
    }
}
