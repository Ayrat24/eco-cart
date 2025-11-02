using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.ItemCollecting;
using Eco.Scripts.Trash;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;
using UnityEngine.AI;

namespace Eco.Scripts.Helpers
{
    public class DogHelper : Helper
    {
        [SerializeField] private int maxDistanceFromPlayer = 30;
        [SerializeField] private LayerMask groundItemsMask;
        [SerializeField] private int playerStopDistance = 6;
        [SerializeField] private Transform holdPoint; // where the dog holds picked items

        private readonly Collider[] _colliders = new Collider[100];
        private Cart _cart;
        private bool _goingToTarget;

        public override void Init(CurrencyManager currencyManager, UpgradesCollection upgrades, Player player,
            int navmeshPriority)
        {
            base.Init(currencyManager, upgrades, player, navmeshPriority);

            // Try to find existing cart (player spawns cart as child)
            _cart = player.GetComponentInChildren<Cart>();

            // listen for cart changes from player as well
            player.OnCartChanged.Subscribe(c => { _cart = c; });

            var interval = TimeSpan.FromSeconds(1);
            Subscription = Observable.Interval(interval).Subscribe(_ => { TickFind(); });
            CancellationTokenSource = new CancellationTokenSource();

            _goingToTarget = false;
        }

        private void TickFind()
        {
            if (_goingToTarget) return;

            if (_cart == null)
            {
                DebugState = "No cart - following player";
                ReturnToPlayer();
                return;
            }

            // ask cart for most common TrashType
            var mostCommon = TrashType.Food;
            Debug.Log($"DogHelper: cart item count={1}, mostCommon={mostCommon}");

            // note: `mostCommon` is an enum (TrashType) and cannot be null; if cart-probing logic
            // is added later it should set an explicit 'none' value or make the type nullable

            // search for TrashItems of this type around player
            Vector3 center = Player.transform.position;
            float radiusConfigured = (searchRadius > 0) ? searchRadius : maxDistanceFromPlayer;
            float radiusToUse = Mathf.Max(radiusConfigured, maxDistanceFromPlayer);
            int layerMaskVal = (groundItemsMask.value != 0) ? groundItemsMask.value : ~0; // all layers as fallback
            int count = Physics.OverlapSphereNonAlloc(center, radiusToUse, _colliders, layerMaskVal);
            Debug.Log($"DogHelper: OverlapSphere found {count} colliders (radius {radiusToUse:F1})");

            var candidates = new List<TrashItem>();
            for (int i = 0; i < count; i++)
            {
                var col = _colliders[i];
                if (col == null) continue;
                if (!col.TryGetComponent<TrashItem>(out var t)) continue;

                // skip if already in cart or being picked up
                bool inDrop = (_cart != null && _cart.DropPoint != null) && t.transform.IsChildOf(_cart.DropPoint);
                if (inDrop) continue;
                if (t.IsBeingPickedUp) continue;
                if (!t.CanBeRecycled) continue;
                if (!t.gameObject.activeInHierarchy) continue;

                if (t.TrashType == mostCommon)
                {
                    candidates.Add(t);
                }
            }

            Debug.Log($"DogHelper: matching items count={candidates.Count} for type={mostCommon}");

            if (candidates.Count == 0)
            {
                DebugState = "No matching items found - following player";
                ReturnToPlayer();
                return;
            }

            // pick the furthest from the player (but within maxDistanceFromPlayer)
            TrashItem chosen = null;
            float maxDist = -1f;
            foreach (var it in candidates)
            {
                var d = Vector3.Distance(it.transform.position, Player.transform.position);
                if (d > maxDist && d <= maxDistanceFromPlayer)
                {
                    maxDist = d;
                    chosen = it;
                }
            }

            // if none within maxDistanceFromPlayer, follow player
            if (chosen == null)
            {
                DebugState = "No matching items within maxDistance - following player";
                ReturnToPlayer();
                return;
            }

            // Check navmesh path to chosen
            var targetPos = chosen.transform.position;
            var path = new NavMeshPath();
            bool hasPath = agent.CalculatePath(targetPos, path) && path.status == NavMeshPathStatus.PathComplete;
            if (!hasPath)
            {
                DebugState = "No path to target - following player";
                ReturnToPlayer();
                return;
            }

            // reserve the item so other collectors/dog won't steal it while dog moves
            try
            {
                chosen.SetPickedUpStatus(true);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            agent.stoppingDistance = 1.0f; // get close enough to pick
            agent.path = path;
            _goingToTarget = true;
            animationController.GoingToTarget = true;
            DebugState = "Going to dog-target";

            BringItemAsync(chosen, CancellationTokenSource.Token).Forget();
        }

        private void ReturnToPlayer()
        {
            DebugState = "Following player";
            agent.stoppingDistance = playerStopDistance;
            agent.destination = Player.transform.position;
            _goingToTarget = false;
            animationController.GoingToTarget = false;
        }

        private async UniTask BringItemAsync(TrashItem target, CancellationToken token)
        {
            const float pollIntervalSeconds = 0.15f;
            const float maxWaitSeconds = 25f;

            bool delivered = false;

            // quick validation
            if (!IsTargetValid(target))
            {
                DebugState = "Target gone";
                _goingToTarget = false;
                animationController.GoingToTarget = false;
                ReturnToPlayer();
                return;
            }

            // Move to the item
            if (!await MoveToItemAsync(target, 1.0f, token, pollIntervalSeconds, maxWaitSeconds))
            {
                DebugState = "Path invalid to target";
                ReleaseTarget(target);
                _goingToTarget = false;
                animationController.GoingToTarget = false;
                ReturnToPlayer();
                return;
            }

            // Pick up / parent to dog
            PickupAndHold(target);

            // Move back near the player
            if (!await MoveToPlayerAsync(1.0f, token, pollIntervalSeconds, maxWaitSeconds))
            {
                DebugState = "Path invalid to player";
                ReleaseTarget(target);
                _goingToTarget = false;
                animationController.GoingToTarget = false;
                ReturnToPlayer();
                Debug.LogWarning("DogHelper: could not reach player after fetching item");
                return;
            }

            // Drop the item near the player so ItemCollector can detect it
            DropItemNearPlayer(target);

            // Move into front position and wait while following player until pickup occurs
            bool pickedUpByPlayer = await MoveToFrontAndWaitAsync(target, token, pollIntervalSeconds, maxWaitSeconds);

            if (pickedUpByPlayer)
            {
                delivered = true;
                DebugState = "Player started pickup";
            }
            else
            {
                DebugState = "Player did not pick up (timeout)";
                Debug.Log("DogHelper: pickup timeout, releasing target");
            }

            // final cleanup
            if (!delivered)
            {
                ReleaseTarget(target);
            }

            _goingToTarget = false;
            animationController.GoingToTarget = false;
            ReturnToPlayer();
        }

        // Small helpers extracted from BringItemAsync
        private bool IsTargetValid(TrashItem target)
        {
            return target != null && target.gameObject != null && target.gameObject.activeInHierarchy;
        }

        private async UniTask<bool> MoveToItemAsync(TrashItem item, float stoppingDistance, CancellationToken token, float pollIntervalSeconds, float maxWaitSeconds)
        {
            if (item == null) return false;
            return await MoveToPositionAsync(item.transform, stoppingDistance, token, pollIntervalSeconds, maxWaitSeconds);
        }

        private void PickupAndHold(TrashItem target)
        {
            try
            {
                ParentToHold(target);
                // ensure the target is marked as being picked up
                target.SetPickedUpStatus(true);
                Debug.Log($"DogHelper: picked up '{target.GetName()}' and holding it");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private async UniTask<bool> MoveToPlayerAsync(float stoppingDistance, CancellationToken token, float pollIntervalSeconds, float maxWaitSeconds)
        {
            if (Player == null) return false;
            return await MoveToPositionAsync(Player.transform, stoppingDistance, token, pollIntervalSeconds, maxWaitSeconds);
        }

        // Repeatedly follow the position in front of the player and wait until the item is picked up or timeout
        private async UniTask<bool> MoveToFrontAndWaitAsync(TrashItem target, CancellationToken token, float pollIntervalSeconds, float maxWaitSeconds)
        {
            if (Player == null) return false;

            float waitDistance = 1.2f;
            Vector3 initialWaitPos = Player.transform.position + Player.transform.forward * waitDistance;

            // try to reach initial front position (best-effort)
            await MoveToWorldPositionAsync(initialWaitPos, 1.0f, token, pollIntervalSeconds, maxWaitSeconds);

            float waited = 0f;
            while (!token.IsCancellationRequested && waited < maxWaitSeconds)
            {
                if (target == null || !target.gameObject.activeInHierarchy)
                {
                    Debug.Log($"DogHelper: target gone while waiting (waited={waited:F1}s)");
                    return true;
                }

                if (target.IsBeingPickedUp)
                {
                    Debug.Log($"DogHelper: detected player started pickup (waited={waited:F1}s)");
                    return true;
                }

                Vector3 frontPos = Player.transform.position + Player.transform.forward * waitDistance;
                frontPos.y = transform.position.y;
                agent.stoppingDistance = 0.6f;
                agent.destination = frontPos;

                DebugState = $"Waiting for pickup; staying at {frontPos.x:F1},{frontPos.z:F1} (waited {waited:F1}s)";

                await UniTask.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), cancellationToken: token);
                waited += pollIntervalSeconds;
            }

            return false;
        }

        // Move the agent to a world-space position (Vector3). Returns true when arrived.
        private async UniTask<bool> MoveToWorldPositionAsync(Vector3 worldPosition, float stoppingDistance,
            CancellationToken token, float pollIntervalSeconds, float maxWaitSeconds)
        {
            float waited = 0f;

            var path = new NavMeshPath();
            if (!agent.CalculatePath(worldPosition, path) || path.status != NavMeshPathStatus.PathComplete)
            {
                Debug.LogWarning($"DogHelper: no path to world position {worldPosition}");
                return false;
            }

            agent.stoppingDistance = stoppingDistance;
            agent.path = path;

            while (!token.IsCancellationRequested)
            {
                if (!agent.hasPath && !agent.pathPending)
                {
                    if (!agent.CalculatePath(worldPosition, path) || path.status != NavMeshPathStatus.PathComplete)
                    {
                        Debug.LogWarning("DogHelper: lost path while moving to world position");
                        return false;
                    }

                    agent.path = path;
                }

                if (!float.IsNaN(agent.remainingDistance) && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                {
                    return true;
                }

                await UniTask.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), cancellationToken: token);
                waited += pollIntervalSeconds;
                if (waited >= maxWaitSeconds) return false;
            }

            return false;
        }

