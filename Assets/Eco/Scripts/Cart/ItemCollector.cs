using System;
using System.Collections.Generic;
using R3;
using Unity.Mathematics;
using UnityEngine;

public class ItemCollector : MonoBehaviour
{
    [SerializeField] SphereCollider sphereCollider;
    [SerializeField] LayerMask layerMask;
    [SerializeField] Cart cart;

    private readonly Collider[] _colliders = new Collider[20];
    private readonly Queue<Collider> _colliderQueue = new();
    private const int MaxQueueSize = 3;

    private bool _skipScan;

    private void Start()
    {
        //sphereCollider.enabled = false;
        sphereCollider.includeLayers = layerMask;

        var subscription = Observable.Interval(TimeSpan.FromSeconds(1f)).Subscribe(x =>
        {
            if (_skipScan)
            {
                _skipScan = false;
                return;
            }

            ScanForItems();
        });
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

        PickItem(shortestCollider);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_skipScan)
        {
            return;
        }

        _skipScan = true;

        PickItem(other);
    }

    private void PickItem(Collider other)
    {
        Debug.Log("Picking item");
        var item = other.GetComponentInParent<ICartItem>();
        if (item != null)
        {
            Debug.Log("Picking item2");
            cart.PickUpItem(item, other);
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