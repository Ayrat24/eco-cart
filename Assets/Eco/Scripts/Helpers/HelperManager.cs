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
        [SerializeField] private CatHelper catPrefab;
        [SerializeField] private ChickenHelper chickenPrefab;
        [SerializeField] private int spawnRadius;
        private CurrencyManager _currencyManager;
        private UpgradesCollection _upgrades;
        IDisposable _subscription;
        private Player _player;

        private readonly List<Vector3> _spawnDirections = new(){Vector3.left, Vector3.right, Vector3.forward, Vector3.back};
        private int _lastSpawnDirection;
        private int _navmeshPriority = 51;
        private readonly List<Helper> _helpers = new();
        
        [Inject]
        private void Init(CurrencyManager currencyManager, UpgradesCollection upgrades, Player player)
        {
            _currencyManager = currencyManager;
            _upgrades = upgrades;
            _player = player;
        }

        public void LoadHelpers()
        {
            SetUpUpgrades();
        }

        private Helper Spawn(Helper prefab, Vector3 spawnPosition)
        {
            var helper = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
            helper.Init(_currencyManager, _upgrades, _player, _navmeshPriority);
            _navmeshPriority++;
            return helper;
        }
        
        private void SetUpUpgrades()
        {
            var builder = new DisposableBuilder();
            foreach (var helperBuyUpgrade in _upgrades.GetUpgradeTypes<HelperBuyUpgrade>())
            {
                helperBuyUpgrade.OnPurchase.Subscribe(SpawnHelper).AddTo(ref builder);

                for (int i = 0; i < helperBuyUpgrade.CurrentLevel.Value; i++)
                {
                    SpawnHelper(helperBuyUpgrade.GetHelperClass());
                }
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

            Helper helper;
            switch (helperClass)
            {
                case HelperClass.Cat:
                    helper = Spawn(catPrefab, spawnPosition);
                    break;
                case HelperClass.Chicken:
                    helper = Spawn(chickenPrefab, spawnPosition);
                    break;
                default:
                    return;
            }
            
            _helpers.Add(helper);
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();

            foreach (var helper in _helpers)
            {
                helper.Clear();
            }
        }

        public enum HelperClass
        {
            Collector,
            Cat,
            Chicken
        }
    }
}