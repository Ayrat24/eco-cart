using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades.Effects
{
    /// <summary>More tentacles grab scrap at the same time.</summary>
    [Serializable]
    public class LimbCountEffect : UpgradeEffect
    {
        private static readonly LocalizedString Label = new(LocTableName, "effect-limb-count");

        [SerializeField, Min(1)] private int limbsPerLevel = 1;

        public override void Apply(StatSheet stats, int level)
        {
            stats.LimbCount += limbsPerLevel * level;
        }

        public override string Describe(StatSheet current, StatSheet next)
        {
            return Change(Label, current.LimbCount.ToString(), next?.LimbCount.ToString());
        }

        public override string EditorSummary => $"Limbs +{limbsPerLevel}/lvl";
    }
}
