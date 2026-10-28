using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades.Effects
{
    /// <summary>
    /// Sets any other <see cref="UnlockTracker"/> flag: the map, the spade, flowers and butterflies on cleared piles.
    /// Trees and pets have their own effects.
    /// </summary>
    [Serializable]
    public class UnlockFeatureEffect : UpgradeEffect
    {
        private static readonly LocalizedString Label = new(LocTableName, "effect-unlock-feature");

        [SerializeField] private UnlockableUpgradeType feature = UnlockableUpgradeType.Map;

        public override void Apply(StatSheet stats, int level)
        {
            if (feature != UnlockableUpgradeType.None)
            {
                stats.Unlocks.Add(feature);
            }
        }

        public override string Describe(StatSheet current, StatSheet next)
        {
            var name = new LocalizedString(LocTableName, $"feature-{feature.ToString().ToLowerInvariant()}");
            return string.Format(Label.GetLocalizedString(), name.GetLocalizedString());
        }

        public override string EditorSummary => $"Unlock {feature}";
    }
}
