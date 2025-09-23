using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Trash;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;

namespace Eco.Scripts.Helpers
{
    public class CatHelper : Helper
    {
        [SerializeField] private Animator animator;
        [SerializeField] private int maxDistanceFromPlayer = 30;
        [SerializeField] private LayerMask groundItemsMask;
        [SerializeField] private int playerStopDistance = 8;

        private Player _player;
        private readonly Collider[] _colliders = new Collider[50];
        private IDisposable _subscription;
        private const TrashType Food = TrashType.Food;

        private bool _goingToTarget;

        private static readonly int State = Animator.StringToHash("State");
        private static readonly int Vert = Animator.StringToHash("Vert");
        private static readonly int Recycle = Animator.StringToHash("Recycle");

        
        public override void Init(CurrencyManager currencyManager, UpgradesCollection upgrades, Player player,
            int navmeshPriority)
        {
            _player = player;
            agent.avoidancePriority = navmeshPriority;

            var interval = TimeSpan.FromSeconds(1);
            _subscription = Observable.Interval(interval).Subscribe(x => { GoToNearbyItem(); });
        }

        public override void Clear()
        {
            _subscription?.Dispose();
        }

        private void FixedUpdate()
        {
            animator.SetFloat(Vert, agent.velocity.magnitude / agent.speed);
            animator.SetFloat(State, _goingToTarget ? 1 : 0);
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

            trashItems.Sort((x, y) =>
                Vector3.Distance(x.transform.position, _player.transform.position) -
                Vector3.Distance(y.transform.position, _player.transform.position) > 0
                    ? -1
                    : 1);

            var food = trashItems[0];

            if (Vector3.Distance(food.transform.position, _player.transform.position) > maxDistanceFromPlayer)
            {
                ReturnToPlayer();
                return;
            }

            agent.stoppingDistance = 3;
            agent.destination = food.transform.position;
            _goingToTarget = true;
            DebugState = "Going to trash";

            ConsumeFoodAsync(food).Forget();
        }

        private void ReturnToPlayer()
        {
            DebugState = "Going around player";
            agent.stoppingDistance = playerStopDistance;
            agent.destination = _player.transform.position;
        }

        private async UniTask ConsumeFoodAsync(TrashItem food)
        {
            await UniTask.WaitUntil(() =>
                Vector3.Distance(food.transform.position, transform.position) <= agent.stoppingDistance);

            if (food.CanBeRecycled)
            {
                animator.SetTrigger(Recycle);
                await food.RecycleAsync();
            }

            _goingToTarget = false;
        }
    }
}