        // Move the agent to a Transform target by converting to a world position.
        private async UniTask<bool> MoveToPositionAsync(Transform position, float stoppingDistance,
            CancellationToken token, float pollIntervalSeconds, float maxWaitSeconds)
        {
            if (position == null) return false;
            Vector3 dest = GetDestination(position);
            return await MoveToWorldPositionAsync(dest, stoppingDistance, token, pollIntervalSeconds, maxWaitSeconds);
        }

        // Drop the item at a short offset in front of the player so ItemCollector can detect it.
        private void DropItemNearPlayer(TrashItem target)
        {
            if (target == null) return;

            var mb = target as MonoBehaviour;
            if (mb != null)
            {
                // Unparent from the dog hold point (if still parented)
                bool isHeld = (holdPoint != null)
                    ? mb.transform.IsChildOf(holdPoint)
                    : mb.transform.IsChildOf(this.transform);
                if (isHeld)
                {
                    mb.transform.SetParent(null, true);
                }

                // Place in front of player with a small random offset to avoid stacking exactly on player
                var forward = Player.transform.forward;
                Vector3 dropPos = Player.transform.position + forward * 1.2f + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0f, UnityEngine.Random.Range(-0.5f, 0.5f));
                // keep original height
                dropPos.y = mb.transform.position.y;
                mb.transform.position = dropPos;
                mb.transform.localRotation = Quaternion.identity;
            }

