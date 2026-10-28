using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades.Effects
{
    /// <summary>The jelly moves faster.</summary>
    [Serializable]
    public class MoveSpeedEffect : UpgradeEffect
    {
        private static readonly LocalizedString Label = new(LocTableName, "effect-move-speed");

        [Tooltip("Metres per second added per level.")]
        [SerializeField, Min(0.01f)] private float speedPerLevel = 1f;

        public override void Apply(StatSheet stats, int level)
        {
            stats.MoveSpeed += speedPerLevel * level;
        }

        public override string Describe(StatSheet current, StatSheet next)
        {
            return Change(Label, Format(current.MoveSpeed), next != null ? Format(next.MoveSpeed) : null);
        }

        public override string EditorSummary => $"Speed +{speedPerLevel}/lvl";

        private static string Format(float speed)
        {
            return $"{speed:0.#}";
        }
    }
}
