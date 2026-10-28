using System;
using Eco.Scripts.Helpers;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades.Effects
{
    /// <summary>Unlocks a pet that follows the jelly and helps.</summary>
    [Serializable]
    public class UnlockPetEffect : UpgradeEffect
    {
        private static readonly LocalizedString Label = new(LocTableName, "effect-unlock-pet");

        [SerializeField] private PetDefinition pet;

        public PetDefinition Pet => pet;

        public override void Apply(StatSheet stats, int level)
        {
            if (pet != null)
            {
                stats.Unlocks.Add(pet.UnlockType);
            }
        }

        public override string Describe(StatSheet current, StatSheet next)
        {
            return string.Format(Label.GetLocalizedString(), pet != null ? pet.GetName() : "?");
        }

        public override string EditorSummary => $"Unlock {(pet != null ? pet.name : "(no pet)")}";
    }
}