            // Make item physical again and available for collectors
            target.MakeKinematic(false);
            target.SetPickedUpStatus(false);
            target.SetInCartState(false);

            Debug.Log($"DogHelper: dropped item '{target.GetName()}' near player at {Player.transform.position}");
        }

        private Vector3 GetDestination(Transform target)
        {
            var pos = target.position;
            pos.y = 0;
            pos -= Vector3.forward;
            return pos;
        }

        private void ParentToHold(TrashItem target)
        {
            var hold = (holdPoint != null) ? holdPoint : this.transform;
            target.OnPickUp(hold);
            var mb = target as MonoBehaviour;
            if (mb != null)
            {
                mb.transform.SetParent(hold, false);
                mb.transform.localPosition = Vector3.zero;
                mb.transform.localRotation = Quaternion.identity;
                Debug.Log($"DogHelper: parented '{target.GetName()}' to hold point");
            }

            target.MakeKinematic(true);
            //target.SetPickedUpStatus(true);

            Debug.Log($"DogHelper: made '{target.GetName()}' kinematic and marked as being picked up");
        }

        private void ReleaseTarget(TrashItem target)
        {
            if (target == null) return;

            target.SetPickedUpStatus(false);
            target.MakeKinematic(false);
            var mb = target as MonoBehaviour;
            if (mb != null)
            {
                bool isHeld = (holdPoint != null)
                    ? mb.transform.IsChildOf(holdPoint)
                    : mb.transform.IsChildOf(this.transform);
                if (isHeld)
                {
                    mb.transform.SetParent(null, true);
                }
            }
        }
    }
}
