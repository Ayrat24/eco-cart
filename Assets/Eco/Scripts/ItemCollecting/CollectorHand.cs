using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Jelly;
using UnityEngine;
using Random = UnityEngine.Random;

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

            // Timing and carry come from the body's hand settings; each pickup varies a little
            var hands = _body.Hands;
            float pace = 1f + Random.Range(-hands.TimingVariation, hands.TimingVariation);
            float carryArcHeight = hands.CarryArcHeight;

            bool grabbed = false;
            bool mouthOpen = false;
            var mouthSize = Vector2.zero;
            bool completed = false;

            // Step 1: grow out of the body and stretch to the item
            bool reached = await AnimateAsync(hands.ReachDuration * pace, t =>
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

                mouthSize = _stomach.GetSwallowedSize(item);
                _body.OpenMouth(mouthSize);
                mouthOpen = true;

                var grabPoint = tentacle.TipPosition;
                var heldOffset = itemTransform.localPosition;
                float startScale = itemTransform.localScale.x;
                float endScale = _stomach.GetSwallowedScale(item);

                float approach = hands.MouthApproachDistance * _body.Growth + mouthSize.x * 0.5f;

                bool carried = await AnimateAsync(hands.CarryDuration * pace, t =>
                {
                    if (!IsAlive(itemTransform))
                    {
                        return false;
                    }

                    // Lift the item off the ground, then swing round to line it up in front of the mouth so it
                    // arrives head-on rather than along the body
                    float e = EaseInOutSine(t);
                    var normal = _body.MouthNormal;
                    var front = _body.MouthLips + normal * approach;
                    var lift = grabPoint + Vector3.up * carryArcHeight;
                    var lineUp = front + normal * (approach * 0.8f) + Vector3.up * (carryArcHeight * 0.5f);
                    tentacle.TipPosition = CubicBezier(grabPoint, lift, lineUp, front, e);
                    tentacle.TipDirection = Vector3.Lerp(Vector3.down, -normal, e);
                    itemTransform.localPosition = Vector3.Lerp(heldOffset, Vector3.zero, e);
                    itemTransform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, e);
                    return true;
                }, token);

                // Step 3: the arm holds still in front of the mouth while its hand stretches in with the item
                if (carried)
                {
                    completed = await AnimateAsync(hands.PushDuration * pace, t =>
                    {
                        if (!IsAlive(itemTransform))
                        {
                            return false;
                        }

                        var normal = _body.MouthNormal;
                        var front = _body.MouthLips + normal * approach;
                        var inside = _body.MouthPosition - normal * (0.25f * _body.WorldScale);
                        tentacle.TipPosition = front;
                        tentacle.TipDirection = -normal;
                        tentacle.ReachPosition = Vector3.Lerp(front, inside, EaseInCubic(t));
                        tentacle.ReachWeight = 1f;
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
                _body.CloseMouth(mouthSize);
            }

            // Step 4: the hand snaps back to the arm, then the arm pulls back into the body
            var retractFrom = tentacle.TipPosition;
            var reachFrom = tentacle.ReachPosition;
            float reachWeight = tentacle.ReachWeight;
            await AnimateAsync(hands.RetractDuration * pace, t =>
            {
                float hand = Mathf.Clamp01(t / 0.3f);
                float arm = Mathf.Clamp01((t - 0.3f) / 0.7f);
                tentacle.ReachPosition = Vector3.Lerp(reachFrom, tentacle.TipPosition, hand);
                tentacle.ReachWeight = reachWeight * (1f - hand);
                tentacle.TipPosition = Vector3.Lerp(retractFrom, tentacle.SurfacePosition, EaseInCubic(arm));
                tentacle.TipDirection = Vector3.zero;
                tentacle.Visibility = 1f - Mathf.SmoothStep(0f, 1f, (arm - 0.5f) * 2f);
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

        private static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
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
