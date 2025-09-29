using System;
using UnityEngine;
using UnityEngine.AI;

namespace Eco.Scripts.Helpers
{
    public class HelperAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        
        private NavMeshAgent _agent;
        private bool _initialized;
        
        private static readonly int State = Animator.StringToHash("State");
        private static readonly int Vert = Animator.StringToHash("Vert");
        private static readonly int ActionState = Animator.StringToHash("Recycle");
        
        public bool GoingToTarget { get; set; }

        public void Init(NavMeshAgent agent)
        {
            _agent = agent;
            _initialized = true;
        }

        public void TriggerAction()
        {
            animator.SetTrigger(ActionState);
        }
        
        private void FixedUpdate()
        {
            if (!_initialized)
            {
                return;
            }
            
            animator.SetFloat(Vert, _agent.velocity.magnitude / _agent.speed);
            animator.SetFloat(State, GoingToTarget ? 1 : 0);
        }
    }
}
