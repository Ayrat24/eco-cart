using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades.Effects
{
    /// <summary>The jelly's water bank holds more, for a longer slime trail.</summary>
    [Serializable]
    public class WetnessCapacityEffect : UpgradeEffect
    {
        private static readonly LocalizedString Label = new(LocTableName, "effect-wetness-capacity");

        [Tooltip("Metres of trail added to the bank per level.")]
        [SerializeField, Min(1f)] private float metresPerLevel = 50f;

        public override void Apply(StatSheet stats, int level)
        {
            stats.WetnessCapacity += metresPerLevel * level;
        }

        public override string Describe(StatSheet current, StatSheet next)
        {
            return Change(Label, Format(current.WetnessCapacity), next != null ? Format(next.WetnessCapacity) : null);
        }

        public override string EditorSummary => $"Water bank +{metresPerLevel}m/lvl";

        private static string Format(float metres)
        {
            return $"{metres:0.#} m";
        }
    }
}
