using System;
using System.Collections.Generic;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;
using UnityEngine.Serialization;

namespace Eco.Scripts.ItemCollecting
{
    public class ItemCollector : MonoBehaviour
    {
        [SerializeField] ItemRecycler itemRecycler;
        [SerializeField] SphereCollider sphereCollider;
        [SerializeField] LayerMask layerMask;

        [SerializeField] private List<CollectorHand> hands;

        private readonly Collider[] _colliders = new Collider[20];
        private readonly Queue<Collider> _colliderQueue = new();
        private const int MaxQueueSize = 5;

        private Cart _cart;
        private IDisposable _subscription;

        public LayerMask LayerMask => layerMask;

        public void Init(CurrencyManager currencyManager, UpgradesCollection upgrades, Cart cart)
        {
            _cart = cart;

            foreach (var hand in hands)
            {
                hand.Init(cart);
            }

            sphereCollider.includeLayers = layerMask;

            _subscription?.Dispose();
            _subscription = Observable.IntervalFrame(10).Subscribe(x =>
            {
                if (!cart.CanAddItems || !HasFreeHands())
                {
                    return;
                }

                ScanForItems();
            });

            itemRecycler.Init(currencyManager, upgrades);
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

            float shortestDistance = Mathf.Infinity;
            Collider shortestCollider = null;

            for (int i = 0; i < count; i++)
            {
                if (_colliderQueue.Contains(_colliders[i]))
                {
                    continue;
                }

                var distance = Vector3.Distance(_colliders[i].transform.position, transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    shortestCollider = _colliders[i];
                }
            }

            if (shortestCollider == null)
            {
                return;
            }

            _colliderQueue.Enqueue(shortestCollider);

            if (_colliderQueue.Count > MaxQueueSize)
            {
                _colliderQueue.Dequeue();
            }

            if (GetClosestFreeHand(shortestCollider.transform.position, out var hand))
            {
                PickItem(hand, shortestCollider);
            }
        }

        private void PickItem(CollectorHand hand, Collider other)
        {
            if (_cart.IsFull)
            {
                return;
            }

            var item = other.GetComponent<ICartItem>();
            if (item is { IsBeingPickedUp: false } && _cart.CanFitItem(item))
            {
                //Don't grab if other helpers already grabbing it
                item.SetInCartState(true);
                item.SetPickedUpStatus(true);
                _cart.AddToCart(item, other);
                hand.PlayAnimation(item, other);
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
    }
}