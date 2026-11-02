using System;
using System.Collections.Generic;
using Eco.Scripts.Trees;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades.Effects
{
    /// <summary>Trees of one type grow their fruit faster.</summary>
    [Serializable]
    public class FruitSpeedEffect : UpgradeEffect
    {
        private static readonly LocalizedString Label = new(LocTableName, "effect-fruit-speed");

        [SerializeField] private TreeDefinition tree;
        [Tooltip("Growth speed added per level: 0.25 is a quarter on top of the tree's base speed.")]
        [SerializeField, Min(0f)] private float speedPerLevel = 0.25f;

        public TreeDefinition Tree => tree;

        public override void Apply(StatSheet stats, int level)
        {
            if (tree != null)
            {
                stats.FruitSpeedBonus[tree] = stats.FruitSpeedBonus.GetValueOrDefault(tree) + speedPerLevel * level;
            }
        }

        public override string Describe(StatSheet current, StatSheet next)
        {
            if (tree == null)
            {
                return "?";
            }

            // The label takes the tree name ({0}) and the seconds per fruit ({1})
            string value = current.GetFruitInterval(tree).ToString("0.#");
            if (next != null)
            {
                value = $"{value} → {next.GetFruitInterval(tree).ToString("0.#")}";
            }

            return string.Format(Label.GetLocalizedString(), tree.GetName(), value);
        }

        public override string EditorSummary =>
            $"{(tree != null ? tree.name : "(no tree)")} fruit speed +{speedPerLevel}/lvl";
    }
}
