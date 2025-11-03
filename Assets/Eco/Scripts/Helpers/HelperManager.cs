using System;
using System.Collections.Generic;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;
using VContainer;

namespace Eco.Scripts.Helpers
{
    public class HelperManager : MonoBehaviour
    {
        [SerializeField] private int spawnRadius;
        private CurrencyManager _currencyManager;
        private UpgradesCollection _upgrades;
        private Player _player;
        private ScoreStats _scoreStats;
        private IDisposable _subscription;

        private readonly List<Vector3> _spawnDirections = new(){Vector3.left, Vector3.right, Vector3.forward, Vector3.back};
        private int _lastSpawnDirection;
        private int _navmeshPriority = 51;
        // single active helper at a time
        private Helper _activeHelper;

        // map HelperClass -> prefab provided by each HelperBuyUpgrade (assumption: HelperBuyUpgrade exposes GetPrefab())
        private readonly Dictionary<HelperBuyUpgrade, Helper> _prefabMap = new();

        [Inject]
        private void Init(CurrencyManager currencyManager, UpgradesCollection upgrades, Player player, ScoreStats scoreStats)
        {
            _currencyManager = currencyManager;
            _upgrades = upgrades;
            _player = player;
            _scoreStats = scoreStats;
        }

        public void LoadHelpers()
        {
            SetUpUpgrades();
        }

        private Helper Spawn(Helper prefab, Vector3 spawnPosition, List<HelperUpgrade> helperClassUpgrades)
        {
            var helper = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
            helper.Init(_currencyManager, _scoreStats, _player, _navmeshPriority, helperClassUpgrades);
            _navmeshPriority++;
            return helper;
        }
        
        private void SetUpUpgrades()
        {
            var builder = new DisposableBuilder();
            foreach (var helperBuyUpgrade in _upgrades.GetUpgradeTypes<HelperBuyUpgrade>())
            {
                // populate prefab map from the upgrade (assumes GetHelperClass() and GetPrefab() exist)
                var prefab = helperBuyUpgrade.GetPrefab();
                _prefabMap[helperBuyUpgrade] = prefab;

                // when a helper upgrade is purchased, replace the active helper
                helperBuyUpgrade.OnPurchase.Subscribe(SpawnHelper).AddTo(ref builder);

                if(helperBuyUpgrade.CurrentLevel.Value >= 2)
                {
                    SpawnHelper(helperBuyUpgrade);
                }
            }

            _subscription = builder.Build();
        }

        private void SpawnHelper(HelperBuyUpgrade helperClass)
        {
            if (_lastSpawnDirection >= _spawnDirections.Count)
            {
                _lastSpawnDirection = 0;
            }
            
            Vector3 spawnPosition = _player.transform.position + _spawnDirections[_lastSpawnDirection] * spawnRadius;
            _lastSpawnDirection++;

            // dispose existing helper (only one allowed at a time)
            if (_activeHelper != null)
            {
                _activeHelper.Clear();
                Destroy(_activeHelper.gameObject);
                _activeHelper = null;
            }

            // lookup prefab for this helper class
            if (!_prefabMap.TryGetValue(helperClass, out var prefab) || prefab == null)
                return; // no prefab configured for this helper class

            _activeHelper = Spawn(prefab, spawnPosition, helperClass.Upgrades);
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();

            if (_activeHelper != null)
            {
                _activeHelper.Clear();
                // don't need to Destroy here since OnDestroy is running, but keep symmetry
                Destroy(_activeHelper.gameObject);
                _activeHelper = null;
            }
        }
    }
}