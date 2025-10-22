using System;
using System.Collections.Generic;
using Eco.Scripts.Pooling;
using Eco.Scripts.Trash;
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

        private readonly List<MapField> _tiles = new List<MapField>();
        private readonly List<Vector2Int> _visibleIds = new();
        private Vector2Int _centerPoint;
        private ObjectPool<MapField> _pool;
        private WorldController _worldController;
        private SaveManager _saveManager;
        private bool _initialized;
        private Player _player;
        private IDisposable _subscription;

        private const int WorldToCanvasCoef = 100;

        public bool PlayerFollowingEnabled { get; set; }
        
        public void Initialize(WorldController worldController, SaveManager saveManager, Player player)
        {
            _worldController = worldController;
            _saveManager = saveManager;
            _player = player;
            _pool = new ObjectPool<MapField>(field, viewportSize * viewportSize, tileParent);

            var builder = new DisposableBuilder();
            TrashItem.OnItemRecycled.Subscribe(OnTrashRecycled).AddTo(ref builder);
            worldController.TreePlanter.OnTreePlanted.Subscribe(OnTreePlanted).AddTo(ref builder);
            Stats.OnUnlocked.Subscribe(OnMapUnlocked).AddTo(ref builder);
            _subscription = builder.Build();

            var mapSize = (_worldController.WorldSize * 2 + viewportSize) * WorldToCanvasCoef;
            content.sizeDelta = new Vector2(mapSize, mapSize);
            
            EnableMap(Stats.IsUpgradeUnlocked(UnlockableUpgradeType.Map));
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

        private void EnableMap(bool enable)
        {
            map.SetActive(enable);
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
        }

        void Update()
        {
            if (!_initialized)
            {
                return;
            }

            UpdatePlayerPosition();
            MoveTiles();
        }

        private void UpdatePlayerPosition()
        {
            Vector2 playerPos = new Vector2(-_player.transform.position.x, -_player.transform.position.z) * 10;
            playerMarker.anchoredPosition = -playerPos;
            playerMarker.rotation = Quaternion.Euler(0, 0, -_player.transform.eulerAngles.y);
            
            if(PlayerFollowingEnabled)
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

            //remove and hide tiles that became not visible
            for (var i = _tiles.Count - 1; i >= 0; i--)
            {
                var t = _tiles[i];

                _pool.ReturnToPool(t);
                _tiles.RemoveAt(i);
            }

            //show new tiles
            foreach (var id in _visibleIds)
            {
                if (!_saveManager.FieldTiles.TryGetValue(id, out var data))
                {
                    continue;
                }

                var t = _pool.Get();
                t.Rect.anchoredPosition = id * WorldToCanvasCoef;
                t.Initialize(WorldController.ChunkSize, iconParent);

                t.SetTileData(id, data);
                _tiles.Add(t);
            }
        }

        private Vector2Int GetMapPosition()
        {
            return new Vector2Int((int)(content.anchoredPosition.x / WorldToCanvasCoef), (int)(
                content.anchoredPosition.y / WorldToCanvasCoef));
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

        private void OnDestroy()
        {
            _subscription.Dispose();
        }
    }
}