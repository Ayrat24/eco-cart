using System;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades
{
    /// <summary>
    /// What buying an <see cref="UpgradeNode"/> does. Nodes hold effects by [SerializeReference], so a new kind of
    /// upgrade is a new subclass: it shows up in the tree editor's "Add Effect" menu by itself.
    /// </summary>
    [Serializable]
    public abstract class UpgradeEffect
    {
        protected const string LocTableName = "Upgrades";

        /// <summary>Adds this effect at <paramref name="level"/> (1 or more) to the stats.</summary>
        public abstract void Apply(StatSheet stats, int level);

        /// <summary>
        /// One line for the menu, e.g. "Stomach capacity: 10 → 15". <paramref name="next"/> is null at max level.
        /// </summary>
        public abstract string Describe(StatSheet current, StatSheet next);

        /// <summary>Short untranslated summary shown on the node in the tree editor.</summary>
        public abstract string EditorSummary { get; }

        protected static string Change(LocalizedString label, string current, string next)
        {
            string text = label.GetLocalizedString();
            if (next == null || next == current)
            {
                return string.Format(text, current);
            }

            return string.Format(text, $"{current} → {next}");
        }
    }
}
