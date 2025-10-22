using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Eco.Scripts.ProgressionScreen
{
    public class MapInputHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] Transform zoomObject;
        [SerializeField] private float zoomScale;
        [SerializeField] Vector2 zoomRange;
        [SerializeField] private ScrollRect scroll;

        [SerializeField] private GameObject lockedImage;
        [SerializeField] private GameObject unlockedImage;

        private InputSystem_Actions _inputActions;
        private float _currentZoom = 1;
        private bool _hasFocus;
        private Map _map;
        private bool _playerTrackingEnabled;
        
        public void Init(Map map)
        {
            _map = map;
            _inputActions = new InputSystem_Actions();
            _inputActions.Map.Enable();
            _inputActions.Map.Zoom.performed += Zoom;
            
            TogglePlayerTracking();
        }
        
        private void Zoom(InputAction.CallbackContext context)
        {
            if (!_hasFocus)
            {
                return;
            }
            
            Vector2 scrollValue = context.ReadValue<Vector2>();

            if (scrollValue.y > 0)
            {
                _currentZoom += zoomScale;
            }
            else if (scrollValue.y < 0)
            {
                _currentZoom -= zoomScale;
            }

            _currentZoom = Mathf.Clamp(_currentZoom, zoomRange.x, zoomRange.y);
            zoomObject.localScale = new Vector3(_currentZoom, _currentZoom, 1f);
        }

        public void TogglePlayerTracking()
        {
            _playerTrackingEnabled = !_playerTrackingEnabled;
            _map.PlayerFollowingEnabled = _playerTrackingEnabled;
            
            scroll.enabled = !_playerTrackingEnabled;
            lockedImage.SetActive(_playerTrackingEnabled);
            unlockedImage.SetActive(!_playerTrackingEnabled);
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            _hasFocus = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hasFocus = false;
        }
    }
}
