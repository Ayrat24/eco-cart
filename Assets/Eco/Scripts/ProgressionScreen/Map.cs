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
        private readonly List<Vector2Int> _inRange = new();
        private Vector2Int _centerPoint;
        private ObjectPool<MapField> _pool;
        private WorldController _worldController;
        private SaveManager _saveManager;
        private bool _initialized;
        private Player _player;
        private IDisposable _subscription;
        private Vector3 _lastPlayerPosition = new(float.NaN, 0, 0);
        private float _lastPlayerYaw = float.NaN;
        private HexTileLayout _layout;

        private const int CanvasUnitsPerMeter = 10;

        public bool PlayerFollowingEnabled { get; set; }
        
        public void Initialize(WorldController worldController, SaveManager saveManager, Player player)
        {
            _worldController = worldController;
            _saveManager = saveManager;
            _player = player;
            _layout = HexTileLayout.Get(worldController.HexRadius);
            _pool = new ObjectPool<MapField>(field, Hex.CountInRange(viewportSize), tileParent);

            var builder = new DisposableBuilder();
            // Every tile change (recycling, planting, piles, chunk saves) ends in a chunk save
            saveManager.OnChunkUpdated.Subscribe(id => _changedIds.Add(id)).AddTo(ref builder);
            UnlockTracker.OnUnlocked.Subscribe(OnMapUnlocked).AddTo(ref builder);
            UnlockTracker.OnLocked.Subscribe(OnMapLocked).AddTo(ref builder);
            _subscription = builder.Build();

            var mapSize = (_worldController.WorldData.MapRadius + viewportSize) * 2 *
                          Hex.Spacing(_worldController.HexRadius) * CanvasUnitsPerMeter;
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
            Vector2 playerPos = new Vector2(-position.x, -position.z) * CanvasUnitsPerMeter;

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
            //find all visible chunks
            Hex.Range(center, viewportSize, _inRange);
            foreach (var id in _inRange)
            {
                _visibleIds.Add(id);
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
            if (!_saveManager.Chunks.TryGetValue(id, out var data) || data.tiles?.Length != _layout.Count)
            {
                return;
            }

            var t = _pool.Get();
            t.Rect.anchoredPosition = Hex.ToPlane(id, _worldController.HexRadius) * CanvasUnitsPerMeter;
            t.Initialize(_layout, CanvasUnitsPerMeter, iconParent);

            t.SetTileData(id, data.tiles);
            _tiles[id] = t;
        }

        // The hex at the middle of the view; the content moves opposite to the world point it shows
        private Vector2Int GetMapPosition()
        {
            return Hex.FromPlane(-content.anchoredPosition / CanvasUnitsPerMeter, _worldController.HexRadius);
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
                    var tiles = _saveManager.Chunks[id].tiles;
                    if (tiles.Length == _layout.Count)
                    {
                        t.SetTileData(id, tiles);
                    }
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