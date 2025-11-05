using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Trash;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;
using UnityEngine.AI;

namespace Eco.Scripts.Helpers
{
    public class CatHelper : Helper
    {
        [SerializeField] private int maxDistanceFromPlayer = 30;
        [SerializeField] private LayerMask groundItemsMask;
        [SerializeField] private int playerStopDistance = 8;

        private readonly Collider[] _colliders = new Collider[50];
        private const TrashType Food = TrashType.Food;

        private bool _goingToTarget;

        // upgrade subscription tracking (managed by this helper only)


        // recycle value multiplier (upgrade index 1)
        private float _recycleMultiplier = 1f;

        public override void Init(CurrencyManager currencyManager, ScoreStats scoreStats, Player player,
            int navmeshPriority, List<HelperUpgrade> helperClassUpgrades)
        {
            base.Init(currencyManager, scoreStats, player, navmeshPriority, helperClassUpgrades);

            var interval = TimeSpan.FromSeconds(1);
            ActionSubscription = Observable.Interval(interval).Subscribe(_ => { GoToNearbyItem(); });
            CancellationTokenSource = new CancellationTokenSource();

            SetupUpgrades(helperClassUpgrades);
        }


        protected override void ApplyHelperUpgrade(HelperUpgrade upgrade, int index)
        {
            if (upgrade == null) return;

            try
            {
                switch (index)
                {
                    case 0:
                        // agent speed multiplier/value
                        if (agent != null)
                        {
                            agent.acceleration = upgrade.Value * 10;
                            agent.speed = upgrade.Value;
                            Debug.Log($"CatHelper: applied speed upgrade -> {agent.speed:F2}");
                        }

                        break;
                    case 1:
                        // recycle value multiplier
                        _recycleMultiplier = upgrade.Value;
                        Debug.Log($"CatHelper: applied recycle multiplier -> {_recycleMultiplier:F2}");
                        break;
                    default:
                        Debug.Log($"CatHelper: received upgrade at index {index} (value={upgrade.Value})");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void GoToNearbyItem()
        {
            if (_goingToTarget)
            {
                return;
            }

            Vector3 center = transform.position;
            int count = Physics.OverlapSphereNonAlloc(center, searchRadius, _colliders, groundItemsMask);

            List<TrashItem> trashItems = new List<TrashItem>();
            for (int i = 0; i < count; i++)
            {
                if (_colliders[i].TryGetComponent<TrashItem>(out var trashItem) && trashItem.TrashType == Food)
                {
                    trashItems.Add(trashItem);
                }
            }

            if (trashItems.Count == 0)
            {
                ReturnToPlayer();
                return;
            }

            // sort by ascending distance to player (closest first)
            trashItems.Sort((x, y) =>
                Vector3.Distance(x.transform.position, Player.transform.position)
                    .CompareTo(Vector3.Distance(y.transform.position, Player.transform.position)));

            var food = trashItems[0];

            if (Vector3.Distance(food.transform.position, Player.transform.position) > maxDistanceFromPlayer)
            {
                ReturnToPlayer();
                return;
            }

            // Try to calculate a valid path before committing to the target. If not reachable, skip it.
            var targetPos = food.transform.position;
            var path = new NavMeshPath();
            bool hasPath = agent.CalculatePath(targetPos, path) && path.status == NavMeshPathStatus.PathComplete;

            if (!hasPath)
            {
                // if we can't reach this food, try the next candidate (or return to player)
                // remove this unreachable item and try again
                trashItems.RemoveAt(0);
                if (trashItems.Count > 0)
                {
                    // attempt the next item on the next tick
                    return;
                }

                ReturnToPlayer();
                return;
            }

            agent.stoppingDistance = 3;
            agent.path = path; // use computed path
            _goingToTarget = true;
            animationController.GoingToTarget = true;

            DebugState = "Going to trash";

            // start the async consumer which will watch arrival, existence and path validity
            ConsumeFoodAsync(food, CancellationTokenSource.Token).Forget();
        }

        private void ReturnToPlayer()
        {
            DebugState = "Going around player";
            agent.stoppingDistance = playerStopDistance;
            agent.destination = Player.transform.position;
            // ensure we are not marked as going to a specific target
            _goingToTarget = false;
            animationController.GoingToTarget = false;
        }

        private async UniTask ConsumeFoodAsync(TrashItem food, CancellationToken token)
        {
            // Robust waiting loop: poll state periodically, abort on timeout or if target disappears or path becomes invalid
            const float pollIntervalSeconds = 0.2f;
            const float maxWaitSeconds = 12f; // safety timeout to avoid infinite waiting
            float waited = 0f;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    // if the target is gone or disabled, abort
                    if (food == null || food.gameObject == null || !food.gameObject.activeInHierarchy)
                    {
                        DebugState = "Target gone";
                        break;
                    }

                    // if agent has no path or path status is invalid, try recalculating once
                    if (!agent.hasPath && !agent.pathPending)
                    {
                        var recalc = new NavMeshPath();
                        if (!agent.CalculatePath(food.transform.position, recalc) ||
                            recalc.status != NavMeshPathStatus.PathComplete)
                        {
                            DebugState = "Path invalid";
                            break;
                        }

                        agent.path = recalc;
                    }

                    // if we are close enough to the target, handle consumption
                    if (!float.IsNaN(agent.remainingDistance) && agent.remainingDistance <= agent.stoppingDistance)
                    {
                        if (food.CanBeRecycled)
                        {
                            animationController.TriggerAction();
                            var money = ScoreStats.GetScoreForTrash(food.TrashType);
                            await food.RecycleAsync();
                            // apply recycle multiplier from upgrades
                            var reward = money * _recycleMultiplier;
                            CurrencyManager.AddMoney(reward);
                            
                            // Show popup at food position
                            ScoreGainedPopup.Show(transform.position, reward);
                            
                            Debug.Log($"CatHelper: recycled {food.GetName()} reward={reward}");
                        }

                        DebugState = "Consumed";
                        break;
                    }

                    await UniTask.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), cancellationToken: token);
                    waited += pollIntervalSeconds;
                    if (waited >= maxWaitSeconds)
                    {
                        DebugState = "Timeout waiting for target";
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // cancellation requested - treat as abort
                DebugState = "Consume cancelled";
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                // always clear going-to-target state so the helper can try again next tick
                _goingToTarget = false;
                animationController.GoingToTarget = false;
            }
        }
    }
}