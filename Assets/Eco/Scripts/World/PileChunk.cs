using System;
using Eco.Scripts.Profiling;
using Eco.Scripts.Scrap;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;

namespace Eco.Scripts.World
{
    public class PileChunk : Chunk
    {
        [SerializeField] private ScrapPile pile;
        [SerializeField] private GameObject flowers;
        [SerializeField] private GameObject butterflies;
        private Tile _tileWithPile;
        private IDisposable _unlockSubscription;
        private IDisposable _lockSubscription;
private CurrencyManager _currencyManager;
        private PlayerStats _stats;

        public const int DifficultyMultiplier = 5;

        public void Init(CurrencyManager currencyManager, PlayerStats stats, int worldPresetDifficulty)
        {
            using var marker = EcoProfilerMarkers.PileChunkInit.Auto();

            _currencyManager = currencyManager;
            _stats = stats;

            bool hasSave = SaveManager.FieldTiles.ContainsKey(Position);
            int pileSize = DifficultyMultiplier * worldPresetDifficulty; // default size
            int centerIndex = (ChunkSize / 2) * ChunkSize + (ChunkSize / 2);
            _tileWithPile = tiles[centerIndex];


            if (hasSave)
            {
                var savedTiles = SaveManager.FieldTiles[Position];
                var center = savedTiles[centerIndex];
                if (center.objectType > 0)
                {
                    pileSize = center.objectId; // size of pile saved in objectId
                }
                else
                {
                    pileSize = 0; // no pile
                }
            }

            bool pileExists = pileSize > 0;
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    var t = tiles[y * ChunkSize + x];
                    t.groundType = pileExists ? TileGroundType.Pile : TileGroundType.Grass;
                }
            }

            if (pileExists)
            {
                SpawnScrapPileAtTile(_tileWithPile, pileSize, worldPresetDifficulty);
                flowers.SetActive(false);
                butterflies.SetActive(false);
            }
            else
            {
                pile.Hide();
                ShowFlowers();
                ShowButterflies();
                PaintGrass();
            }

            // subscribe to unlock events so that if Flowers/Butterflies are unlocked later
            // and there's no pile, we enable those visuals
            _unlockSubscription = UnlockTracker.OnUnlocked.Subscribe(OnUpgradeUnlocked);
            _lockSubscription = UnlockTracker.OnLocked.Subscribe(OnUpgradeLocked);

            // A loaded chunk already matches its save
            if (!hasSave)
            {
                SaveTiles();
            }
        }

        private void SpawnScrapPileAtTile(Tile tile, int size, int difficulty)
        {
            pile.Initialize(size, difficulty, _stats.DigPower);
            tile.item = pile;
            tile.objectType = TileObjectType.ScrapPile;

            pile.OnPileCleaned += OnPileCleaned;
        }

        private void OnPileCleaned()
        {
            var tile = _tileWithPile;
            tile.item = null;
            tile.objectType = TileObjectType.Empty;
            tile.containedScrap = true;

            pile.OnPileCleaned -= OnPileCleaned;

            PaintGrass();

            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    var t = tiles[y * ChunkSize + x];
                    t.groundType = TileGroundType.Grass;
                }
            }

            // Award money and show popup
            var reward = _stats.PileScore;
            _currencyManager.AddMoney(reward);

            ScoreGainedPopup.Show(pile.transform.position, reward);

            // If the upgrades are unlocked, enable visuals when pile cleared
            ShowFlowers();
            ShowButterflies();
            SaveTiles();
        }

        // TerrainPainter paints on the next frame and skips circles that are already painted
        private void PaintGrass()
        {
            TerrainPainter.PaintTerrainTexture(TerrainPainter.TerrainTexture.Grass, GetTileWorldPosition(_tileWithPile),
                TreePlanter.PileGrassRadius);
        }

        private void OnUpgradeUnlocked(UnlockableUpgradeType upgrade)
        {
            // Only enable visuals if the pile is not present
            bool pilePresent = _tileWithPile is { objectType: TileObjectType.ScrapPile };

            if (pilePresent)
            {
                return;
            }

            if (upgrade == UnlockableUpgradeType.Flowers)
            {
                ShowFlowers();
            }
            else if (upgrade == UnlockableUpgradeType.Butterflies)
            {
                ShowButterflies();
            }
        }

        private void OnUpgradeLocked(UnlockableUpgradeType upgrade)
        {
            if (upgrade == UnlockableUpgradeType.Flowers)
            {
                flowers.SetActive(false);
            }
            else if (upgrade == UnlockableUpgradeType.Butterflies)
            {
                butterflies.SetActive(false);
            }
        }

        private void ShowFlowers()
        {
            if (!UnlockTracker.IsUpgradeUnlocked(UnlockableUpgradeType.Flowers))
            {
                return;
            }

            flowers.SetActive(true);
        }

        private void ShowButterflies()
        {
            if (!UnlockTracker.IsUpgradeUnlocked(UnlockableUpgradeType.Butterflies))
            {
                return;
            }

            butterflies.SetActive(true);
        }

        public override void OnDespawn()
        {
            if (pile != null)
            {
                pile.OnPileCleaned -= OnPileCleaned;
            }

            _unlockSubscription?.Dispose();
            _lockSubscription?.Dispose();

            base.OnDespawn();
        }
    }
}