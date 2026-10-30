using System;
using System.Collections.Generic;
using Eco.Scripts.Upgrades;
using Eco.Scripts.World;
using R3;
using UnityEngine;
using VContainer;

namespace Eco.Scripts.Helpers
{
    /// <summary>
    /// Spawns the active pet. Pets are unlocked in the upgrade tree; until the pet menu exists, a newly unlocked
    /// pet becomes the active one, and a refunded one leaves. One pet at a time.
    /// </summary>
    public class HelperManager : MonoBehaviour
    {
        [SerializeField] private int spawnRadius;
        [SerializeField] private List<PetDefinition> pets = new();

        private CurrencyManager _currencyManager;
        private Player _player;
        private PlayerStats _playerStats;
        private IDisposable _subscription;

        private readonly List<Vector3> _spawnDirections = new(){Vector3.left, Vector3.right, Vector3.forward, Vector3.back};
        private int _lastSpawnDirection;
        private int _navmeshPriority = 51;
        private Helper _activeHelper;
        private PetDefinition _activePet;

        public IReadOnlyList<PetDefinition> Pets => pets;
        public PetDefinition ActivePet => _activePet;

        [Inject]
        private void Init(CurrencyManager currencyManager, Player player, PlayerStats playerStats)
        {
            _currencyManager = currencyManager;
            _player = player;
            _playerStats = playerStats;
        }

        public void LoadHelpers(SaveManager saveManager)
        {
            var builder = new DisposableBuilder();
            UnlockTracker.OnUnlocked.Subscribe(OnUnlocked).AddTo(ref builder);
            UnlockTracker.OnLocked.Subscribe(OnLocked).AddTo(ref builder);
            _subscription?.Dispose();
            _subscription = builder.Build();

            var saved = FindPet(saveManager.Progress.selectedPet);
            if (saved != null && saved.Unlocked)
            {
                SelectPet(saved);
            }
        }

        public void Save(SaveManager saveManager)
        {
            saveManager.Progress.selectedPet = _activePet != null ? _activePet.Id : string.Empty;
        }

        /// <summary>Makes <paramref name="pet"/> the active pet, replacing the current one. Null sends it away.</summary>
        public void SelectPet(PetDefinition pet)
        {
            if (_activeHelper != null)
            {
                _activeHelper.Clear();
                Destroy(_activeHelper.gameObject);
                _activeHelper = null;
            }

            _activePet = pet;
            if (pet == null || pet.Prefab == null)
            {
                return;
            }

            if (_lastSpawnDirection >= _spawnDirections.Count)
            {
                _lastSpawnDirection = 0;
            }

            Vector3 spawnPosition = _player.transform.position + _spawnDirections[_lastSpawnDirection] * spawnRadius;
            _lastSpawnDirection++;

            _activeHelper = Instantiate(pet.Prefab, WorldController.ClosestLand(spawnPosition), Quaternion.identity,
                transform);
            _activeHelper.Init(_currencyManager, _playerStats, _player, _navmeshPriority);
            _navmeshPriority++;
        }

        private void OnUnlocked(UnlockableUpgradeType type)
        {
            foreach (var pet in pets)
            {
                if (pet != null && pet.UnlockType == type)
                {
                    SelectPet(pet);
                    return;
                }
            }
        }

        private void OnLocked(UnlockableUpgradeType type)
        {
            if (_activePet != null && _activePet.UnlockType == type)
            {
                SelectPet(null);
            }
        }

        private PetDefinition FindPet(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (var pet in pets)
            {
                if (pet != null && pet.Id == id)
                {
                    return pet;
                }
            }

            return null;
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();

            if (_activeHelper != null)
            {
                _activeHelper.Clear();
                Destroy(_activeHelper.gameObject);
                _activeHelper = null;
            }
        }
    }
}
