using System;
using Eco.Scripts.UI;
using Eco.Scripts.Utils;
using PrimeTween;
using R3;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Eco.Scripts
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] CinemachineFollow cinemachineFollow;
        [SerializeField] private CameraZoomSettings zoomSettings;

        // A fast flick (or a trackpad burst) counts as at most this many wheel notches per frame
        private const float MaxNotchesPerFrame = 3f;
        private const float MenuShiftDuration = 0.4f;

        private InputSystem_Actions _inputActions;
        private CinemachineCamera _camera;
        private IDisposable _subscription;
        // Fraction of the screen width the upgrade menu covers from the left; the jelly stays centered in the rest
        private float _screenCover;
        private Tween _coverTween;
        private Transform _cameraTransform;
        // Follow offset at the default zoom, as authored on the scene's CinemachineFollow
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
            _camera = cinemachineFollow.GetComponent<CinemachineCamera>();
            _baseOffset = cinemachineFollow.FollowOffset;
            _authoredEuler = _cameraTransform.eulerAngles;
            _inputActions = new InputSystem_Actions();
            _subscription = UpgradeTreeMenu.OnScreenCoverChanged.Subscribe(OnScreenCoverChanged);
        }

        private void OnScreenCoverChanged(float cover)
        {
            _coverTween.Stop();
            _coverTween = Tween.Custom(_screenCover, cover, MenuShiftDuration, value => _screenCover = value,
                Ease.OutCubic);
        }

        private void OnEnable()
        {
            _inputActions.Player.Zoom.Enable();
        }

        private void OnDisable()
        {
            _inputActions.Player.Zoom.Disable();
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

            cinemachineFollow.FollowOffset = new Vector3(_baseOffset.x * scale + MenuShift(distance),
                Mathf.Sin(radians) * distance, -Mathf.Cos(radians) * distance);

            // The scene's pitch doesn't aim exactly at the player; that look fades out as the camera tilts, until it
            // aims a little above the player for a third-person view
            float pitch = elevation + (_authoredEuler.x - baseElevation) * (1f - tilt)
                          - zoomSettings.ZoomedInLookUp * tilt;
            _cameraTransform.rotation = Quaternion.Euler(pitch, _authoredEuler.y, _authoredEuler.z);
        }

        // Moving the camera left by this puts the jelly in the middle of the screen part the menu leaves free
        private float MenuShift(float distance)
        {
            if (_screenCover <= 0f || _camera == null)
            {
                return 0f;
            }

            float aspect = Camera.main != null ? Camera.main.aspect : 16f / 9f;
            float halfWidth = distance * Mathf.Tan(_camera.Lens.FieldOfView * 0.5f * Mathf.Deg2Rad) * aspect;
            return -_screenCover * halfWidth;
        }

        private void OnDestroy()
        {
            _coverTween.Stop();
            _subscription?.Dispose();
            _inputActions.Dispose();
        }
    }
}
