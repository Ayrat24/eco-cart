using System.Collections.Generic;
using Eco.Scripts.Pooling;
using Eco.Scripts.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Eco.Scripts.ProgressionScreen
{
    public class MapField : MonoBehaviour, IPooledObject
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private RawImage tileMap;

        [SerializeField] private Image treeIconPrefab;

        [SerializeField] private List<TileColor> tileColors;
        [SerializeField] private Color scrapColor;

        [SerializeField] private Sprite[] treeSprites;

        private Texture2D _texture;

        private Vector2Int _id;
        private bool _initialized;
        private Dictionary<int, Color> _tileColors = new();
        private ObjectPool<Image> _imagePool;
        private List<Image> _spawnedImages = new();
        private Color[] _colors;
        private bool _hasId;
        private HexTileLayout _layout;
        private float _unitsPerTile;
        private int _size;
        private int[] _pixelTiles;
        private Vector2 _center;

        public Vector2Int Id => _id;
        public RectTransform Rect => rectTransform;

        /// <param name="unitsPerTile">Canvas units per tile (one metre of the world).</param>
        public void Initialize(HexTileLayout layout, float unitsPerTile, Transform iconParent)
        {
            _layout = layout;
            _unitsPerTile = unitsPerTile;

            if (_initialized)
            {
                return;
            }

            // The player isn't meant to know the world is made of chunks
            text.gameObject.SetActive(false);
            _imagePool = new ObjectPool<Image>(treeIconPrefab, 0, iconParent);

            foreach (var tc in tileColors)
            {
                _tileColors[(int)tc.groundType] = tc.color;
            }

            // A world-aligned square of 1 m pixels around the hex
            _size = Mathf.CeilToInt(layout.Radius * 2f) + 1;
            _texture = new Texture2D(_size, _size);
            _texture.filterMode = FilterMode.Point;
            _texture.wrapMode = TextureWrapMode.Clamp;
            _colors = new Color[_size * _size];
            _pixelTiles = new int[_size * _size];
            tileMap.texture = _texture;
            _initialized = true;
        }

        public void SetTileData(Vector2Int id, TileData[] data)
        {
            if (!_hasId || _id != id)
            {
                MapPixels(id);
            }

            _id = id;
            _hasId = true;
            ReturnTreeIcons();

            var colors = _colors;
            for (int pixel = 0; pixel < colors.Length; pixel++)
            {
                int tile = _pixelTiles[pixel];
                if (tile < 0)
                {
                    colors[pixel] = Color.clear;
                    continue;
                }

                var tileData = data[tile];
                colors[pixel] = tileData.objectType == (int)TileObjectType.Scrap
                    ? scrapColor
                    : _tileColors.GetValueOrDefault(tileData.ground, Color.magenta);
            }

            for (int index = 0; index < data.Length; index++)
            {
                var tileData = data[index];
                if (tileData.objectType != (int)TileObjectType.Tree)
                {
                    continue;
                }

                var treeIconId = tileData.objectId - 100;
                if (treeIconId < 0 || treeSprites.Length <= treeIconId)
                {
                    treeIconId = 0;
                }

                var icon = _imagePool.Get();
                icon.rectTransform.anchoredPosition = (_center + _layout.LocalPosition(index)) * _unitsPerTile;
                icon.sprite = treeSprites[treeIconId];

                _spawnedImages.Add(icon);
            }

            _texture.SetPixels(colors);
            _texture.Apply();
        }

        // Pixels are world-aligned 1 m cells, each drawn by the one hex its centre is in, so neighbouring fields
        // fit together without seams and the map shows no hex outlines. Each pixel shows its nearest tile.
        private void MapPixels(Vector2Int id)
        {
            _center = Hex.ToPlane(id, _layout.Radius);
            int half = _size / 2;
            var origin = new Vector2(Mathf.FloorToInt(_center.x) - half, Mathf.FloorToInt(_center.y) - half);
            rectTransform.anchoredPosition = (origin + Vector2.one * (_size / 2f)) * _unitsPerTile;
            rectTransform.sizeDelta = Vector2.one * _size * _unitsPerTile;

            for (int z = 0; z < _size; z++)
            {
                for (int x = 0; x < _size; x++)
                {
                    var cellCenter = origin + new Vector2(x + 0.5f, z + 0.5f);
                    _pixelTiles[z * _size + x] = Hex.FromPlane(cellCenter, _layout.Radius) == id
                        ? NearestTile(cellCenter - _center)
                        : -1;
                }
            }
        }

        private int NearestTile(Vector2 local)
        {
            int index = _layout.IndexAt(local);
            if (index >= 0)
            {
                return index;
            }

            // Cells along the hex edge have no tile of their own
            float best = float.MaxValue;
            for (int i = 0; i < _layout.Count; i++)
            {
                float distance = (_layout.LocalPosition(i) - local).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    index = i;
                }
            }

            return index;
        }

        public void SetAsUndiscovered()
        {
        }

        [System.Serializable]
        struct TileColor
        {
            public TileGroundType groundType;
            public Color color;
        }

        public void OnSpawn()
        {
            
        }

        public void OnDespawn()
        {
            ReturnTreeIcons();
        }

        // SetTileData runs again whenever the chunk saves; without this each redraw stacked another icon per tree.
        private void ReturnTreeIcons()
        {
            foreach (var i in _spawnedImages)
            {
                _imagePool.ReturnToPool(i);
            }

            _spawnedImages.Clear();
        }
    }
}