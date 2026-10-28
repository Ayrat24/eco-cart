using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Profiling;
using Eco.Scripts.Tools;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;

namespace Eco.Scripts.ItemCollecting
{
    /// <summary>
    /// Finds scrap in reach and sends free tentacles for it. How many tentacles (LimbCount) and how far they reach
    /// (LimbRange, around the sphere collider's center) come from <see cref="PlayerStats"/>; tentacles past the
    /// authored ones are cloned from the first.
    /// </summary>
    public class ItemCollector : Tool
    {
        [SerializeField] ItemRecycler itemRecycler;
        [SerializeField] SphereCollider sphereCollider;
        [SerializeField] LayerMask layerMask;
        [SerializeField] private List<CollectorHand> hands;

        private readonly Collider[] _colliders = new Collider[20];
        private Stomach _stomach;
        private IDisposable _subscription;
        private int _limbCount;
        private float _range;

        public LayerMask LayerMask => layerMask;

        public void Init(CurrencyManager currencyManager, PlayerStats stats, Stomach stomach)
        {
            _stomach = stomach;
            _range = sphereCollider.radius;
            _limbCount = hands.Count;

            foreach (var hand in hands)
            {
                hand.Init(stomach);
            }

            sphereCollider.includeLayers = layerMask;

            var builder = new DisposableBuilder();
            Observable.IntervalFrame(10).Subscribe(_ =>
             {
                 if (!Active || !stomach.CanAddItems || !HasFreeHands())
                 {
                     return;
                 }

                 ScanForItems();
             }).AddTo(ref builder);
            stats.LimbCount.Subscribe(SetLimbCount).AddTo(ref builder);
            stats.LimbRange.Subscribe(range => _range = range).AddTo(ref builder);

            _subscription?.Dispose();
            _subscription = builder.Build();

            itemRecycler.Init(currencyManager, stats);
            stomach.Init(itemRecycler, this);
        }

        // Only the first count hands go for items; missing ones are grown from the first hand's tentacle
        private void SetLimbCount(int count)
        {
            _limbCount = Mathf.Max(1, count);
            while (hands.Count < _limbCount)
            {
                var template = hands[0].Tentacle;
                var tentacle = Instantiate(template, template.transform.parent);
                tentacle.name = $"{template.name} {hands.Count + 1}";

                var hand = gameObject.AddComponent<CollectorHand>();
                hand.Setup(tentacle, $"Hand {hands.Count + 1}");
                hand.Init(_stomach);
                hands.Add(hand);
            }
        }

        private void ScanForItems()
        {
            using var marker = EcoProfilerMarkers.ScanForItems.Auto();

            // Reach grows with the body
            float growth = _stomach.Body.Growth;
            var localCenter = sphereCollider.center;
            localCenter.z *= growth;
            Vector3 center = sphereCollider.transform.TransformPoint(localCenter);
            int count = Physics.OverlapSphereNonAlloc(center, _range * growth, _colliders, layerMask);

            if (count == 0)
            {
                return;
            }

            // Build a small candidate list sorted by distance so we attempt nearest items first.
            var candidates = new List<(Collider col, float dist)>();
            for (int i = 0; i < count; i++)
            {
                var col = _colliders[i];
                if (col == null) continue;
                float dist = Vector3.Distance(col.transform.position, transform.position);
                candidates.Add((col, dist));
            }

            if (candidates.Count == 0) return;

            candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

            // Try each candidate in distance order and pick the first we can successfully reserve and assign.
            foreach (var cand in candidates)
            {
                var col = cand.col;
                if (col == null) continue;

                var item = col.GetComponent<ICartItem>();
                if (item == null) continue;

                // If someone else is already grabbing it, skip
                if (item.IsBeingPickedUp)
                    continue;

                // Check if the stomach has room for this item
                if (!_stomach.CanFitItem(item))
                    continue;

                if (!GetFreeHand(out var hand))
                    continue;

                // Double-check item still not being picked (race) and hand/stomach still valid
                if (item.IsBeingPickedUp || _stomach.IsFull || !hand.IsFree)
                    continue;

                // Reserve the item immediately so no other scan picks it.
                // Mark as in-stomach and mark as being picked up; do this in a safe order and rollback on failure.
                item.SetInCartState(true);
                item.SetPickedUpStatus(true);

                // Attempt to reserve room in the stomach. If that fails, rollback reservation immediately.
                bool added = false;
                try
                {
                    added = _stomach.Reserve(item);
                }
                catch (Exception)
                {
                    added = false;
                }

                if (!added)
                {
                    // rollback reservation
                    try { item.SetPickedUpStatus(false); } catch { }
                    try { item.SetInCartState(false); } catch { }
                    continue;
                }

                // Start hand animation (runs async). We intentionally don't wait here. If animation later fails internally
                // it must clean up its own state; Reserve already counted the item in the stomach.
                try
                {
                    hand.PlayAnimation(item, col);
                }
                catch (Exception)
                {
                    // If PlayAnimation threw synchronously (unlikely), rollback the stomach entry and flags.
                    try { _stomach.Release(item); } catch { }
                    try { item.SetPickedUpStatus(false); } catch { }
                    try { item.SetInCartState(false); } catch { }
                    continue;
                }

                // We picked one item this tick — stop.
                break;
            }
        }

        private bool HasFreeHands()
        {
            return GetFreeHand(out _);
        }

        // Hands sprout wherever on the body faces the item, so any free one will do
        private bool GetFreeHand(out CollectorHand freeHand)
        {
            for (int i = 0; i < _limbCount && i < hands.Count; i++)
            {
                if (hands[i].IsFree)
                {
                    freeHand = hands[i];
                    return true;
                }
            }

            freeHand = null;
            return false;
        }

        public bool AllHandsAreFree()
        {
            foreach (var hand in hands)
            {
                if (!hand.IsFree)
                {
                    return false;
                }
            }

            return true;
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();

            foreach (var hand in hands)
            {
                hand.Clear();
            }
        }

        void OnDrawGizmos()
        {
            if (sphereCollider == null) return;

            Gizmos.color = Color.magenta;
            Vector3 center = sphereCollider.transform.TransformPoint(sphereCollider.center);
            Gizmos.DrawWireSphere(center, Application.isPlaying ? _range : sphereCollider.radius);
        }

        public override UniTask Enable()
        {
            Active = true;
            return UniTask.CompletedTask;
        }

        public override async UniTask Disable()
        {
            Active = false;
            await UniTask.WaitUntil(AllHandsAreFree);
        }
    }
}