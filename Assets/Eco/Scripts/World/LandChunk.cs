using System.Collections.Generic;
using Eco.Scripts.Profiling;
using Eco.Scripts.Pooling;
using Eco.Scripts.Scrap;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Eco.Scripts.World
{
    /// <summary>
    /// A walkable hex: loose scrap and planted trees on its tiles, and optionally a scrap pile in the middle
    /// (<see cref="PileSite"/>, sized by <see cref="ChunkScrapSettings.PileSize"/>).
    /// </summary>
    public class LandChunk : Chunk
    {
        // Tiles this close to the pile are pile ground on the map, and stay free of loose scrap
        private const float PileRadius = 2.5f;

        // Random candidates tried per scrap item; the one furthest from the others wins
        private const int ScrapPlacementCandidates = 8;

        // Piles sit up to this share of the hex radius off its centre, so they don't line up on the grid
        private const float PileJitter = 0.3f;

        private ChunkContext _context;
        private PileSite _pileSite;
        private Tile _pileTile;
        private bool _pileCleared;
        private readonly List<ScrapItem> _restingScrap = new();
        private readonly List<int> _scrapTiles = new();

        private bool HasPileSite => _pileSite != null && Definition.Scrap.HasPile;

        protected override void Awake()
        {
            base.Awake();
            _pileSite = GetComponentInChildren<PileSite>(true);
        }

        public override void Init(ChunkContext context)
        {
            using var marker = EcoProfilerMarkers.ChunkInit.Auto();

            _context = context;
            _pileTile = tiles[Layout.CenterIndex];

            bool hasSave = HasSave;
            if (hasSave)
            {
                LoadSave();
            }

            UpdateWater(context.World, false);
            if (hasSave)
            {
                DropDrownedScrap();
            }

            InitPile(hasSave);

            if (!hasSave)
            {
                CreateScrap();
            }

            PaintGrass();

            // A loaded chunk already matches its save
            if (!hasSave)
            {
                SaveTiles();
            }
        }

        private void InitPile(bool hasSave)
        {
            if (_pileSite != null && !Definition.Scrap.HasPile)
            {
                _pileSite.gameObject.SetActive(false);
            }

            if (!Definition.Scrap.HasPile)
            {
                return;
            }

            if (_pileSite == null)
            {
                Debug.LogWarning($"Chunk '{Definition.name}' has a scrap pile but its prefab has no ScrapPileSite. " +
                                 "Open it in the World Editor to fix the prefab.", this);
                return;
            }

            PlacePileSite();

            int maxSize = Definition.Scrap.PileSize;
            int size = maxSize;
            if (hasSave)
            {
                // The pile's remaining size is saved in its tile's objectId
                var saved = SavedTiles[_pileTile.index];
                size = saved.objectType == (int)TileObjectType.ScrapPile ? saved.objectId : 0;
            }

            if (size <= 0)
            {
                _pileCleared = true;
                _pileSite.ShowCleared();
                return;
            }

            _pileCleared = false;
            _pileSite.ShowPile(size, maxSize, _context.Stats.DigPower);
            _pileSite.Cleaned += OnPileCleaned;
            _pileTile.item = _pileSite.Pile;
            _pileTile.objectType = TileObjectType.ScrapPile;

            if (!hasSave)
            {
                SetGroundAroundPile(TileGroundType.Pile, PileRadius);
            }
        }

        // A fixed spot and turn per hex (saves find the pile on the same tile), on the ground, off the centre
        private void PlacePileSite()
        {
            float angle = Hex.Random01(Position, 1) * Mathf.PI * 2f;
            float distance = Hex.Random01(Position, 2) * Layout.Radius * PileJitter;
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            int index = Layout.IndexAt(offset);
            if (index < 0 || tiles[index].groundType == TileGroundType.Water)
            {
                offset = Vector2.zero;
                index = Layout.CenterIndex;
            }

            _pileTile = tiles[index];
            var position = WorldTerrain.OnGround(transform.position + new Vector3(offset.x, 0, offset.y));
            _pileSite.transform.SetPositionAndRotation(position,
                Quaternion.Euler(0f, Hex.Random01(Position, 3) * 360f, 0f));
        }

        private void OnPileCleaned()
        {
            _pileSite.Cleaned -= OnPileCleaned;
            _pileCleared = true;

            _pileTile.item = null;
            _pileTile.objectType = TileObjectType.Empty;
            _pileTile.containedScrap = true;

            SetGroundAroundPile(TileGroundType.Grass, TreePlanter.PileGrassRadius);
            PaintGrass();

            // Award money and show popup
            var reward = _context.Stats.PileScore;
            _context.Currency.AddMoney(reward);
            ScoreGainedPopup.Show(_pileSite.Pile.transform.position, reward);

            SaveTiles();
        }

        private void SetGroundAroundPile(TileGroundType ground, float radius)
        {
            var center = Layout.LocalPosition(_pileTile.index);
            foreach (var tile in tiles)
            {
                if (tile.groundType != TileGroundType.Water &&
                    (Layout.LocalPosition(tile.index) - center).sqrMagnitude <= radius * radius)
                {
                    tile.groundType = ground;
                }
            }
        }

        private void CreateScrap()
        {
            int target = Mathf.Min(Definition.Scrap.RollCount(), tiles.Length);
            if (target == 0)
            {
                return;
            }

            var pileCenter = Layout.LocalPosition(_pileTile.index);
            bool avoidPile = HasPileSite;
            _scrapTiles.Clear();

            // Best of a few random candidates: keeps the scrap spread out without a fixed pattern
            for (int i = 0; i < target; i++)
            {
                int best = -1;
                float bestDistance = -1f;
                for (int attempt = 0; attempt < ScrapPlacementCandidates; attempt++)
                {
                    int candidate = Random.Range(0, tiles.Length);
                    var position = Layout.LocalPosition(candidate);
                    if (tiles[candidate].objectType != TileObjectType.Empty ||
                        tiles[candidate].groundType == TileGroundType.Water ||
                        (avoidPile && (position - pileCenter).sqrMagnitude <= PileRadius * PileRadius))
                    {
                        continue;
                    }

                    float distance = float.MaxValue;
                    foreach (int other in _scrapTiles)
                    {
                        distance = Mathf.Min(distance, (Layout.LocalPosition(other) - position).sqrMagnitude);
                    }

                    if (distance > bestDistance)
                    {
                        bestDistance = distance;
                        best = candidate;
                    }
                }

                if (best < 0)
                {
                    continue;
                }

                _scrapTiles.Add(best);
                SpawnScrapAtTile(tiles[best], PickScrapPrefab(), false);
            }
        }

        // Saved scrap the terrain now puts under water (the world was baked again since) couldn't be reached
        private void DropDrownedScrap()
        {
            foreach (var tile in tiles)
            {
                if (tile.groundType != TileGroundType.Water || tile.item is not ScrapItem scrap)
                {
                    continue;
                }

                _restingScrap.Remove(scrap);
                PoolManager.Instance.ReturnItem(scrap);
                tile.item = null;
                tile.objectType = TileObjectType.Empty;
            }
        }

        private ScrapItem PickScrapPrefab()
        {
            var items = Definition.Scrap.Items.Count > 0 ? Definition.Scrap.Items : _context.World.ScrapItems;
            return items.Count > 0 ? items[Random.Range(0, items.Count)] : null;
        }

        private void LoadSave()
        {
            for (int index = 0; index < tiles.Length; index++)
            {
                Tile tile = tiles[index];
                var savedData = SavedTiles[index];
                tile.groundType = (TileGroundType)savedData.ground;
                tile.containedScrap = savedData.containedScrap;

                switch ((TileObjectType)savedData.objectType)
                {
                    case TileObjectType.Scrap:
                        var scrap = SpawnScrapAtTile(tile, savedData.objectId, savedData.containedScrap);
                        if (_context.RestingPoses.TryTake(Position, index, out var pose))
                        {
                            scrap.PlaceAtRest(pose);
                            _restingScrap.Add(scrap);
                        }
                        break;
                    case TileObjectType.Tree:
                        _context.TreePlanter.PlantTree(savedData.objectId, tile, this);
                        break;
                }
            }
        }

        public void MakeGrass()
        {
            PaintGrass();
            SaveTiles();
        }

        // TerrainPainter paints on the next frame and skips circles that are already painted
        private void PaintGrass()
        {
            foreach (var tile in tiles)
            {
                if (tile.objectType == TileObjectType.Tree)
                {
                    TerrainPainter.PaintGrass(GetTileWorldPosition(tile), TreePlanter.TreeGrassRadius);
                }
            }

            if (HasPileSite && _pileCleared)
            {
                TerrainPainter.PaintGrass(GetTileWorldPosition(_pileTile), TreePlanter.PileGrassRadius);
            }
        }

        private ScrapItem SpawnScrapAtTile(Tile tile, ScrapItem prefab, bool contained)
        {
            var scrap = prefab != null ? PoolManager.Instance.GetScrap(prefab) : PoolManager.Instance.GetRandomScrap();
            return PlaceScrap(scrap, tile, contained);
        }

        private ScrapItem SpawnScrapAtTile(Tile tile, int id, bool contained)
        {
            var scrap = id <= 0 ? PoolManager.Instance.GetRandomScrap() : PoolManager.Instance.GetScrap(id);
            return PlaceScrap(scrap, tile, contained);
        }

        private ScrapItem PlaceScrap(ScrapItem scrap, Tile tile, bool contained)
        {
            scrap.transform.parent = transform;
            scrap.transform.position = GetTileWorldPosition(tile);
            scrap.Initialize(tile);
            tile.item = scrap;
            tile.objectType = TileObjectType.Scrap;
            tile.containedScrap = contained;
            return scrap;
        }

        /// <summary>
        /// Called by WorldController once the spawned chunks' transforms have reached physics.
        /// </summary>
        public void SleepRestingScrap()
        {
            foreach (var scrap in _restingScrap)
            {
                scrap.Sleep();
            }

            _restingScrap.Clear();
        }

        /// <summary>
        /// Called by WorldController before the chunk goes back to the pool: the pool deactivates it before
        /// OnDespawn, and an inactive rigidbody no longer reports whether it was asleep.
        /// </summary>
        public void StoreRestingScrap()
        {
            for (var i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].item is ScrapItem scrap && scrap.TryGetRestingPose(out var pose))
                {
                    _context.RestingPoses.Store(Position, i, pose);
                }
            }
        }

        public override void OnDespawn()
        {
            if (_pileSite != null)
            {
                _pileSite.Cleaned -= OnPileCleaned;
                _pileSite.Clear();
            }

            _restingScrap.Clear();
            base.OnDespawn();
        }
    }
}
