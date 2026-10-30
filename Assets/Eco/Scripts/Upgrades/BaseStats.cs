using System;
using System.Collections.Generic;
using Eco.Scripts.Scrap;
using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    /// <summary>The jelly's stats before any upgrade. Upgrade effects add to these.</summary>
    [CreateAssetMenu(menuName = "Upgrade/Base Stats")]
    public class BaseStats : ScriptableObject
    {
        [Header("Collecting")]
        [Tooltip("Total scrap weight the stomach holds.")]
        [SerializeField, Min(1)] private int maxWeight = 10;
        [Tooltip("Tentacles that grab scrap at the same time.")]
        [SerializeField, Min(1)] private int limbCount = 2;
        [Tooltip("Radius around the collector's center that tentacles reach, in metres (grows with the body).")]
        [SerializeField, Min(0.1f)] private float limbRange = 2.4f;
        [SerializeField, Min(0.1f)] private float moveSpeed = 8f;

        [Header("Slime trail")]
        [Tooltip("Metres of trail the water bank holds.")]
        [SerializeField, Min(1f)] private float wetnessCapacity = 100f;
        [Tooltip("Metres of trail the bank soaks up per second in the water.")]
        [SerializeField, Min(0.1f)] private float wetnessFillSpeed = 35f;

        [Header("Recycling")]
        [Tooltip("Combo bonus per earlier item of the same type in a load, once the combo is unlocked.")]
        [SerializeField, Min(0f)] private float comboMultiplier = 0.5f;
        [SerializeField] private List<ScrapScore> scrapScores = new();

        [Header("Piles")]
        [SerializeField, Min(1)] private int digPower = 3;
        [SerializeField, Min(0f)] private float pileScore = 2700f;
        [Tooltip("Extra pile reward, as a fraction of the pile score, once flowers are unlocked.")]
        [SerializeField, Min(0f)] private float flowersBonus = 0.5f;
        [Tooltip("Extra pile reward, as a fraction of the pile score, once butterflies are unlocked.")]
        [SerializeField, Min(0f)] private float butterfliesBonus = 1f;

        public int MaxWeight => maxWeight;
        public int LimbCount => limbCount;
        public float LimbRange => limbRange;
        public float MoveSpeed => moveSpeed;
        public float WetnessCapacity => wetnessCapacity;
        public float WetnessFillSpeed => wetnessFillSpeed;
        public float ComboMultiplier => comboMultiplier;
        public IReadOnlyList<ScrapScore> ScrapScores => scrapScores;
        public int DigPower => digPower;
        public float PileScore => pileScore;
        public float FlowersBonus => flowersBonus;
        public float ButterfliesBonus => butterfliesBonus;

        [Serializable]
        public struct ScrapScore
        {
            public ScrapType type;
            [Tooltip("Money per unit of weight.")]
            public float score;
            [Tooltip("Color of the gained money label.")]
            public Color color;
        }
    }
}
