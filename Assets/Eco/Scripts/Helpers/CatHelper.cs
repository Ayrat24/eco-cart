using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Trash;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;

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

        public override void Init(CurrencyManager currencyManager, UpgradesCollection upgrades, Player player,
            int navmeshPriority)
        {
            base.Init(currencyManager, upgrades, player, navmeshPriority);
            
            var interval = TimeSpan.FromSeconds(1);
            Subscription = Observable.Interval(interval).Subscribe(x => { GoToNearbyItem(); });
            CancellationTokenSource = new CancellationTokenSource();
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
                Vector3.Distance(x.transform.position, Player.transform.position) -
                Vector3.Distance(y.transform.position, Player.transform.position) > 0
                    ? -1
                    : 1);

            var food = trashItems[0];

            if (Vector3.Distance(food.transform.position, Player.transform.position) > maxDistanceFromPlayer)
            {
                ReturnToPlayer();
                return;
            }

            agent.stoppingDistance = 3;
            agent.destination = food.transform.position;
            _goingToTarget = true;
            animationController.GoingToTarget = true;

            DebugState = "Going to trash";

            ConsumeFoodAsync(food, CancellationTokenSource.Token).Forget();
        }

        private void ReturnToPlayer()
        {
            DebugState = "Going around player";
            agent.stoppingDistance = playerStopDistance;
            agent.destination = Player.transform.position;
        }

        private async UniTask ConsumeFoodAsync(TrashItem food, CancellationToken token)
        {
            await UniTask.WaitUntil(() =>
                    Vector3.Distance(food.transform.position, transform.position) <= agent.stoppingDistance,
                cancellationToken: token);

            if (food.CanBeRecycled)
            {
                animationController.TriggerAction();
                var money = UpgradesCollection.TrashScoreUpgrades[food.TrashType].ScoreForCurrentUpgrade;
                await food.RecycleAsync();
                CurrencyManager.AddMoney(money);
            }

            _goingToTarget = false;
            animationController.GoingToTarget = false;
        }
    }
}