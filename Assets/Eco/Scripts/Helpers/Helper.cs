using System;
using System.Threading;
using Eco.Scripts.Upgrades;
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
        protected UpgradesCollection UpgradesCollection;
        
        protected IDisposable Subscription;
        protected CancellationTokenSource CancellationTokenSource;

        
        public virtual void Init(CurrencyManager currencyManager, UpgradesCollection upgrades, Player player,
            int navmeshPriority)
        {
            Player = player;
            CurrencyManager = currencyManager;
            UpgradesCollection = upgrades;
            agent.avoidancePriority = navmeshPriority;
            
            animationController.Init(agent);
        }

        public virtual void Clear()
        {
            Subscription?.Dispose();
            CancellationTokenSource?.Cancel();
            CancellationTokenSource?.Dispose();
            Subscription = null;
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