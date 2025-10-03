using System.Collections.Generic;
using Eco.Scripts.Pooling;
using Eco.Scripts.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Eco.Scripts.ProgressionScreen
{
    public class MapField : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private RawImage tileMap;

        [SerializeField] private Image treeIconPrefab;

        [SerializeField] private List<TileColor> tileColors;
        [SerializeField] private Color trashColor;

        [SerializeField] private Sprite[] treeSprites;

        private Texture2D _texture;

        private Vector2Int _id;
        private bool _initialized;
        private Dictionary<int, Color> _tileColors = new();
        private ObjectPool<Image> _imagePool;
        private List<Image> _spawnedImages = new();

        public Vector2Int Id => _id;
        public RectTransform Rect => rectTransform;

        public void Initialize(int size, Transform iconParent)
        {
            if (_initialized)
            {
                return;
            }

            _imagePool = new ObjectPool<Image>(treeIconPrefab, 0, iconParent);

            foreach (var tc in tileColors)
            {
                _tileColors[(int)tc.groundType] = tc.color;
            }

            _texture = new Texture2D(size, size);
            _texture.filterMode = FilterMode.Point;

            Color[] colors = new Color[size * size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    colors[x + y * size] = Color.blue;
                }
            }

            _texture.SetPixels(colors);
            _texture.Apply();
            tileMap.texture = _texture;
            _initialized = true;
        }

        public void SetTileData(Vector2Int id, TileData[] data)
        {
            _id = id;
            text.text = id.ToString();

            foreach (var i in _spawnedImages)
            {
                _imagePool.ReturnToPool(i);
            }

            _spawnedImages.Clear();

            var size = _texture.width;
            Color[] colors = new Color[size * size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    int index = x + y * size;
                    var tileData = data[y + x * size];
                    if (tileData.objectType == (int)TileObjectType.Trash)
                    {
                        colors[index] = trashColor;
                        continue;
                    }

                    if (tileData.objectType == (int)TileObjectType.Tree)
                    {
                        var treeIconId = tileData.objectId - 100;
                        if (treeSprites.Length <= treeIconId)
                        {
                            treeIconId = 0;
                        }

                        var icon = _imagePool.Get();
                        icon.rectTransform.anchoredPosition = rectTransform.anchoredPosition +
                                                              new Vector2(x * size - rectTransform.sizeDelta.x / 2f,
                                                                  y * size - rectTransform.sizeDelta.x / 2f);
                        icon.sprite = treeSprites[treeIconId];

                        _spawnedImages.Add(icon);
                    }

                    colors[index] = _tileColors[data[y + x * size].ground];
                }
            }

            _texture.SetPixels(colors);
            _texture.Apply();

            tileMap.gameObject.SetActive(true);
        }

        public void SetAsUndiscovered()
        {
            tileMap.gameObject.SetActive(false);
        }

        [System.Serializable]
        struct TileColor
        {
            public TileGroundType groundType;
            public Color color;
        }
    }
}