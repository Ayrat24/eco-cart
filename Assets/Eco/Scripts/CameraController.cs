using System;
using Eco.Scripts.ItemCollecting;
using Eco.Scripts.UI;
using PrimeTween;
using Unity.Cinemachine;
using UnityEngine;
using R3;

namespace Eco.Scripts
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] CinemachineFollow cinemachineFollow;

        private IDisposable _subscription;
        private Player _player;

        private Stomach _stomach;
        private Tween _xTween;
        private Tween _yTween;
        private Tween _zTween;
        private bool _menuOpened;
        
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

            var zStart = cinemachineFollow.FollowOffset.z;
            var zEnd = stomach.Stats.cameraOffset.z;
            _xTween = Tween.Custom(zStart, zEnd, 0.5f,
                f => cinemachineFollow.FollowOffset.z = f);

            var yStart = cinemachineFollow.FollowOffset.y;
            var yEnd = stomach.Stats.cameraOffset.y;
            _yTween = Tween.Custom(yStart, yEnd, 0.5f,
                f => cinemachineFollow.FollowOffset.y = f);

            if (!_menuOpened)
            {
                return;
            }
            
            var xStart = cinemachineFollow.FollowOffset.x;
            var xEnd = stomach.Stats.menuCameraOffset;
            _zTween = Tween.Custom(xStart, xEnd, 0.5f,
                f => cinemachineFollow.FollowOffset.x = f);
        }

        private void OnMenuOpened(bool isOpen)
        {
            _menuOpened = isOpen;
            
            if (_stomach == null)
            {
                return;
            }

            if (isOpen)
            {
                var start = cinemachineFollow.FollowOffset.x;
                var offset = _stomach.Stats.menuCameraOffset;
                Tween.Custom(start, offset, 0.4f,
                    f => cinemachineFollow.FollowOffset.x = f);
            }
            else
            {
                var start = _stomach.Stats.menuCameraOffset;
                var offset = 0;
                Tween.Custom(start, offset, 0.4f,
                    f => cinemachineFollow.FollowOffset.x = f);
            }
        }

        private void OnDestroy()
        {
            _xTween.Stop();
            _yTween.Stop();
            _zTween.Stop();
            _subscription?.Dispose();
        }
    }
}