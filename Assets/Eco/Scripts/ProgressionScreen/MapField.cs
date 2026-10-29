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

        public Vector2Int Id => _id;
        public RectTransform Rect => rectTransform;

        /// <param name="unitsPerTile">Canvas units per tile (one metre of the world).</param>
        public void Initialize(HexTileLayout layout, float unitsPerTile, Transform iconParent)
        {
            _layout = layout;
            _unitsPerTile = unitsPerTile;
            rectTransform.sizeDelta = Vector2.one * layout.Size * unitsPerTile;

            if (_initialized)
            {
                return;
            }

            _imagePool = new ObjectPool<Image>(treeIconPrefab, 0, iconParent);

            foreach (var tc in tileColors)
            {
                _tileColors[(int)tc.groundType] = tc.color;
            }

            int size = layout.Size;
            _texture = new Texture2D(size, size);
            _texture.filterMode = FilterMode.Point;
            _texture.wrapMode = TextureWrapMode.Clamp;

            // Cells outside the hex stay transparent
            _colors = new Color[size * size];
            _texture.SetPixels(_colors);
            _texture.Apply();
            tileMap.texture = _texture;
            _initialized = true;
        }

        public void SetTileData(Vector2Int id, TileData[] data)
        {
            // Redraws of the same chunk keep the label instead of allocating a new string
            if (!_hasId || _id != id)
            {
                text.text = id.ToString();
            }

            _id = id;
            _hasId = true;
            ReturnTreeIcons();

            var colors = _colors;
            int size = _layout.Size;
            var origin = rectTransform.anchoredPosition - rectTransform.sizeDelta / 2f;
            for (int index = 0; index < data.Length; index++)
            {
                var cell = _layout.Cell(index);
                int pixel = cell.x + cell.y * size;
                var tileData = data[index];
                if (tileData.objectType == (int)TileObjectType.Scrap)
                {
                    colors[pixel] = scrapColor;
                    continue;
                }

                if (tileData.objectType == (int)TileObjectType.Tree)
                {
                    var treeIconId = tileData.objectId - 100;
                    if (treeIconId < 0 || treeSprites.Length <= treeIconId)
                    {
                        treeIconId = 0;
                    }

                    var icon = _imagePool.Get();
                    icon.rectTransform.anchoredPosition = origin + ((Vector2)cell + Vector2.one * 0.5f) * _unitsPerTile;
                    icon.sprite = treeSprites[treeIconId];

                    _spawnedImages.Add(icon);
                }

                colors[pixel] = _tileColors.GetValueOrDefault(tileData.ground, Color.magenta);
            }

            _texture.SetPixels(colors);
            _texture.Apply();

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