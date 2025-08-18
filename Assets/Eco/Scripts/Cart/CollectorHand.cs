using System;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Utils;
using PrimeTween;
using UnityEngine;

namespace Eco.Scripts.Cart
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
        private global::Eco.Scripts.Cart.Cart _cart;

        public Vector3 Position => ikTarget.TransformPoint(_initialPosition);
        public bool IsFree => !_animationInProgress;

        private void Start()
        {
            _initialPosition = ikTarget.localPosition;
            _baseATargetParent = ikTarget.parent;
        }

        public void Init(global::Eco.Scripts.Cart.Cart cart)
        {
            _cart = cart;
        }

        public void PlayAnimation(ICartItem item, Collider other)
        {
            PlayPickUpAnimationAsync(item, other).Forget();
        }

        private async UniTask PlayPickUpAnimationAsync(ICartItem item, Collider other)
        {
            _animationInProgress = true;

            //Step 1: Place hand on the item
            ik.Target = ikTarget;
            ikTarget.parent = other.transform;
            ik.enabled = true;

            Tween.LocalPosition(ikTarget, Vector3.zero, pickAnimationDuration);

            await UniTask.Delay(TimeSpan.FromSeconds(pickAnimationDuration));

            //Step 2 : Place hand above the drop point

            item.MakeKinematic(true);
            item.OnPickUp(_cart.dropPoint);
            ik.Target = other.transform;

            //Tween.LocalPosition(other.transform.parent, Vector3.zero, placeAnimationDuration);

            Vector3 pointA = other.transform.localPosition;
            Vector3 pointB = other.transform.localPosition / 2 + animationMiddlePoint;
            Vector3 pointC = Vector3.zero;
            Tween.Custom(0f, 1f, placeAnimationDuration, ease: Ease.Linear, onValueChange: (t) =>
            {
                // Quadratic Bezier formula: B(t) = (1−t)²*A + 2*(1−t)*t*B + t²*C
                Vector3 pos = Mathf.Pow(1 - t, 2) * pointA +
                              2 * (1 - t) * t * pointB +
                              Mathf.Pow(t, 2) * pointC;
                other.transform.localPosition = pos;
            });

            await UniTask.Delay(TimeSpan.FromSeconds(placeAnimationDuration));

            _cart.PickUpItem(item, other);
            item.SetInCartState(true);
            item.MakeKinematic(false);

            //Step 3: Return hand to the body

            ikTarget.parent = _baseATargetParent;
            ikTarget.position = other.transform.parent.position;
            ik.Target = ikTarget;

            Tween.LocalPosition(ikTarget, _initialPosition, swingBackAnimationDuration);
            await UniTask.Delay(TimeSpan.FromSeconds(swingBackAnimationDuration));

            ik.enabled = false;
            _animationInProgress = false;
        }
    }
}