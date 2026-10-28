using System;
using Eco.Scripts.Trees;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades.Effects
{
    /// <summary>Lets a tree type be planted.</summary>
    [Serializable]
    public class UnlockTreeEffect : UpgradeEffect
    {
        private static readonly LocalizedString Label = new(LocTableName, "effect-unlock-tree");

        [SerializeField] private TreeDefinition tree;

        public TreeDefinition Tree => tree;

        public override void Apply(StatSheet stats, int level)
        {
            if (tree != null)
            {
                stats.Unlocks.Add(tree.UnlockType);
            }
        }

        public override string Describe(StatSheet current, StatSheet next)
        {
            return string.Format(Label.GetLocalizedString(), tree != null ? tree.GetName() : "?");
        }

        public override string EditorSummary => $"Unlock {(tree != null ? tree.name : "(no tree)")}";
    }
}
