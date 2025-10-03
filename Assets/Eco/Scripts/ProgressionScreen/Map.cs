using System.Collections.Generic;
using Eco.Scripts.Pooling;
using Eco.Scripts.Trash;
using Eco.Scripts.World;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Eco.Scripts.ProgressionScreen
{
    public class Map : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollView;
        [SerializeField] private RectTransform content;
        [SerializeField] Transform tileParent;
        [SerializeField] Transform zoomObject;
        [SerializeField] private MapField field;
        [SerializeField] private int cellSize;
        [SerializeField] private int viewportSize;
        [SerializeField] private float zoomScale;
        [SerializeField] Vector2 zoomRange;

        [SerializeField] private RectTransform playerMarker;

        private readonly List<MapField> _tiles = new List<MapField>();
        private readonly List<Vector2Int> _visibleIds = new();
        private Vector2Int _centerPoint = Vector2Int.one;
        private ObjectPool<MapField> _pool;
        private WorldController _worldController;
        private SaveManager _saveManager;
        private bool _initialized;
        private InputSystem_Actions _inputActions;
        private float _currentZoom = 1;
        private Player _player;

        public void Initialize(WorldController worldController, SaveManager saveManager, Player player)
        {
            _worldController = worldController;
            _saveManager = saveManager;
            _player = player;
            _pool = new ObjectPool<MapField>(field, viewportSize * viewportSize, tileParent);

            _inputActions = new InputSystem_Actions();
            _inputActions.Map.Enable();
            _inputActions.Map.Zoom.performed += Zoom;

            TrashItem.OnItemRecycled.Subscribe(OnTrashRecycled);
            worldController.TreePlanter.OnTreePlanted.Subscribe(OnTreePlanted);
            
            _initialized = true;
        }

        private void OnTrashRecycled(Tile tile)
        {
            _worldController.SaveWorld();
            UpdateVisibleFields();
        }

        private void OnTreePlanted(int id)
        {
            _worldController.SaveWorld();
            UpdateVisibleFields();
            Debug.LogError("here");
        }
        
        private void Zoom(InputAction.CallbackContext context)
        {
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

        // Update is called once per frame
        void Update()
        {
            if (!_initialized)
            {
                return;
            }

            Vector2 playerPos = new Vector2(-_player.transform.position.x, -_player.transform.position.z) * 10;
            playerMarker.anchoredPosition = -playerPos;
            playerMarker.rotation = Quaternion.Euler(0, 0, -_player.transform.eulerAngles.y);
            content.anchoredPosition = playerPos;

            MoveTiles();
        }

        public void ResetView()
        {
            scrollView.StopMovement();
            content.anchoredPosition = Vector2.zero;
        }

        private void MoveTiles()
        {
            Vector2Int center = new Vector2Int((int)(content.anchoredPosition.x / 100), (int)(
                content.anchoredPosition.y / 100));

            if (center == _centerPoint)
            {
                return;
            }

            _centerPoint = center;

            _visibleIds.Clear();
            for (int x = -viewportSize; x < viewportSize; x++)
            {
                for (int y = -viewportSize; y < viewportSize; y++)
                {
                    var id = new Vector2Int(x, y) - center;

                    _visibleIds.Add(id);
                }
            }

            for (var i = _tiles.Count - 1; i >= 0; i--)
            {
                var t = _tiles[i];
                if (_visibleIds.Contains(t.Id))
                {
                    _visibleIds.Remove(t.Id);
                    continue;
                }

                _pool.ReturnToPool(t);
                _tiles.RemoveAt(i);
            }

            foreach (var id in _visibleIds)
            {
                var t = _pool.Get();
                t.Rect.anchoredPosition = id * 100;
                t.Initialize(_worldController.ChunkSize);

                if (_saveManager.FieldTiles.TryGetValue(id, out var data))
                {
                    t.SetTileData(id, data);
                }
                else
                {
                    t.SetAsUndiscovered();
                }

                _tiles.Add(t);
            }
        }

        private void UpdateVisibleFields()
        {
            foreach (var t in _tiles)
            {
                var id = t.Id;
                if (_saveManager.FieldTiles.TryGetValue(id, out var data))
                {
                    t.SetTileData(id, data);
                }
                else
                {
                    t.SetAsUndiscovered();
                }
            }
        }
    }
}