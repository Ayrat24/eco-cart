using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Utils;
using NUnit.Framework;
using PrimeTween;
using R3;
using UnityEngine;

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
            
            //Step 1: Place hand on the item
            ik.Target = ikTarget;
            ikTarget.parent = other.transform;
            ik.enabled = true;

            _tweens.Add(Tween.LocalPosition(ikTarget, Vector3.zero, pickAnimationDuration));

            await UniTask.Delay(TimeSpan.FromSeconds(pickAnimationDuration), cancellationToken: token);

            //Step 2 : Place hand above the drop point

            item.MakeKinematic(true);
            item.OnPickUp(_cart.DropPoint);
            ik.Target = other.transform;

            //Tween.LocalPosition(other.transform.parent, Vector3.zero, placeAnimationDuration);

            Vector3 pointA = other.transform.localPosition;
            Vector3 pointB = other.transform.localPosition / 2 + animationMiddlePoint;
            Vector3 pointC = Vector3.zero;
            _tweens.Add(Tween.Custom(0f, 1f, placeAnimationDuration, ease: Ease.Linear, onValueChange: (t) =>
            {
                // Quadratic Bezier formula: B(t) = (1−t)²*A + 2*(1−t)*t*B + t²*C
                Vector3 pos = Mathf.Pow(1 - t, 2) * pointA +
                              2 * (1 - t) * t * pointB +
                              Mathf.Pow(t, 2) * pointC;
                other.transform.localPosition = pos;
            }));

            await UniTask.Delay(TimeSpan.FromSeconds(placeAnimationDuration), cancellationToken: token);

            item.MakeKinematic(false);

            //Step 3: Return hand to the body

            ikTarget.parent = _baseATargetParent;
            ikTarget.position = other.transform.parent.position;
            ik.Target = ikTarget;

            _tweens.Add(Tween.LocalPosition(ikTarget, _initialPosition, swingBackAnimationDuration));
            await UniTask.Delay(TimeSpan.FromSeconds(swingBackAnimationDuration), cancellationToken: token);

            ik.enabled = false;
            _animationInProgress = false;
            _tweens.Clear();

            item.SetPickedUpStatus(false);
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