using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Eco.Scripts.World
{
    public class WorldProgress
    {
        private readonly SaveManager _saveManager;
        private readonly WorldController _worldController;
        private int _totalChunks;
        private int _worldSize;
        private readonly HashSet<Vector2Int> _cleanChunks = new();

        public readonly ReactiveProperty<float> ClearPercentage = new();

        public WorldProgress(SaveManager saveManager, WorldController worldController)
        {
            _saveManager = saveManager;
            _worldController = worldController;
        }

        public void Init()
        {
            _worldSize = _worldController.WorldSize;
            _totalChunks = _worldSize * _worldSize;
            _saveManager.OnChunkUpdated.Subscribe(OnChunkUpdated);

            // Initial calculation of clean chunks
            foreach (var position in _saveManager.FieldTiles.Keys)
            {
                OnChunkUpdated(position);
            }
        }

        private void OnChunkUpdated(Vector2Int position)
        {
            if (_cleanChunks.Contains(position) || !_saveManager.FieldTiles.TryGetValue(position, out var tiles))
            {
                return;
            }

            //ignore world bounds/water
            var size = _worldController.WorldSideSize;
            if (position.x < -size || position.x > size ||
                position.y < -size || position.y > size)
            {
                return;
            }

            bool allClean = true;
            foreach (var tile in tiles)
            {
                if (!tile.clean)
                {
                    allClean = false;
                    break;
                }
            }

            if (allClean)
            {
                _cleanChunks.Add(position);
                SetClearPercentage();
            }
        }

        private void SetClearPercentage()
        {
            float percentage = 0;
            if (_totalChunks > 0)
            {
                percentage = (float)_cleanChunks.Count / _totalChunks;
            }

            ClearPercentage.Value = percentage;
        }
    }
}