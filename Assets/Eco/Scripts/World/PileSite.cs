using System;
using Eco.Scripts.Scrap;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;

namespace Eco.Scripts.World
{
    /// <summary>
    /// The scrap pile in the middle of a land chunk, and the flowers and butterflies that replace it once it's dug
    /// out (when those upgrades are unlocked). Lives in the ScrapPileSite prefab, nested into chunk prefabs whose
    /// definition has a pile; <see cref="LandChunk"/> drives it.
    /// </summary>
    public class PileSite : MonoBehaviour
    {
        [SerializeField] private ScrapPile pile;
        [SerializeField] private GameObject flowers;
        [SerializeField] private GameObject butterflies;

        private IDisposable _unlockSubscription;
        private IDisposable _lockSubscription;
        private bool _cleared;

        public ScrapPile Pile => pile;

        /// <summary>The pile was dug out completely.</summary>
        public event Action Cleaned;

        public void ShowPile(int size, int maxSize, int digPower)
        {
            _cleared = false;
            gameObject.SetActive(true);
            pile.Initialize(size, maxSize, digPower);
            pile.OnPileCleaned += OnPileCleaned;
            flowers.SetActive(false);
            butterflies.SetActive(false);
            SubscribeUnlocks();
        }

        public void ShowCleared()
        {
            _cleared = true;
            gameObject.SetActive(true);
            pile.Hide();
            ShowFlowers();
            ShowButterflies();
            SubscribeUnlocks();
        }

        public void Clear()
        {
            pile.OnPileCleaned -= OnPileCleaned;
            _unlockSubscription?.Dispose();
            _lockSubscription?.Dispose();
            _unlockSubscription = null;
            _lockSubscription = null;
        }

        private void OnPileCleaned()
        {
            pile.OnPileCleaned -= OnPileCleaned;
            _cleared = true;
            ShowFlowers();
            ShowButterflies();
            Cleaned?.Invoke();
        }

        // Flowers and butterflies can be unlocked or refunded while the chunk is spawned
        private void SubscribeUnlocks()
        {
            _unlockSubscription ??= UnlockTracker.OnUnlocked.Subscribe(OnUpgradeUnlocked);
            _lockSubscription ??= UnlockTracker.OnLocked.Subscribe(OnUpgradeLocked);
        }

        private void OnUpgradeUnlocked(UnlockableUpgradeType upgrade)
        {
            if (!_cleared)
            {
                return;
            }

            if (upgrade == UnlockableUpgradeType.Flowers)
            {
                ShowFlowers();
            }
            else if (upgrade == UnlockableUpgradeType.Butterflies)
            {
                ShowButterflies();
            }
        }

        private void OnUpgradeLocked(UnlockableUpgradeType upgrade)
        {
            if (upgrade == UnlockableUpgradeType.Flowers)
            {
                flowers.SetActive(false);
            }
            else if (upgrade == UnlockableUpgradeType.Butterflies)
            {
                butterflies.SetActive(false);
            }
        }

        // Scene unloads destroy spawned chunks without despawning them; the unlock subjects are static
        private void OnDestroy()
        {
            Clear();
        }

        private void ShowFlowers()
        {
            flowers.SetActive(UnlockTracker.IsUpgradeUnlocked(UnlockableUpgradeType.Flowers));
        }

        private void ShowButterflies()
        {
            butterflies.SetActive(UnlockTracker.IsUpgradeUnlocked(UnlockableUpgradeType.Butterflies));
        }
    }
}
