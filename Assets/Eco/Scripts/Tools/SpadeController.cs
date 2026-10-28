using System;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Jelly;
using Eco.Scripts.Scrap;
using PrimeTween;
using R3;
using UnityEngine;

namespace Eco.Scripts.Tools
{
    /// <summary>
    /// Spade tool. Two tentacles grow out to hold the spade in front of the jelly, and it stabs at the nearest
    /// scrap pile in reach once a second.
    /// </summary>
    [DefaultExecutionOrder(105)]
    public class SpadeController : Tool
    {
        [SerializeField] private JellyBody body;
        [SerializeField] private JellyTentacle leftTentacle;
        [SerializeField] private JellyTentacle rightTentacle;
        [SerializeField] private Transform leftHandPosition;
        [SerializeField] private Transform rightHandPosition;
        [Tooltip("Parent of the spade, kept in front of the body.")]
        [SerializeField] private Transform spadeMount;
        [SerializeField] private GameObject spade;
        [SerializeField, Range(0f, 1f)] private float mountHeight = 0.45f;
        [Tooltip("How far in front of the body surface the spade is held, in metres.")]
        [SerializeField] private float mountDistance = 0.45f;
        [SerializeField] private float gripDuration = 0.3f;
        [SerializeField] private float digDuration = 0.55f;
        [Tooltip("Spade movement at the bottom of a stab, in the mount's space.")]
        [SerializeField] private Vector3 digThrust = new(0f, -0.5f, 0.4f);
        [SerializeField] private float digTilt = 30f;

        [SerializeField] private SphereCollider sphereCollider;
        [SerializeField] LayerMask layerMask;

        private readonly Collider[] _colliders = new Collider[3];
        private IDisposable _subscription;
        private bool _isDigging;
        private bool _holding;
        private float _grip;
        private JellyAnchor _leftAnchor;
        private JellyAnchor _rightAnchor;
        private Vector3 _spadeRestPosition;
        private Quaternion _spadeRestRotation;
        private Tween _digTween;

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

            _spadeRestPosition = spade.transform.localPosition;
            _spadeRestRotation = spade.transform.localRotation;
        }

        private void LateUpdate()
        {
            if (!spade.activeSelf)
            {
                return;
            }

            body.GetSurfacePoseAt(0f, mountHeight, out var front, out _);
            var root = body.transform.parent;
            spadeMount.SetPositionAndRotation(front + root.forward * (mountDistance * body.Growth), root.rotation);

            _grip = Mathf.MoveTowards(_grip, _holding ? 1f : 0f, Time.deltaTime / gripDuration);
            UpdateTentacle(leftTentacle, leftHandPosition);
            UpdateTentacle(rightTentacle, rightHandPosition);
        }

        private void UpdateTentacle(JellyTentacle tentacle, Transform grip)
        {
            if (!tentacle.IsShown)
            {
                return;
            }

            float reach = 1f - (1f - _grip) * (1f - _grip);
            tentacle.TipPosition = Vector3.Lerp(tentacle.SurfacePosition, grip.position, reach);
            tentacle.Visibility = Mathf.Clamp01(_grip * 3f);
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

            var pile = shortestCollider.gameObject.GetComponent<ScrapPile>();
            if (pile == null)
            {
                return;
            }

            Dig(pile);
        }

        private void Dig(ScrapPile scrapPile)
        {
            if (!Active)
            {
                return;
            }

            scrapPile.Dig();
            PlayDigAnimationAsync().Forget();
        }

        private async UniTask PlayDigAnimationAsync()
        {
            if (_isDigging)
            {
                return;
            }

            _isDigging = true;
            body.Squash(-0.5f);

            var spadeTransform = spade.transform;
            _digTween = Tween.Custom(0f, 1f, digDuration, t =>
            {
                float stab = Mathf.Sin(t * Mathf.PI);
                spadeTransform.localPosition = _spadeRestPosition + digThrust * stab;
                spadeTransform.localRotation = Quaternion.Euler(digTilt * stab, 0f, 0f) * _spadeRestRotation;
            });

            await UniTask.WaitForSeconds(digDuration);
            spadeTransform.SetLocalPositionAndRotation(_spadeRestPosition, _spadeRestRotation);
            _isDigging = false;
        }

        private void SwitchGrip(bool hold)
        {
            if (hold == _holding)
            {
                return;
            }

            _holding = hold;
            if (!hold)
            {
                return;
            }

            ShowTentacle(leftTentacle, leftHandPosition, ref _leftAnchor);
            ShowTentacle(rightTentacle, rightHandPosition, ref _rightAnchor);
        }

        private void ShowTentacle(JellyTentacle tentacle, Transform grip, ref JellyAnchor anchor)
        {
            // Still out from last time: keep the anchor it already has
            if (tentacle.IsShown || !body.TryTakeAnchor(grip.position, out anchor))
            {
                return;
            }

            tentacle.Show(body, anchor);
        }

        private void HideTentacles()
        {
            if (leftTentacle.IsShown)
            {
                leftTentacle.Hide();
                body.ReleaseAnchor(_leftAnchor);
            }

            if (rightTentacle.IsShown)
            {
                rightTentacle.Hide();
                body.ReleaseAnchor(_rightAnchor);
            }
        }

        private void OnDestroy()
        {
            _digTween.Stop();
            _subscription?.Dispose();
        }

        public override async UniTask Enable()
        {
            Active = true;
            spade.SetActive(true);
            SwitchGrip(true);
            await UniTask.WaitForSeconds(gripDuration);
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
            SwitchGrip(false);
            await UniTask.WaitUntil(() => _grip <= 0f);
            HideTentacles();
            spade.SetActive(false);
        }
    }
}
