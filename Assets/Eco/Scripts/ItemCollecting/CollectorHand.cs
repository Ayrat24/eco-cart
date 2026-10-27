using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Jelly;
using UnityEngine;

namespace Eco.Scripts.ItemCollecting
{
    /// <summary>
    /// One of the jelly's grabbing tentacles. For each pickup it grows out of whichever part of the hand zones
    /// faces the item, stretches to it, carries it to the mouth, squeezes it in and pulls back into the body.
    /// </summary>
    public class CollectorHand : MonoBehaviour
    {
        [SerializeField] string handName;
        [SerializeField] private JellyTentacle tentacle;

        [SerializeField] private float pickAnimationDuration = 0.3f;
        [SerializeField] private float placeAnimationDuration = 0.45f;
        [SerializeField] private float swallowAnimationDuration = 0.12f;
        [SerializeField] private float swingBackAnimationDuration = 0.25f;
        [Tooltip("How high the tentacle lifts an item on its way to the mouth, in metres.")]
        [SerializeField] private float carryArcHeight = 1.2f;

        private Stomach _stomach;
        private JellyBody _body;
        private bool _animationInProgress;
        private CancellationTokenSource _cancellationTokenSource;

        public bool IsFree => !_animationInProgress;

        public void Init(Stomach stomach)
        {
            _stomach = stomach;
            _body = stomach.Body;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void PlayAnimation(ICartItem item, Collider other)
        {
            var itemTransform = item is Component component ? component.transform : other.transform;
            if (!_body.TryTakeAnchor(itemTransform.position, out var anchor))
            {
                RollbackItem(item);
                return;
            }

            PlayPickUpAnimationAsync(item, itemTransform, anchor, _cancellationTokenSource.Token).Forget();
        }

        private async UniTask PlayPickUpAnimationAsync(ICartItem item, Transform itemTransform, JellyAnchor anchor,
            CancellationToken token)
        {
            _animationInProgress = true;
            tentacle.Show(_body, anchor);

            bool grabbed = false;
            bool mouthOpen = false;
            bool completed = false;

            // Step 1: grow out of the body and stretch to the item
            bool reached = await AnimateAsync(pickAnimationDuration, t =>
            {
                if (!IsAlive(itemTransform))
                {
                    return false;
                }

                var from = tentacle.SurfacePosition;
                var target = itemTransform.position;
                tentacle.TipPosition = Vector3.Lerp(from, target, EaseOutCubic(t)) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.4f);
                tentacle.TipDirection = Vector3.Lerp(target - from, Vector3.down, t);
                tentacle.Visibility = Mathf.Clamp01(t * 3f);
                return true;
            }, token);

            if (reached)
            {
                // Step 2: grab it and carry it to the mouth, squeezing it down to its swallowed size
                grabbed = true;
                item.MakeKinematic(true);
                itemTransform.SetParent(tentacle.Tip, true);
                _body.Poke(tentacle.SurfacePosition, 0.03f);

                _body.OpenMouth();
                mouthOpen = true;

                var grabPoint = tentacle.TipPosition;
                var heldOffset = itemTransform.localPosition;
                float startScale = itemTransform.localScale.x;
                float endScale = _stomach.GetSwallowedScale(item);

                bool carried = await AnimateAsync(placeAnimationDuration, t =>
                {
                    if (!IsAlive(itemTransform))
                    {
                        return false;
                    }

                    float e = EaseInOutSine(t);
                    var lips = _body.MouthPosition + _body.MouthNormal * (0.35f * _body.WorldScale);
                    var control = (grabPoint + lips) * 0.5f + Vector3.up * carryArcHeight;
                    float u = 1f - e;
                    tentacle.TipPosition = u * u * grabPoint + 2f * u * e * control + e * e * lips;
                    tentacle.TipDirection = Vector3.Lerp(Vector3.down, -_body.MouthNormal, e);
                    itemTransform.localPosition = Vector3.Lerp(heldOffset, Vector3.zero, e);
                    itemTransform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, e);
                    return true;
                }, token);

                // Step 3: push it through the mouth
                if (carried)
                {
                    var lipsPoint = tentacle.TipPosition;
                    completed = await AnimateAsync(swallowAnimationDuration, t =>
                    {
                        if (!IsAlive(itemTransform))
                        {
                            return false;
                        }

                        var inside = _body.MouthPosition - _body.MouthNormal * (0.25f * _body.WorldScale);
                        tentacle.TipPosition = Vector3.Lerp(lipsPoint, inside, EaseInCubic(t));
                        return true;
                    }, token);
                }
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            if (completed)
            {
                _stomach.Swallow(item);
                item.SetPickedUpStatus(false);
            }
            else
            {
                if (grabbed && IsAlive(itemTransform))
                {
                    itemTransform.SetParent(null, true);
                    item.MakeKinematic(false);
                }

                RollbackItem(item);
            }

            if (mouthOpen)
            {
                _body.CloseMouth();
            }

            // Step 4: pull back into the body
            var retractFrom = tentacle.TipPosition;
            await AnimateAsync(swingBackAnimationDuration, t =>
            {
                tentacle.TipPosition = Vector3.Lerp(retractFrom, tentacle.SurfacePosition, EaseInCubic(t));
                tentacle.TipDirection = Vector3.zero;
                tentacle.Visibility = 1f - Mathf.SmoothStep(0f, 1f, (t - 0.5f) * 2f);
                return true;
            }, token);

            if (token.IsCancellationRequested)
            {
                return;
            }

            tentacle.Hide();
            _body.ReleaseAnchor(anchor);
            _animationInProgress = false;
        }

        private void RollbackItem(ICartItem item)
        {
            if (item == null)
            {
                return;
            }

            _stomach.Release(item);
            item.SetPickedUpStatus(false);
            item.SetInCartState(false);
        }

        private static bool IsAlive(Transform itemTransform)
        {
            return itemTransform != null && itemTransform.gameObject.activeInHierarchy;
        }

        // Runs step(t) once per frame, t from 0 to 1 over the duration, just before LateUpdate so the tentacle
        // rebuilds from this frame's tip. False if cancelled or step gave up.
        private static async UniTask<bool> AnimateAsync(float duration, Func<float, bool> step, CancellationToken token)
        {
            float time = 0f;
            while (true)
            {
                float t = duration > 0f ? Mathf.Clamp01(time / duration) : 1f;
                if (!step(t))
                {
                    return false;
                }

                if (t >= 1f)
                {
                    return true;
                }

                if (await UniTask.Yield(PlayerLoopTiming.PreLateUpdate, token).SuppressCancellationThrow())
                {
                    return false;
                }

                time += Time.deltaTime;
            }
        }

        private static float EaseOutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        private static float EaseInCubic(float t)
        {
            return t * t * t;
        }

        private static float EaseInOutSine(float t)
        {
            return 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);
        }

        public void Clear()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
}
