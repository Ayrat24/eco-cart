using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades.Effects
{
    /// <summary>The slime trail, and the grass that grows on it, is wider.</summary>
    [Serializable]
    public class TrailWidthEffect : UpgradeEffect
    {
        private static readonly LocalizedString Label = new(LocTableName, "effect-trail-width");

        [Tooltip("Share of the starting width added per level.")]
        [SerializeField, Min(0.01f)] private float widthPerLevel = 0.25f;

        public override void Apply(StatSheet stats, int level)
        {
            stats.TrailWidthScale += widthPerLevel * level;
        }

        public override string Describe(StatSheet current, StatSheet next)
        {
            return Change(Label, Format(current.TrailWidthScale), next != null ? Format(next.TrailWidthScale) : null);
        }

        public override string EditorSummary => $"Trail width +{widthPerLevel:0%}/lvl";

        private static string Format(float scale)
        {
            return $"{scale:0%}";
        }
    }
}
