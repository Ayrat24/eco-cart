using System;
using Eco.Scripts.Trees;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades.Effects
{
    /// <summary>Trees of one type start growing fruit.</summary>
    [Serializable]
    public class UnlockFruitEffect : UpgradeEffect
    {
        private static readonly LocalizedString Label = new(LocTableName, "effect-unlock-fruit");

        [SerializeField] private TreeDefinition tree;

        public TreeDefinition Tree => tree;

        public override void Apply(StatSheet stats, int level)
        {
            if (tree != null)
            {
                stats.FruitTrees.Add(tree);
            }
        }

        public override string Describe(StatSheet current, StatSheet next)
        {
            if (tree == null)
            {
                return "?";
            }

            // The label takes the tree name ({0}), seconds per fruit ({1}) and the most fruit on a tree ({2})
            return string.Format(Label.GetLocalizedString(), tree.GetName(), current.GetFruitInterval(tree).ToString("0.#"),
                tree.MaxFruits);
        }

        public override string EditorSummary => $"Fruit on {(tree != null ? tree.name : "(no tree)")}";
    }
}
