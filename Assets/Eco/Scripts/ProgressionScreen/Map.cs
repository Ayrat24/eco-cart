using System.Collections;
using System.Collections.Generic;
using Eco.Scripts.Pooling;
using Eco.Scripts.ProgressionScreen;
using UnityEngine;
using UnityEngine.UI;

public class Map : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollView;
    [SerializeField] private RectTransform content;
    [SerializeField] private MapTile tile;
    [SerializeField] private int cellSize;
    [SerializeField] private int viewportSize;

    private Vector2Int _centerPoint = Vector2Int.one;
    private List<MapTile> _tiles = new List<MapTile>();
    private List<Vector2Int> _visibleIds = new();
    private ObjectPool<MapTile> _pool;

    void Start()
    {
        _pool = new ObjectPool<MapTile>(tile, viewportSize * viewportSize, content);
    }

    // Update is called once per frame
    void Update()
    {
        MoveTiles();
    }

    public void ResetView()
    {
        scrollView.StopMovement();
        content.anchoredPosition = Vector2.zero;
    }
    
    private void MoveTiles()
    {
        Vector2Int center = new Vector2Int((int)(content.anchoredPosition.x / 100), (int)(content.anchoredPosition.y /
            100));

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
            // t.transform.parent = content;
            // t.transform.localScale = Vector3.one;
            t.Rect.anchoredPosition = id * 100;
            t.SetTileData(id);
            _tiles.Add(t);
        }
    }
}