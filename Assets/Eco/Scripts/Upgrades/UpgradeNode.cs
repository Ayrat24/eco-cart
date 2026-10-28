using System;
using System.Collections.Generic;
using LargeNumbers;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades
{
    /// <summary>
    /// One upgrade in the <see cref="UpgradeTree"/>. Buying it raises its level up to <see cref="MaxLevel"/>;
    /// every effect is applied with the current level. Nodes that require it become buyable once it has a level.
    /// </summary>
    [Serializable]
    public class UpgradeNode
    {
        [Tooltip("Save key. Changing it resets this upgrade in existing saves.")]
        [SerializeField] private string id;
        [SerializeField] private LocalizedString title = new();
        [SerializeField] private LocalizedString description = new();
        [SerializeField] private Sprite icon;
        [Tooltip("Center of the node in the menu, in pixels at zoom 1. Y points down.")]
        [SerializeField] private Vector2 position;
        [SerializeField, Min(1)] private int maxLevel = 1;
        [Tooltip("Price of the first level.")]
        [SerializeField] private AlphabeticNotation baseCost = new(10);
        [Tooltip("Each level costs this many times the previous one.")]
        [SerializeField, Min(1f)] private float costGrowth = 1.5f;
        [Tooltip("Any: one bought requirement opens the node. All: every requirement must be bought.")]
        [SerializeField] private UpgradeRequirement requirement = UpgradeRequirement.Any;
        [Tooltip("Ids of the nodes this one hangs off. None means it is available from the start.")]
        [SerializeField] private List<string> requires = new();
        [SerializeReference] private List<UpgradeEffect> effects = new();

        public string Id => id;
        public LocalizedString Title => title;
        public LocalizedString Description => description;
        public Sprite Icon => icon;
        public Vector2 Position => position;
        public int MaxLevel => Mathf.Max(1, maxLevel);
        public UpgradeRequirement Requirement => requirement;
        public IReadOnlyList<string> Requires => requires;
        public IReadOnlyList<UpgradeEffect> Effects => effects;

        /// <summary>Price of going from <paramref name="level"/> to the next level.</summary>
        public AlphabeticNotation GetCost(int level)
        {
            var cost = baseCost * new AlphabeticNotation(Math.Pow(costGrowth, level));
            if (cost.magnitude == 0)
            {
                cost.coefficient = Math.Ceiling(cost.coefficient);
            }

            return cost;
        }

        public void Apply(StatSheet stats, int level)
        {
            if (level <= 0)
            {
                return;
            }

            foreach (var effect in effects)
            {
                effect?.Apply(stats, level);
            }
        }

#if UNITY_EDITOR
        public UpgradeNode(string id, Vector2 position)
        {
            this.id = id;
            this.position = position;
        }

        public string EditorId
        {
            get => id;
            set => id = value;
        }

        public Vector2 EditorPosition
        {
            get => position;
            set => position = value;
        }

        public List<string> EditorRequires => requires;
        public List<UpgradeEffect> EditorEffects => effects;
#endif
    }
}
