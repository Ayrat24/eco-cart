using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Eco.Scripts.World
{
    public class ProgressTracker
    {
        private readonly SaveManager _saveManager;
        private readonly WorldController _worldController;
        private int _worldSize;

        public readonly ReactiveProperty<float> ClearPercentage = new();

        // Tracks percentage of grass tiles (TileGroundType.Ground)
        public readonly ReactiveProperty<float> GreenPercentage = new();

        // total tiles in the world (chunks * chunkSize * chunkSize)
        private int _totalTiles;

        // total green tiles across the world
        private int _greenTilesTotal;

        // total clean tiles across the world
        private int _cleanTilesTotal;

        // per-chunk green tile counts to allow replacing counts on updates
        private readonly Dictionary<Vector2Int, int> _greenTilesPerChunk = new();

        // per-chunk clean tile counts to allow replacing counts on updates
        private readonly Dictionary<Vector2Int, int> _cleanTilesPerChunk = new();

        public ProgressTracker(SaveManager saveManager, WorldController worldController)
        {
            _saveManager = saveManager;
            _worldController = worldController;
        }

        public void Init()
        {
            _worldSize = _worldController.WorldSize;
            _totalTiles = _worldSize * _worldSize * WorldController.ChunkSize * WorldController.ChunkSize;
            _saveManager.OnChunkUpdated.Subscribe(OnChunkUpdated);

            // Initial calculation of clean/green tiles
            foreach (var position in _saveManager.FieldTiles.Keys)
            {
                OnChunkUpdated(position);
            }
        }

        private void OnChunkUpdated(Vector2Int position)
        {
            // get tiles for this chunk; if none, ignore
            if (!_saveManager.FieldTiles.TryGetValue(position, out var tiles))
                return;

            // ignore world bounds/water
            var size = _worldController.WorldSideSize;
            if (position.x < -size || position.x > size ||
                position.y < -size || position.y > size)
            {
                return;
            }

            // Compute both green and clean counts in a single pass
            int greenCount = 0;
            int cleanCount = 0;
            foreach (var tile in tiles)
            {
                if (tile.ground == (int)TileGroundType.Ground)
                    greenCount++;

                if (tile.clean)
                    cleanCount++;
            }

            if (_greenTilesPerChunk.TryGetValue(position, out var prevGreen))
                _greenTilesTotal -= prevGreen;
            _greenTilesPerChunk[position] = greenCount;
            _greenTilesTotal += greenCount;

            if (_cleanTilesPerChunk.TryGetValue(position, out var prevClean))
                _cleanTilesTotal -= prevClean;
            _cleanTilesPerChunk[position] = cleanCount;
            _cleanTilesTotal += cleanCount;

            SetGreenPercentage();
            SetClearPercentage();
        }

        private void SetClearPercentage()
        {
            float percentage = 0;
            if (_totalTiles > 0)
            {
                percentage = (float)_cleanTilesTotal / _totalTiles;
            }

            ClearPercentage.Value = percentage;
        }

        private void SetGreenPercentage()
        {
            float percentage = 0;
            if (_totalTiles > 0)
            {
                percentage = (float)_greenTilesTotal / _totalTiles;
            }

            GreenPercentage.Value = percentage;
        }
    }
}