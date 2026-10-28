using System;
using System.Threading;
using Eco.Scripts.Upgrades;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        protected PlayerStats PlayerStats;
        
        protected IDisposable ActionSubscription;
        protected CancellationTokenSource CancellationTokenSource;

        public virtual void Init(CurrencyManager currencyManager, PlayerStats playerStats, Player player,
            int navmeshPriority)
        {
            Player = player;
            CurrencyManager = currencyManager;
            PlayerStats = playerStats;
            
            agent.avoidancePriority = navmeshPriority;
            
            animationController.Init(agent);
        }

        public virtual void Clear()
        {
            ActionSubscription?.Dispose();
            CancellationTokenSource?.Cancel();
            CancellationTokenSource?.Dispose();
            ActionSubscription = null;
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