using System;
using System.Collections.Generic;
using Eco.Scripts.Pooling;
using Eco.Scripts.Upgrades;
using Eco.Scripts.World;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Eco.Scripts.ProgressionScreen
{
    public class Map : MonoBehaviour
    {
        [SerializeField] private GameObject map;

        [SerializeField] private ScrollRect scrollView;
        [SerializeField] private RectTransform content;
        [SerializeField] Transform tileParent;
        [SerializeField] private MapField field;
        [SerializeField] private int cellSize;
        [SerializeField] private int viewportSize;
        [SerializeField] MapInputHandler inputHandler;
       
        [SerializeField] private Transform iconParent;
        [SerializeField] private RectTransform playerMarker;

        private readonly Dictionary<Vector2Int, MapField> _tiles = new();
        private readonly HashSet<Vector2Int> _visibleIds = new();
        private readonly List<Vector2Int> _idsToHide = new();
        private readonly HashSet<Vector2Int> _changedIds = new();
        private Vector2Int _centerPoint;
        private ObjectPool<MapField> _pool;
        private WorldController _worldController;
        private SaveManager _saveManager;
        private bool _initialized;
        private Player _player;
        private IDisposable _subscription;
        private Vector3 _lastPlayerPosition = new(float.NaN, 0, 0);
        private float _lastPlayerYaw = float.NaN;

        private const int WorldToCanvasCoef = 100;

        public bool PlayerFollowingEnabled { get; set; }
        
        public void Initialize(WorldController worldController, SaveManager saveManager, Player player)
        {
            _worldController = worldController;
            _saveManager = saveManager;
            _player = player;
            _pool = new ObjectPool<MapField>(field, viewportSize * viewportSize, tileParent);

            var builder = new DisposableBuilder();
            // Every tile change (recycling, planting, piles, chunk saves) ends in a chunk save
            saveManager.OnChunkUpdated.Subscribe(id => _changedIds.Add(id)).AddTo(ref builder);
            UnlockTracker.OnUnlocked.Subscribe(OnMapUnlocked).AddTo(ref builder);
            UnlockTracker.OnLocked.Subscribe(OnMapLocked).AddTo(ref builder);
            _subscription = builder.Build();

            var mapSize = (_worldController.WorldSideSize * 2 + viewportSize) * WorldToCanvasCoef;
            content.sizeDelta = new Vector2(mapSize, mapSize);
            
            EnableMap(UnlockTracker.IsUpgradeUnlocked(UnlockableUpgradeType.Map));
            MoveTiles(true);

            inputHandler.Init(this);
            _initialized = true;
        }

        private void OnMapUnlocked(UnlockableUpgradeType upgradeType)
        {
            if (upgradeType == UnlockableUpgradeType.Map)
            {
                map.SetActive(true);
            }
        }

        private void OnMapLocked(UnlockableUpgradeType upgradeType)
        {
            if (upgradeType == UnlockableUpgradeType.Map)
            {
                map.SetActive(false);
            }
        }

        private void EnableMap(bool enable)
        {
            map.SetActive(enable);
        }

        void Update()
        {
            if (!_initialized)
            {
                return;
            }

            UpdatePlayerPosition();
            MoveTiles();
            RedrawChangedFields();
        }

        private void UpdatePlayerPosition()
        {
            // Setting anchoredPosition dirties the canvas, so only write what changed
            var playerTransform = _player.transform;
            var position = playerTransform.position;
            var yaw = playerTransform.eulerAngles.y;
            Vector2 playerPos = new Vector2(-position.x, -position.z) * 10;

            if (position != _lastPlayerPosition || yaw != _lastPlayerYaw)
            {
                _lastPlayerPosition = position;
                _lastPlayerYaw = yaw;
                playerMarker.anchoredPosition = -playerPos;
                playerMarker.rotation = Quaternion.Euler(0, 0, -yaw);
            }

            if (PlayerFollowingEnabled && content.anchoredPosition != playerPos)
            {
                content.anchoredPosition = playerPos;
            }
        }

        public void ResetView()
        {
            scrollView.StopMovement();
            content.anchoredPosition = Vector2.zero;
        }

        private void MoveTiles(bool force = false)
        {
            Vector2Int center = GetMapPosition();

            if (!force && center == _centerPoint)
            {
                return;
            }

            _centerPoint = center;

            _visibleIds.Clear();
            //find all visible tiles
            for (int x = -viewportSize; x < viewportSize; x++)
            {
                for (int y = -viewportSize; y < viewportSize; y++)
                {
                    var id = new Vector2Int(x, y) - center;

                    _visibleIds.Add(id);
                }
            }

            // Only fields that scrolled in or out change; the rest keep their texture
            _idsToHide.Clear();
            foreach (var id in _tiles.Keys)
            {
                if (!_visibleIds.Contains(id))
                {
                    _idsToHide.Add(id);
                }
            }

            foreach (var id in _idsToHide)
            {
                _pool.ReturnToPool(_tiles[id]);
                _tiles.Remove(id);
            }

            foreach (var id in _visibleIds)
            {
                if (!_tiles.ContainsKey(id))
                {
                    ShowField(id);
                }
            }
        }

        private void ShowField(Vector2Int id)
        {
            if (!_saveManager.FieldTiles.TryGetValue(id, out var data))
            {
                return;
            }

            var t = _pool.Get();
            t.Rect.anchoredPosition = id * WorldToCanvasCoef;
            t.Initialize(WorldController.ChunkSize, iconParent);

            t.SetTileData(id, data);
            _tiles[id] = t;
        }

        private Vector2Int GetMapPosition()
        {
            return new Vector2Int((int)(content.anchoredPosition.x / WorldToCanvasCoef), (int)(
                content.anchoredPosition.y / WorldToCanvasCoef));
        }

        // Redraws only the chunks saved since the last frame, at most once each
        private void RedrawChangedFields()
        {
            if (_changedIds.Count == 0)
            {
                return;
            }

            foreach (var id in _changedIds)
            {
                if (_tiles.TryGetValue(id, out var t))
                {
                    t.SetTileData(id, _saveManager.FieldTiles[id]);
                }
                else if (_visibleIds.Contains(id))
                {
                    ShowField(id);
                }
            }

            _changedIds.Clear();
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
        }
    }
}