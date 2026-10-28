using System;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades.Effects
{
    /// <summary>Turns on the combo: items of the same type in one load pay more the more of them there are.</summary>
    [Serializable]
    public class ComboUnlockEffect : UpgradeEffect
    {
        private static readonly LocalizedString Label = new(LocTableName, "effect-combo-unlock");

        public override void Apply(StatSheet stats, int level)
        {
            stats.ComboUnlocked = true;
        }

        public override string Describe(StatSheet current, StatSheet next)
        {
            return string.Format(Label.GetLocalizedString(), $"+x{current.ComboMultiplier:0.##}");
        }

        public override string EditorSummary => "Unlock combo";
    }
}
