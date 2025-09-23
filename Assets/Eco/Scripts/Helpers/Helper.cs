using Eco.Scripts.Upgrades;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Eco.Scripts.Helpers
{
    public abstract class Helper : MonoBehaviour
    {
        [SerializeField] protected NavMeshAgent agent;
        [SerializeField] protected int searchRadius;
        protected string DebugState;

        public abstract void Init(CurrencyManager currencyManager, UpgradesCollection upgrades, Player player,
            int navmeshPriority);

        public abstract void Clear();
        
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
