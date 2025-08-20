using System;
using System.Collections.Generic;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace Eco.Scripts.Helpers
{
    public class HelperManager : MonoBehaviour
    {
        [SerializeField] private CollectorHelper collectorHelperPrefab;
        [SerializeField] private int spawnRadius;
        private CurrencyManager currencyManager;
        private UpgradesCollection _upgrades;
        IDisposable _subscription;
        private Player _player;

        private List<Vector3> _spawnDirections = new(){Vector3.left, Vector3.right, Vector3.forward, Vector3.back};
        private int _lastSpawnDirection;
        private int _navmeshPriority = 51;
        
        [Inject]
        private void Init(CurrencyManager currencyManager, UpgradesCollection upgrades, Player player)
        {
            this.currencyManager = currencyManager;
            _upgrades = upgrades;
            _player = player;
        }

        public void LoadHelpers()
        {
            SetUpUpgrades();
        }

        private void SpawnCollector(Vector3 spawnPosition)
        {
            var helper = Instantiate(collectorHelperPrefab, spawnPosition, Quaternion.identity, transform);
            helper.Init(currencyManager, _upgrades, _player, _navmeshPriority);
            _navmeshPriority++;
        }

        private void SetUpUpgrades()
        {
            var builder = new DisposableBuilder();
            foreach (var helperBuyUpgrade in _upgrades.helperBuyUpgrades)
            {
                helperBuyUpgrade.OnPurchase.Subscribe(SpawnHelper).AddTo(ref builder);
            }

            _subscription = builder.Build();
        }

        private void SpawnHelper(HelperClass helperClass)
        {
            if (_lastSpawnDirection >= _spawnDirections.Count)
            {
                _lastSpawnDirection = 0;
            }
            
            Vector3 spawnPosition = _player.transform.position + _spawnDirections[_lastSpawnDirection] * spawnRadius;
            _lastSpawnDirection++;
            
            switch (helperClass)
            {
                case HelperClass.Collector:
                    SpawnCollector(spawnPosition);
                    break;
            }
        }

        private void OnDestroy()
        {
            _subscription.Dispose();
        }

        public enum HelperClass
        {
            Collector,
            Planter
        }
    }
}