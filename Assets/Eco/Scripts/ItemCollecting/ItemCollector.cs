using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Tools;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;

namespace Eco.Scripts.ItemCollecting
{
    public class ItemCollector : Tool
    {
        [SerializeField] ItemRecycler itemRecycler;
        [SerializeField] SphereCollider sphereCollider;
        [SerializeField] LayerMask layerMask;
        [SerializeField] private List<CollectorHand> hands;

        private readonly Collider[] _colliders = new Collider[20];
        private Cart _cart;
        private IDisposable _subscription;

        public LayerMask LayerMask => layerMask;

        public void Init(CurrencyManager currencyManager, ScoreStats scoreStats, Cart cart)
        {
            _cart = cart;

            foreach (var hand in hands)
            {
                hand.Init(cart);
            }

            sphereCollider.includeLayers = layerMask;

            _subscription?.Dispose();
            _subscription = Observable.IntervalFrame(10).Subscribe(_ =>
             {
                 if (!Active || !cart.CanAddItems || !HasFreeHands())
                 {
                     return;
                 }

                 ScanForItems();
             });

            itemRecycler.Init(currencyManager, scoreStats);
            cart.Init(itemRecycler, this);
        }

        private void ScanForItems()
        {
            Vector3 center = sphereCollider.transform.TransformPoint(sphereCollider.center);
            int count = Physics.OverlapSphereNonAlloc(center, sphereCollider.radius, _colliders, layerMask);

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

                // Check if cart can accept this item
                if (!_cart.CanFitItem(item))
                    continue;

                // Find the closest free hand for this item
                if (!GetClosestFreeHand(col.transform.position, out var hand))
                    continue;

                // Double-check item still not being picked (race) and hand/cart still valid
                if (item.IsBeingPickedUp || _cart.IsFull || !hand.IsFree)
                    continue;

                // Reserve the item immediately so no other scan picks it.
                // Mark as in-cart and mark as being picked up; do this in a safe order and rollback on failure.
                item.SetInCartState(true);
                item.SetPickedUpStatus(true);

                // Attempt to add to cart. If AddToCart returns false, rollback reservation immediately.
                bool added = false;
                try
                {
                    added = _cart.AddToCart(item, col);
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
                // it must clean up its own state; AddToCart already recorded the item in the cart.
                try
                {
                    hand.PlayAnimation(item, col);
                }
                catch (Exception)
                {
                    // If PlayAnimation threw synchronously (unlikely), rollback the cart entry and flags.
                    try { _cart.RemoveFromCart(item); } catch { }
                    try { item.SetPickedUpStatus(false); } catch { }
                    try { item.SetInCartState(false); } catch { }
                    continue;
                }

                // We picked one item this tick — stop.
                break;
            }
        }

        private void PickItem(CollectorHand hand, Collider other)
        {
            if (_cart.IsFull)
            {
                return;
            }

            var item = other.GetComponent<ICartItem>();
            if (item == null) return;

            if (item.IsBeingPickedUp || !_cart.CanFitItem(item) || !hand.IsFree)
                return;

            // Reserve and attempt pick (same safe flow as in ScanForItems)
            item.SetInCartState(true);
            item.SetPickedUpStatus(true);

            bool added = false;
            try
            {
                added = _cart.AddToCart(item, other);
            }
            catch (Exception)
            {
                added = false;
            }

            if (!added)
            {
                try { item.SetPickedUpStatus(false); } catch { }
                try { item.SetInCartState(false); } catch { }
                return;
            }

            try
            {
                hand.PlayAnimation(item, other);
            }
            catch (Exception)
            {
                try { _cart.RemoveFromCart(item); } catch { }
                try { item.SetPickedUpStatus(false); } catch { }
                try { item.SetInCartState(false); } catch { }
            }
        }

        private bool HasFreeHands()
        {
            foreach (var hand in hands)
            {
                if (hand.IsFree)
                {
                    return true;
                }
            }

            return false;
        }

        private bool GetClosestFreeHand(Vector3 itemPos, out CollectorHand freeHand)
        {
            freeHand = null;
            float minDistance = float.MaxValue;
            bool foundHand = false;

            foreach (var hand in hands)
            {
                if (!hand.IsFree)
                {
                    continue;
                }

                var distance = Vector3.Distance(hand.Position, itemPos);

                if (distance < minDistance)
                {
                    freeHand = hand;
                    minDistance = distance;
                    foundHand = true;
                }
            }

            return foundHand;
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
            Gizmos.DrawWireSphere(center, sphereCollider.radius);
        }

        public override UniTask Enable()
        {
            _cart.gameObject.SetActive(true);
            Active = true;
            return UniTask.CompletedTask;
        }

        public override async UniTask Disable()
        {
            Active = false;
            await UniTask.WaitUntil(AllHandsAreFree);
            _cart.gameObject.SetActive(false);
        }
    }
}