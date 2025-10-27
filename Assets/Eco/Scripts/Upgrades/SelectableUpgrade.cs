using System;
using LargeNumbers;
using R3;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades
{
    /// <summary>
    /// CurrentLevel.Value = 0 -> not purchased
    /// CurrentLevel.Value = 1 -> purchased
    /// CurrentLevel.Value = 2 -> selected
    /// </summary>
    public abstract class SelectableUpgrade : Upgrade
    {
        [SerializeField] private LocalizedString selectString = new(LocTableName, SelectLocString);
        [SerializeField] private LocalizedString notSelectedString = new(LocTableName, NotSelectedLocString);

        protected abstract string SelectableGroupId { get; }

        private const string LocTableName = "GameUI";
        private const string SelectLocString = "upgrade-selected";
        private const string NotSelectedLocString = "upgrade-not-selected";

        private IDisposable _subscription;
        private static readonly Subject<SelectableUpgrade> OnSelect = new();

        private void OnValidate()
        {
            if (selectString == null || notSelectedString == null)
            {
                selectString = new LocalizedString(LocTableName, SelectLocString);
                notSelectedString = new LocalizedString(LocTableName, NotSelectedLocString);
            }
        }

        protected override AlphabeticNotation CalculateCost()
        {
            if (CurrentLevel.Value > 0)
            {
                return new AlphabeticNotation(0);
            }

            return new AlphabeticNotation(baseCost);
        }

        public override void BuyUpgrade()
        {
            // Buying an upgrade should also select it. If it's not purchased yet (0) or purchased but not selected (1),
            // set it to selected (2), apply the upgrade and notify subscribers.
            if (CurrentLevel.Value < 2)
            {
                Available = false;
                CurrentLevel.Value = 2;
                ApplyUpgrade(1); //upgrade level doesn't matter
                OnSelect.OnNext(this);
            }
        }

        private void Deselect(SelectableUpgrade upgrade)
        {
            if (upgrade.SelectableGroupId != SelectableGroupId || upgrade == this)
            {
                return;
            }

            // Only deselect this upgrade if it is currently selected (level == 2).
            if (CurrentLevel.Value == 2)
            {
                Available = true;
                CurrentLevel.Value = 1;
            }
        }

        protected override void Load(int level)
        {
            base.Load(level);
            // Guard against double subscriptions: dispose previous subscription if present.
            _subscription?.Dispose();
            _subscription = OnSelect.Subscribe(Deselect);

            if (CurrentLevel.Value == 2)
            {
                Available = false;
            }
        }

        public override void Clear()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        public override string GetButtonText()
        {
            return CurrentLevel.Value switch
            {
                0 => base.GetButtonText(),
                1 => notSelectedString.GetLocalizedString(),
                _ => selectString.GetLocalizedString()
            };
        }
    }
}