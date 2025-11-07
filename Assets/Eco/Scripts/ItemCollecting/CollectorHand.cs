using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Utils;
using PrimeTween;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Eco.Scripts.ItemCollecting
{
    public class CollectorHand : MonoBehaviour
    {
        [SerializeField] string handName;
        [SerializeField] Transform ikTarget;
        [SerializeField] IKExtendBones ik;
        [SerializeField] private Vector3 animationMiddlePoint;

        [SerializeField] private float pickAnimationDuration;
        [SerializeField] private float placeAnimationDuration;
        [SerializeField] private float swingBackAnimationDuration;

        private Vector3 _initialPosition;
        private Transform _baseATargetParent;
        private bool _animationInProgress;
        private Cart _cart;
        private IDisposable _subscription;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly List<Tween> _tweens = new();

        public Vector3 Position => ikTarget.TransformPoint(_initialPosition);
        public bool IsFree => !_animationInProgress;

        private void Start()
        {
            _initialPosition = ikTarget.localPosition;
            _baseATargetParent = ikTarget.parent;
        }

        public void Init(Cart cart)
        {
            _cart = cart;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void PlayAnimation(ICartItem item, Collider other)
        {
            PlayPickUpAnimationAsync(item, other, _cancellationTokenSource.Token).Forget();
        }

        private async UniTask PlayPickUpAnimationAsync(ICartItem item, Collider other, CancellationToken token)
        {
            _animationInProgress = true;

            // Helper: non-throwing wait that respects cancellation token
            async UniTask<bool> WaitSeconds(float seconds, CancellationToken ct)
            {
                float waited = 0f;
                while (waited < seconds)
                {
                    if (ct.IsCancellationRequested) return false;
                    await UniTask.Yield();
                    waited += Time.deltaTime;
                }
                return true;
            }

            void CleanupAndClearTweens()
            {
                _animationInProgress = false;
                foreach (var tween in _tweens) tween.Stop();
                _tweens.Clear();
            }

            void RollbackItem(ICartItem it)
            {
                if (it == null) return;
                _cart.RemoveFromCart(it);
                it.SetPickedUpStatus(false);
                it.SetInCartState(false);
            }

            // Resolve the item's root transform (ICartItem implementations are MonoBehaviours at runtime)
            var itemMb = item as MonoBehaviour;
            var itemTransform = itemMb != null ? itemMb.transform : (other != null ? other.transform : null);

            // Basic null checks - item or other might be destroyed during async work
            if (item == null || itemTransform == null)
            {
                RollbackItem(item);
                CleanupAndClearTweens();
                return;
            }

            // Step 1: Place hand on the item (use item's root transform)
            ik.Target = ikTarget;
            ikTarget.parent = itemTransform;
            ik.enabled = true;

            _tweens.Add(Tween.LocalPosition(ikTarget, Vector3.zero, pickAnimationDuration));

            var waitedPick = await WaitSeconds(pickAnimationDuration, token);
            if (!waitedPick)
            {
                // cancelled
                RollbackItem(item);
                CleanupAndClearTweens();
                return;
            }

            // Step 2 : Place hand above the drop point
            if (itemTransform == null)
            {
                RollbackItem(item);
                CleanupAndClearTweens();
                return;
            }

            // Reparent the item to the DropPoint but preserve its world position so it doesn't jump.
            var itemMbLocal = item as MonoBehaviour;
            if (itemMbLocal != null && _cart != null && _cart.DropPoint != null)
            {
                // Parent the item to the drop point but preserve world position first so there's no jump.
                itemMbLocal.transform.SetParent(_cart.DropPoint, true);

                // Compute local-space quadratic Bezier control so the curve is relative to the moving DropPoint.
                Vector3 startLocal = itemMbLocal.transform.localPosition;
                Vector3 targetLocal = Vector3.zero;
                // Convert animationMiddlePoint (designer offset) into DropPoint local space.
                Vector3 middleLocal = _cart.DropPoint.InverseTransformVector(animationMiddlePoint);
                Vector3 controlLocal = (startLocal + targetLocal) * 0.5f + middleLocal;

                _tweens.Add(Tween.Custom(0f, 1f, placeAnimationDuration, ease: Ease.Linear, onValueChange: (t) =>
                {
                    float u = 1 - t;
                    Vector3 pos = u * u * startLocal + 2 * u * t * controlLocal + t * t * targetLocal;
                    itemMbLocal.transform.localPosition = pos;
                }));
            }
            else
            {
                // Fallback: animate world-space quadratic Bezier, re-sampling target each frame so we follow a moving DropPoint.
                Vector3 startWorldPos = itemTransform.position;
                _tweens.Add(Tween.Custom(0f, 1f, placeAnimationDuration, ease: Ease.Linear, onValueChange: (t) =>
                {
                    Vector3 currentTarget = (_cart != null && _cart.DropPoint != null) ? _cart.DropPoint.position : startWorldPos;
                    Vector3 control = (startWorldPos + currentTarget) * 0.5f + animationMiddlePoint;
                    float u = 1 - t;
                    Vector3 pos = u * u * startWorldPos + 2 * u * t * control + t * t * currentTarget;
                    if (itemTransform != null) itemTransform.position = pos;
                }));
            }

            var waitedPlace = await WaitSeconds(placeAnimationDuration, token);
            if (!waitedPlace)
            {
                // cancelled during placement
                RollbackItem(item);
                CleanupAndClearTweens();
                return;
            }

            if (itemTransform == null)
            {
                RollbackItem(item);
                CleanupAndClearTweens();
                return;
            }

            // Finalize pickup: parent the item to the drop point and keep it kinematic so it follows the cart exactly.
            item.OnPickUp(_cart.DropPoint);
            var itemMbFinalize = item as MonoBehaviour;
            if (itemMbFinalize != null && _cart != null && _cart.DropPoint != null)
            {
                // Parent without preserving world position to ensure local alignment, then zero local transform.
                itemMbFinalize.transform.SetParent(_cart.DropPoint, false);

                float offset = 0.02f;
                var pos = new Vector3(Random.Range(-offset, offset),Random.Range(-offset, offset), Random.Range(-offset, offset));
                itemMbFinalize.transform.localPosition = pos;
                itemMbFinalize.transform.localRotation = Quaternion.identity;
            }
            // Keep kinematic true while item is in the cart to prevent physics from moving it off the drop point.
            item.MakeKinematic(true);

            // Step 3: Return hand to the body
            ikTarget.parent = _baseATargetParent;
            if (itemTransform != null)
                ikTarget.position = itemTransform.position;
            ik.Target = ikTarget;
            
            item.MakeKinematic(false);
            _tweens.Add(Tween.LocalPosition(ikTarget, _initialPosition, swingBackAnimationDuration));

            var waitedSwing = await WaitSeconds(swingBackAnimationDuration, token);
            if (!waitedSwing)
            {
                // cancelled during swing back - treat as done but ensure cleanup
                CleanupAndClearTweens();
                return;
            }

            ik.enabled = false;

            // Completed successfully: clear picked up flag so other systems consider it placed.
            item.SetPickedUpStatus(false);

            
            CleanupAndClearTweens();
        }

        public void Clear()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            foreach (var tween in _tweens)
            {
                tween.Stop();
            }

            _tweens.Clear();

            _subscription?.Dispose();
        }
    }
}