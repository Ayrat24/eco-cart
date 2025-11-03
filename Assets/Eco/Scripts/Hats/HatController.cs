using System;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eco.Scripts.Hats
{
    public class HatController
    {
        private readonly Transform _hatParent;
        private readonly UpgradesCollection _upgrades;
        
        private Hat _currentHat;
        private IDisposable _subscription;

        public HatController(UpgradesCollection upgrades, Transform hatParent)
        {
            _upgrades = upgrades;
            _hatParent = hatParent;
        }

        public void Initialize(SaveManager saveManager)
        {
            // Get all hat upgrades
            var hatUpgrades = _upgrades.GetUpgradeTypes<HatBuyUpgrade>();
            
            // Subscribe to hat selection events
            var builder = new DisposableBuilder();
            foreach (var hatUpgrade in hatUpgrades)
            {
                hatUpgrade.OnSelected.Subscribe(EquipHat).AddTo(ref builder);
            }
            _subscription = builder.Build();

            // Load the saved hat if any
            if (!string.IsNullOrEmpty(saveManager.Progress.selectedHat))
            {
                var savedHatUpgrade = hatUpgrades.Find(x => x.upgradeId == saveManager.Progress.selectedHat);
                if (savedHatUpgrade != null && savedHatUpgrade.CurrentLevel.Value >= 2)
                {
                    var hatData = savedHatUpgrade.GetHatData();
                    SpawnHat(hatData, saveManager.Progress.selectedHat);
                }
            }
        }

        private void EquipHat(HatBuyUpgrade.HatData hatData)
        {
            // Remove current hat if exists
            if (_currentHat != null)
            {
                Object.Destroy(_currentHat.gameObject);
            }

            // Find the hat upgrade ID
            var hatUpgrades = _upgrades.GetUpgradeTypes<HatBuyUpgrade>();
            string hatId = null;
            foreach (var hatUpgrade in hatUpgrades)
            {
                if (hatUpgrade.GetHatData().prefab == hatData.prefab)
                {
                    hatId = hatUpgrade.upgradeId;
                    break;
                }
            }

            SpawnHat(hatData, hatId);
        }

        private void SpawnHat(HatBuyUpgrade.HatData hatData, string hatId)
        {
            _currentHat = Object.Instantiate(hatData.prefab, _hatParent);
            _currentHat.transform.localPosition = hatData.positionOffset;
            _currentHat.transform.localRotation = Quaternion.identity;
            _currentHat.Setup(hatId);
        }

        public void SaveHat(SaveManager saveManager)
        {
            saveManager.Progress.selectedHat = _currentHat?.Id ?? string.Empty;
        }

        public void Clear()
        {
            _subscription?.Dispose();
        }
    }
}
