using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades.Effects
{
    /// <summary>The jelly's water bank fills faster in the water.</summary>
    [Serializable]
    public class WetnessFillSpeedEffect : UpgradeEffect
    {
        private static readonly LocalizedString Label = new(LocTableName, "effect-wetness-fill-speed");

        [Tooltip("Metres of trail per second added per level.")]
        [SerializeField, Min(0.1f)] private float speedPerLevel = 15f;

        public override void Apply(StatSheet stats, int level)
        {
            stats.WetnessFillSpeed += speedPerLevel * level;
        }

        public override string Describe(StatSheet current, StatSheet next)
        {
            return Change(Label, Format(current.WetnessFillSpeed), next != null ? Format(next.WetnessFillSpeed) : null);
        }

        public override string EditorSummary => $"Fill speed +{speedPerLevel}m/s/lvl";

        private static string Format(float speed)
        {
            return $"{speed:0.#} m/s";
        }
    }
}
