using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades.Effects
{
    /// <summary>Tentacles reach scrap further away.</summary>
    [Serializable]
    public class LimbRangeEffect : UpgradeEffect
    {
        private static readonly LocalizedString Label = new(LocTableName, "effect-limb-range");

        [Tooltip("Metres added to the reach per level.")]
        [SerializeField, Min(0.01f)] private float rangePerLevel = 0.5f;

        public override void Apply(StatSheet stats, int level)
        {
            stats.LimbRange += rangePerLevel * level;
        }

        public override string Describe(StatSheet current, StatSheet next)
        {
            return Change(Label, Format(current.LimbRange), next != null ? Format(next.LimbRange) : null);
        }

        public override string EditorSummary => $"Range +{rangePerLevel}m/lvl";

        private static string Format(float range)
        {
            return $"{range:0.#} m";
        }
    }
}
