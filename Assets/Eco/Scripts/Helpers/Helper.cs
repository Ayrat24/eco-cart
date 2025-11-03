using System;
using System.Collections.Generic;
using System.Threading;
using Eco.Scripts.Upgrades;
using R3;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Eco.Scripts.Helpers
{
    [RequireComponent(typeof(HelperAnimationController))]
    public abstract class Helper : MonoBehaviour
    {
        [SerializeField] protected NavMeshAgent agent;
        [SerializeField] protected int searchRadius;
        [SerializeField] protected HelperAnimationController animationController;
        
        protected string DebugState;
        protected Player Player;
        protected CurrencyManager CurrencyManager;
        protected ScoreStats ScoreStats;
        protected List<HelperUpgrade> Upgrades; 
        
        protected IDisposable ActionSubscription;
        protected CancellationTokenSource CancellationTokenSource;
        
        protected readonly List<IDisposable> UpgradeSubscriptions = new();

        
        public virtual void Init(CurrencyManager currencyManager, ScoreStats scoreStats, Player player,
            int navmeshPriority, List<HelperUpgrade> helperClassUpgrades)
        {
            Player = player;
            CurrencyManager = currencyManager;
            ScoreStats = scoreStats;
            Upgrades = helperClassUpgrades;
            
            agent.avoidancePriority = navmeshPriority;
            
            animationController.Init(agent);
        }
        
        protected void SetupUpgrades(List<HelperUpgrade> helperClassUpgrades)
        {
            for (int i = 0; i < helperClassUpgrades.Count; i++)
            {
                var u = helperClassUpgrades[i];

                // capture loop variables into locals to avoid closure capture issues
                int index = i;
                var upgrade = u;

                // subscribe to level changes and apply immediately
                var sub = upgrade.CurrentLevel.Subscribe(_ => { ApplyHelperUpgrade(upgrade, index); });
                UpgradeSubscriptions.Add(sub);

                // apply current values right away
                ApplyHelperUpgrade(upgrade, index);
            }
        }

        protected virtual void ApplyHelperUpgrade(HelperUpgrade upgrade, int index)
        {
            // to be overridden by subclasses
        }

        public virtual void Clear()
        {
            ActionSubscription?.Dispose();
            CancellationTokenSource?.Cancel();
            CancellationTokenSource?.Dispose();
            ActionSubscription = null;
            
            foreach (var s in UpgradeSubscriptions)
            {
                try { s.Dispose(); } catch (Exception ex) { Debug.LogException(ex); }
            }
            UpgradeSubscriptions.Clear();
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, searchRadius);

            var pos = transform.position + Vector3.up * 2f;
            Handles.Label(pos
                , DebugState);
        }
#endif
    }
}