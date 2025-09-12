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

        private Cart _cart;
        private Tween _xTween;
        private Tween _yTween;
        private Tween _zTween;

        public void Init(Player player)
        {
            _player = player;

            var builder = new DisposableBuilder();
            
            _player.OnCartChanged.Subscribe(OnCartChanged).AddTo(ref builder);
            UpgradeMenu.OnOpen.Subscribe(OnMenuOpened).AddTo(ref builder);

            _subscription = builder.Build();
        }

        private void OnCartChanged(Cart cart)
        {
            _cart = cart;

            var xStart = cinemachineFollow.FollowOffset.x;
            var xEnd = cart.CartData.cameraOffset.x;
            _xTween = Tween.Custom(xStart, xEnd, 0.5f,
                f => cinemachineFollow.FollowOffset.x = f);

            var yStart = cinemachineFollow.FollowOffset.y;
            var yEnd = cart.CartData.cameraOffset.y;
            _yTween = Tween.Custom(yStart, yEnd, 0.5f,
                f => cinemachineFollow.FollowOffset.y = f);
            
            var zStart = cinemachineFollow.FollowOffset.z;
            var zEnd = cart.CartData.menuCameraOffset;
            _zTween = Tween.Custom(zStart, zEnd, 0.5f,
                f => cinemachineFollow.FollowOffset.z = f);
        }

        private void OnMenuOpened(bool isOpen)
        {
            if (_cart == null)
            {
                return;
            }

            if (isOpen)
            {
                var start = cinemachineFollow.FollowOffset.z;
                var offset = _cart.CartData.menuCameraOffset;
                Tween.Custom(start, offset, 0.4f,
                    f => cinemachineFollow.FollowOffset.z = f);
            }
            else
            {
                var start = _cart.CartData.menuCameraOffset;
                var offset = 0;
                Tween.Custom(start, offset, 0.4f,
                    f => cinemachineFollow.FollowOffset.z = f);
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