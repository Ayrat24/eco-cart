using System;
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
        [SerializeField] private int maxDistanceFromPlayer = 30;
        [SerializeField] private LayerMask groundItemsMask;
        [SerializeField] private int playerStopDistance = 8;
        [SerializeField] private TrashItem eggPrefab;

        private CancellationTokenSource _cancellationTokenSource;

        private bool _initialized;

        public override void Init(CurrencyManager currencyManager, UpgradesCollection upgrades, Player player,
            int navmeshPriority)
        {
            base.Init(currencyManager, upgrades, player, navmeshPriority);

            var interval = TimeSpan.FromSeconds(5);
            Subscription = Observable.Interval(interval).Subscribe(x => { LayEgg(); });
            CancellationTokenSource = new CancellationTokenSource();
            
            agent.destination = player.transform.position;
            animationController.GoingToTarget = true;
            
            _initialized = true;
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
            var egg = PoolManager.Instance.GetTrash(eggPrefab);
            var spawnPos = transform.position;
            spawnPos.y = 0;
            egg.transform.position = spawnPos;
        }

    }
}