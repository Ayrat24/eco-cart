using System;
using Eco.Scripts.ItemCollecting;
using Eco.Scripts.UI;
using Eco.Scripts.Utils;
using PrimeTween;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using R3;

namespace Eco.Scripts
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] CinemachineFollow cinemachineFollow;
        [SerializeField] private CameraZoomSettings zoomSettings;

        // A fast flick (or a trackpad burst) counts as at most this many wheel notches per frame
        private const float MaxNotchesPerFrame = 3f;

        private IDisposable _subscription;
        private Player _player;

        private Stomach _stomach;
        private Tween _xTween;
        private Tween _yTween;
        private Tween _zTween;
        private bool _menuOpened;

        private InputSystem_Actions _inputActions;
        private Transform _cameraTransform;
        // Follow offset at the default zoom: the cart's camera offset plus the sideways shift while the menu is open
        private Vector3 _baseOffset;
        // The scene camera's rotation, kept as is at the default zoom
        private Vector3 _authoredEuler;
        // Zoom as the log of the distance scale: 0 = default view, negative = zoomed in
        private float _logScale;
        private float _targetLogScale;
        private float _logScaleVelocity;

        private void Awake()
        {
            _cameraTransform = cinemachineFollow.transform;
            _baseOffset = cinemachineFollow.FollowOffset;
            _authoredEuler = _cameraTransform.eulerAngles;
            _inputActions = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            _inputActions.Player.Zoom.Enable();
        }

        private void OnDisable()
        {
            _inputActions.Player.Zoom.Disable();
        }

        public void Init(Player player)
        {
            _player = player;

            var builder = new DisposableBuilder();

            _player.OnStomachChanged.Subscribe(OnStomachChanged).AddTo(ref builder);
            UpgradeMenu.OnOpen.Subscribe(OnMenuOpened).AddTo(ref builder);

            _subscription = builder.Build();
        }

        private void OnStomachChanged(Stomach stomach)
        {
            _stomach = stomach;

            var zEnd = stomach.Stats.cameraOffset.z;
            _zTween.Stop();
            _zTween = Tween.Custom(_baseOffset.z, zEnd, 0.5f, f => _baseOffset.z = f);

            var yEnd = stomach.Stats.cameraOffset.y;
            _yTween.Stop();
            _yTween = Tween.Custom(_baseOffset.y, yEnd, 0.5f, f => _baseOffset.y = f);

            if (!_menuOpened)
            {
                return;
            }

            var xEnd = stomach.Stats.menuCameraOffset;
            _xTween.Stop();
            _xTween = Tween.Custom(_baseOffset.x, xEnd, 0.5f, f => _baseOffset.x = f);
        }

        private void OnMenuOpened(bool isOpen)
        {
            _menuOpened = isOpen;

            if (_stomach == null)
            {
                return;
            }

            var xEnd = isOpen ? _stomach.Stats.menuCameraOffset : 0f;
            _xTween.Stop();
            _xTween = Tween.Custom(_baseOffset.x, xEnd, 0.4f, f => _baseOffset.x = f);
        }

        private void Update()
        {
            ReadZoomInput();

            _logScale = Mathf.SmoothDamp(_logScale, _targetLogScale, ref _logScaleVelocity, zoomSettings.SmoothTime);
            ApplyZoom();
        }

        private void ReadZoomInput()
        {
            // One wheel notch reads as 1 (InputSettings.scrollDeltaBehavior is uniform across platforms)
            float notches = _inputActions.Player.Zoom.ReadValue<float>();
            // Scrolling over the upgrade menu, the map and other UI is theirs
            if (notches == 0f || FindUIBlockers.IsPointerOverUI(Mouse.current.position.ReadValue()))
            {
                return;
            }

            // Scrolling up zooms in
            notches = Mathf.Clamp(notches, -MaxNotchesPerFrame, MaxNotchesPerFrame);
            _targetLogScale = Mathf.Clamp(_targetLogScale + notches * Mathf.Log(1f - zoomSettings.ScrollStep),
                Mathf.Log(zoomSettings.MinDistanceScale), Mathf.Log(zoomSettings.MaxDistanceScale));
        }

        private void ApplyZoom()
        {
            float scale = Mathf.Exp(_logScale);
            float minLogScale = Mathf.Log(zoomSettings.MinDistanceScale);
            // 0 at the default view, 1 at full zoom-in; zooming out doesn't tilt
            float tilt = zoomSettings.EvaluateTilt(minLogScale < 0f ? _logScale / minLogScale : 0f);

            float baseDistance = new Vector2(_baseOffset.y, _baseOffset.z).magnitude;
            float baseElevation = Mathf.Atan2(_baseOffset.y, -_baseOffset.z) * Mathf.Rad2Deg;
            float elevation = Mathf.Lerp(baseElevation, zoomSettings.ZoomedInElevation, tilt);
            float distance = baseDistance * scale;
            float radians = elevation * Mathf.Deg2Rad;

            // The menu shift scales with the distance so the player moves the same amount on screen
            cinemachineFollow.FollowOffset = new Vector3(_baseOffset.x * scale,
                Mathf.Sin(radians) * distance, -Mathf.Cos(radians) * distance);

            // The scene's pitch doesn't aim exactly at the player; that look fades out as the camera tilts, until it
            // aims a little above the player for a third-person view
            float pitch = elevation + (_authoredEuler.x - baseElevation) * (1f - tilt)
                          - zoomSettings.ZoomedInLookUp * tilt;
            _cameraTransform.rotation = Quaternion.Euler(pitch, _authoredEuler.y, _authoredEuler.z);
        }

        private void OnDestroy()
        {
            _xTween.Stop();
            _yTween.Stop();
            _zTween.Stop();
            _subscription?.Dispose();
            _inputActions.Dispose();
        }
    }
}
