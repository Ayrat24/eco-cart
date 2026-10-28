using System;
using Eco.Scripts.Scrap;
using LargeNumbers;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades.Effects
{
    /// <summary>One scrap type pays more per unit of weight when recycled.</summary>
    [Serializable]
    public class ScrapScoreEffect : UpgradeEffect
    {
        private static readonly LocalizedString Label = new(LocTableName, "effect-scrap-score");

        [SerializeField] private ScrapType scrapType;
        [Tooltip("Money per unit of weight added per level.")]
        [SerializeField, Min(0f)] private float scorePerLevel = 2f;

        public override void Apply(StatSheet stats, int level)
        {
            stats.ScrapScores[scrapType] = stats.GetScrapScore(scrapType) + scorePerLevel * level;
        }

        public override string Describe(StatSheet current, StatSheet next)
        {
            // The label takes the scrap name ({0}) and the value ({1})
            string value = Format(current.GetScrapScore(scrapType));
            if (next != null)
            {
                value = $"{value} → {Format(next.GetScrapScore(scrapType))}";
            }

            return string.Format(Label.GetLocalizedString(), ScrapNames.Get(scrapType), value);
        }

        public override string EditorSummary => $"{scrapType} score +{scorePerLevel}/lvl";

        private static string Format(double score)
        {
            return new AlphabeticNotation(score).ToString();
        }
    }
}
