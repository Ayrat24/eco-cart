using System;
using System.Collections.Generic;
using System.Threading;
using Eco.Scripts.Pooling;
using Eco.Scripts.Trash;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Eco.Scripts.Helpers
{
    public class ChickenHelper : Helper
    {
        [SerializeField] private int maxDistanceFromPlayer = 15;
        [SerializeField] private LayerMask groundItemsMask;
        [SerializeField] private int playerStopDistance = 8;
        [SerializeField] private TrashItem eggPrefab;
        [SerializeField] private float eggReplaceDistanceFromPlayer = 20;

        private bool _initialized;

        private ObjectPool<TrashItem> _eggPool;
        private readonly HashSet<TrashItem> _activeEggs = new();

        // runtime-configurable values (affected by upgrades)
        private int _currentMaxConcurrentEggs;
        private float _eggIntervalSeconds = 5f;

        public override void Init(CurrencyManager currencyManager, ScoreStats scoreStats, Player player,
            int navmeshPriority, List<HelperUpgrade> helperClassUpgrades)
        {
            base.Init(currencyManager, scoreStats, player, navmeshPriority, helperClassUpgrades);

            var maxConcurrentEggs = (int)Upgrades[0].Value;
            _eggPool = new ObjectPool<TrashItem>(eggPrefab, maxConcurrentEggs, PoolManager.Instance.transform);

            // initialize runtime values from serialized defaults
            _currentMaxConcurrentEggs = Mathf.Max(1, maxConcurrentEggs);
            _eggIntervalSeconds = 5f;

            // start periodic lay timer (will be reconfigured by upgrades via ApplyHelperUpgrade)
            ActionSubscription =
                Observable.Interval(TimeSpan.FromSeconds(_eggIntervalSeconds)).Subscribe(_ => LayEgg());
            CancellationTokenSource = new CancellationTokenSource();

            agent.destination = player.transform.position;
            animationController.GoingToTarget = true;

            _initialized = true;

            if (helperClassUpgrades != null)
            {
                SetupUpgrades(helperClassUpgrades);
            }
        }

        protected void FixedUpdate()
        {
            if (!_initialized)
            {
                return;
            }

            WalkAround();
        }

        private void WalkAround()
        {
            if (Vector3.Distance(agent.destination, transform.position) > agent.stoppingDistance)
            {
                return;
            }

            var position = Player.transform.position +
                           new Vector3(Random.Range(-maxDistanceFromPlayer, maxDistanceFromPlayer), 0,
                               Random.Range(-maxDistanceFromPlayer, maxDistanceFromPlayer));
            agent.destination = position;
        }

        private void LayEgg()
        {
            animationController.TriggerAction();

            // Clean up any inactive or null references that could have been left in the set
            // (defensive: if an egg was returned to a different pool or callbacks missed).
            foreach (var e in System.Linq.Enumerable.ToArray(_activeEggs))
            {
                if (e == null || !e.gameObject.activeSelf)
                {
                    _activeEggs.Remove(e);
                }
            }

            // Limit total eggs spawned by this chicken
            if (_activeEggs.Count >= _currentMaxConcurrentEggs)
            {
                // try to find an active egg that is far away from the player and reuse it
                TrashItem farEgg = null;
                var playerPos = Player.transform.position;
                var replaceDistSq = eggReplaceDistanceFromPlayer * eggReplaceDistanceFromPlayer;

                // iterate a copy to avoid issues if we modify the set while returning an egg
                foreach (var e in System.Linq.Enumerable.ToArray(_activeEggs))
                {
                    if (e == null) continue;
                    // only replace eggs that are not being picked up / already collected
                    if (!e.CanBeRecycled) continue;
                    if ((e.transform.position - playerPos).sqrMagnitude > replaceDistSq)
                    {
                        farEgg = e;
                        break;
                    }
                }

                if (farEgg == null)
                {
                    // no suitable egg to replace, bail out
                    return;
                }

                // Reuse the far egg in-place instead of returning it to the pool. This avoids
                // races where the egg gets returned to a pool and notifications / bookkeeping
                // get out of sync, which could cause the chicken to stop spawning new eggs.
                var spawnPosReuse = transform.position;
                spawnPosReuse.y = 0;

                // Reinitialize some visual state and move the egg to the new spawn position
                farEgg.OnSpawn();
                farEgg.transform.position = spawnPosReuse;

                // Make sure callbacks are still set so this chicken continues tracking the egg
                farEgg.ReturnToPoolCallback = ReturnEggToPool;
                farEgg.OnReturnedToPool = OnEggReturnedToPool;

                // We've reused an existing egg, so we don't need to Get() a new one.
                return;
            }

            var egg = _eggPool.Get();
            if (egg == null)
            {
                // pool exhausted (shouldn't happen because we guard by _activeEggs), bail out
                return;
            }

            // Make the chicken own the egg return lifecycle
            egg.ReturnToPoolCallback = ReturnEggToPool;
            // Also listen for the item being returned to any pool (defensive):
            egg.OnReturnedToPool = OnEggReturnedToPool;

            var spawnPos = transform.position;
            spawnPos.y = 0;
            egg.transform.position = spawnPos;

            _activeEggs.Add(egg);
        }

        private void ReturnEggToPool(TrashItem egg)
        {
            if (egg == null) return;

            // clear callbacks so the egg isn't processed twice
            egg.ReturnToPoolCallback = null;
            egg.OnReturnedToPool = null;

            _activeEggs.Remove(egg);
            // Always return the egg to the pool even if it wasn't tracked (defensive)
            _eggPool.ReturnToPool(egg);
        }

        private void OnEggReturnedToPool(TrashItem egg)
        {
            // Called from TrashItem.OnDespawn when this item was returned to any pool.
            // Make this idempotent and non-throwing.
            if (egg == null) return;

            egg.ReturnToPoolCallback = null;
            egg.OnReturnedToPool = null;
            _activeEggs.Remove(egg);
        }

        public override void Clear()
        {
            // Return all active eggs to the local pool
            foreach (var egg in System.Linq.Enumerable.ToArray(_activeEggs))
            {
                if (egg == null) continue;
                ReturnEggToPool(egg);
            }

            _activeEggs.Clear();

            base.Clear();
        }

        // Handle helper-class upgrades: index 0 = max concurrent eggs, index 1 = egg spawn interval (seconds)
        protected override void ApplyHelperUpgrade(HelperUpgrade upgrade, int index)
        {
            if (upgrade == null) return;

            try
            {
                switch (index)
                {
                    case 0:
                        // treat upgrade.Value as desired capacity (round to int)
                        _currentMaxConcurrentEggs = Mathf.Max(1, Mathf.RoundToInt(upgrade.Value));
                        break;
                    case 1:
                        // treat upgrade.Value as spawn interval seconds (clamp to a sensible minimum)
                        var newInterval = Mathf.Max(0.1f, upgrade.Value);
                        if (Math.Abs(newInterval - _eggIntervalSeconds) > 0.001f)
                        {
                            _eggIntervalSeconds = newInterval;
                            // resubscribe periodic timer
                            ActionSubscription?.Dispose();
                            ActionSubscription = Observable.Interval(TimeSpan.FromSeconds(_eggIntervalSeconds))
                                .Subscribe(_ => LayEgg());
                        }

                        break;
                    default:
                        Debug.Log($"ChickenHelper: received upgrade at index {index} (value={upgrade.Value})");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}