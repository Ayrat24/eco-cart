using System.Collections.Generic;
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

        [SerializeField] private List<TileColor> tileColors;
        [SerializeField] private Color trashColor;

        private Texture2D _texture;

        private Vector2Int _id;
        private bool _initialized;
        private Dictionary<int, Color> _tileColors = new();

        public Vector2Int Id => _id;
        public RectTransform Rect => rectTransform;

        public void Initialize(int size)
        {
            if (_initialized)
            {
                return;
            }

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

            var size = _texture.width;
            Color[] colors = new Color[size * size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    var objectType = data[y + x * size].objectType;
                    if (objectType == (int)TileObjectType.Trash)
                    {
                        colors[x + y * size] = trashColor;
                        continue;
                    }

                    colors[x + y * size] = _tileColors[data[y + x * size].ground];
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