using System;
using Eco.Scripts.Upgrades;
using UnityEditor;
using UnityEngine;

namespace Eco.Scripts.EditorTools
{
    /// <summary>Deep copies of upgrade nodes: effects are cloned, not shared between the copies.</summary>
    internal static class UpgradeNodeCopier
    {
        public static UpgradeNode Copy(UpgradeTree tree, UpgradeNode source, Vector2 position)
        {
            var copy = new UpgradeNode(tree.MakeUniqueId(source.Id), position);
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(source), copy);
            copy.EditorId = tree.MakeUniqueId(source.Id);
            copy.EditorPosition = position;

            copy.EditorEffects.Clear();
            foreach (var effect in source.Effects)
            {
                if (effect == null)
                {
                    continue;
                }

                var effectCopy = (UpgradeEffect)Activator.CreateInstance(effect.GetType());
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(effect), effectCopy);
                copy.EditorEffects.Add(effectCopy);
            }

            return copy;
        }
    }
}
