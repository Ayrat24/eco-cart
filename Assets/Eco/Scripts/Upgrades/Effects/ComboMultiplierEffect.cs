using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades.Effects
{
    /// <summary>Raises the combo bonus each further item of the same type adds.</summary>
    [Serializable]
    public class ComboMultiplierEffect : UpgradeEffect
    {
        private static readonly LocalizedString Label = new(LocTableName, "effect-combo-multiplier");

        [SerializeField, Min(0.01f)] private float multiplierPerLevel = 0.25f;

        public override void Apply(StatSheet stats, int level)
        {
            stats.ComboMultiplier += multiplierPerLevel * level;
        }

        public override string Describe(StatSheet current, StatSheet next)
        {
            return Change(Label, Format(current.ComboMultiplier),
                next != null ? Format(next.ComboMultiplier) : null);
        }

        public override string EditorSummary => $"Combo +{multiplierPerLevel}/lvl";

        private static string Format(float multiplier)
        {
            return $"+x{multiplier:0.##}";
        }
    }
}
