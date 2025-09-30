using TMPro;
using UnityEngine;

namespace Eco.Scripts.ProgressionScreen
{
    public class MapTile : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TextMeshProUGUI text;
        private Vector2Int _id;
        public Vector2Int Id => _id;
        public RectTransform Rect => rectTransform;
    
        public void SetTileData(Vector2Int id)
        {
            _id = id;
            text.text = id.ToString();
        }
    }
}