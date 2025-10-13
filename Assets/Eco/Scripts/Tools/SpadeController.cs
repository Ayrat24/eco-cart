using System;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Trash;
using Eco.Scripts.Utils;
using R3;
using UnityEngine;

namespace Eco.Scripts.Tools
{
    public class SpadeController : Tool
    {
        [SerializeField] private Transform leftHandPosition;
        [SerializeField] private Transform rightHandPosition;
        [SerializeField] private IKExtendBones rightHandIK;
        [SerializeField] private IKExtendBones leftHandIK;
        [SerializeField] private Transform rightHanIKTarget;
        [SerializeField] private Transform leftHandIKTarget;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject spade;

        [SerializeField] private SphereCollider sphereCollider;
        [SerializeField] LayerMask layerMask;

        private static readonly int DigTriggerId = Animator.StringToHash("dig");
        private readonly Collider[] _colliders = new Collider[3];
        private IDisposable _subscription;
        private bool _isDigging;
        private Transform _leftStartParent;
        private Transform _rightStartParent;

        private void Awake()
        {
            _subscription = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(x =>
            {
                if (!Active)
                {
                    return;
                }

                ScanForItems();
            });
            
            _leftStartParent = leftHandIKTarget.parent;
            _rightStartParent = rightHanIKTarget.parent;
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

            var pile = shortestCollider.gameObject.GetComponent<TrashPile>();
            if (pile == null)
            {
                return;
            }

            Dig(pile);
        }

        private void Dig(TrashPile trashPile)
        {
            if (!Active)
            {
                return;
            }

            trashPile.Dig();
            animator.SetTrigger(DigTriggerId);
        }

        private void SwitchIK(bool active)
        {
            if (active)
            {
                leftHandIKTarget.parent = leftHandPosition;
                leftHandIKTarget.localPosition = Vector3.zero;
                rightHanIKTarget.parent = rightHandPosition;
                rightHanIKTarget.localPosition = Vector3.zero;
            }
            else
            {
                leftHandIKTarget.parent = _leftStartParent;
                rightHanIKTarget.parent = _rightStartParent;
            }

            leftHandIK.enabled = active;
            rightHandIK.enabled = active;
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
        }

        public override async UniTask Enable()
        {
            Active = true;
            spade.SetActive(true);
            SwitchIK(true);
            await UniTask.WaitForEndOfFrame();
        }

        public override async UniTask Disable()
        {
            if (!Active)
            {
                spade.SetActive(false);
                return;
            }

            Active = false;
            await UniTask.WaitWhile(() => _isDigging);
            spade.SetActive(false);
            SwitchIK(false);
            await UniTask.WaitForEndOfFrame();
        }
    }
}