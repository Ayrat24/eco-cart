using System;
using Eco.Scripts.Cart;
using R3;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Eco.Scripts.Helpers
{
    public class CollectorHelper : MonoBehaviour
    {
        [SerializeField] NavMeshAgent agent;
        [SerializeField] private int searchRadius;
        [SerializeField] private Cart.Cart cart;
        [SerializeField] ItemCollector itemCollector;
        [SerializeField] private int playerStopDistance = 8;
        
        private IDisposable _subscription;
        private readonly Collider[] _colliders = new Collider[100];
        private Player _player;
        private string _state = "";

        public void Init(MoneyController moneyController, UpgradesCollection upgrades, Player player)
        {
            _player = player;

            itemCollector.Init(moneyController, upgrades);

            var interval = TimeSpan.FromSeconds(1);
            _subscription = Observable.Interval(interval).Subscribe(x => { GoToNearbyItem(); });
        }

        private void GoToNearbyItem()
        {
            if (cart.IsFull)
            {
                _state = "Emptying";
                cart.EmptyCart();
                return;
            }

            Vector3 center = transform.position;
            int count = Physics.OverlapSphereNonAlloc(center, searchRadius, _colliders, itemCollector.LayerMask);

            float shortestDistance = Mathf.Infinity;
            Collider shortestCollider = null;

            for (int i = 0; i < count; i++)
            {
                var distance = Vector3.Distance(_colliders[i].transform.position, transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    shortestCollider = _colliders[i];
                }
            }

            if (shortestCollider == null)
            {
                _state = "Going around player";

                Debug.LogError("Go To Player");
                if (!agent.hasPath)
                {
                    _state = "Making path around player";
                    agent.destination = MoveAroundCircle(transform.position, _player.transform.position, playerStopDistance, 1);
                }

                return;
            }

            _state = "Going to trash";
            agent.destination = shortestCollider.transform.position;
        }

        public Vector3 MoveAroundCircle(Vector3 currentPos, Vector3 center, float targetRadius, float maxAngleChange)
        {
            // Step 1: Move position to the exact target radius
            Vector3 direction = (currentPos - center).normalized; 
            Vector3 onCirclePos = center + direction * targetRadius;

            // Step 2: Get current angle
            float currentAngle = Mathf.Atan2(onCirclePos.z - center.z, onCirclePos.x - center.x);

            // Step 3: Apply small random rotation
            float angleChange = maxAngleChange;
            float newAngle = currentAngle + angleChange;

            // Step 4: Calculate new position on the circle
            float newX = Mathf.Cos(newAngle) * targetRadius;
            float newZ = Mathf.Sin(newAngle) * targetRadius;

            return new Vector3(center.x + newX, currentPos.y, center.z + newZ);
        }

        private void OnDestroy()
        {
            _subscription.Dispose();
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, searchRadius);
            
            var pos = transform.position + Vector3.up * 2f;
            Handles.Label(pos
                , _state);
        }
    }
}