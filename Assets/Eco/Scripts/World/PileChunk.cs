using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Trash;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;

namespace Eco.Scripts.World
{
    public class PileChunk : Chunk
    {
        [SerializeField] private TrashPile pile;
        [SerializeField] private GameObject flowers;
        [SerializeField] private GameObject butterflies;
        private Tile _tileWithPile;
        private IDisposable _unlockSubscription;
        private IDisposable _digPowerSubscription;
        private CurrencyManager _currencyManager;
        private PileScoreUpgrade _pileScoreUpgrade;
        private DigPowerUpgrade _digPowerUpgrade;

        public const int DifficultyMultiplier = 5;

        public void Init(CurrencyManager currencyManager, PileScoreUpgrade pileScoreUpgrade,
            DigPowerUpgrade digPowerUpgrade, int worldPresetDifficulty)
        {
            _currencyManager = currencyManager;
            _pileScoreUpgrade = pileScoreUpgrade;
            _digPowerUpgrade = digPowerUpgrade;

            // Subscribe to dig power level changes to update pile dynamically
            _digPowerSubscription = _digPowerUpgrade.CurrentLevel.Subscribe(_ => UpdatePileDigPower());

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

            if(Position == new Vector2Int(-2,0))
            {
                Debug.LogError(hasSave + " " + pileSize);
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
                SpawnTrashPileAtTile(_tileWithPile, pileSize, worldPresetDifficulty);
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

            if (!hasSave)
            {
                SaveTiles();
            }
        }

        private void SpawnTrashPileAtTile(Tile tile, int size, int difficulty)
        {
            int digPower = _digPowerUpgrade != null ? _digPowerUpgrade.DigPower : 1;
            pile.Initialize(size, difficulty, digPower);
            tile.item = pile;
            tile.objectType = TileObjectType.TrashPile;

            pile.OnPileCleaned += OnPileCleaned;

            SaveTiles();
        }

        private void UpdatePileDigPower()
        {
            if (_digPowerUpgrade != null && pile != null && pile.gameObject.activeSelf)
            {
                pile.SetDigPower(_digPowerUpgrade.DigPower);
            }
        }

        private void OnPileCleaned()
        {
            var tile = _tileWithPile;
            tile.item = null;
            tile.objectType = TileObjectType.Empty;

            Debug.LogError("here");

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
            var reward = _pileScoreUpgrade.ScoreForCurrentUpgrade;
            _currencyManager.AddMoney(reward);

            ScoreGainedPopup.Show(pile.transform.position, reward);

            // If the upgrades are unlocked, enable visuals when pile cleared
            ShowFlowers();
            ShowButterflies();
            SaveTiles();
        }

        private void PaintGrass()
        {
            MakeGrass(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask MakeGrass(CancellationToken cancellationToken)
        {
            await UniTask.NextFrame(cancellationToken);
            TerrainPainter.PaintTerrainTexture(TerrainPainter.TerrainTexture.Grass, GetTileWorldPosition(_tileWithPile),
                TreePlanter.PileGrassRadius);
        }

        private void OnUpgradeUnlocked(UnlockableUpgradeType upgrade)
        {
            // Only enable visuals if the pile is not present
            bool pilePresent = _tileWithPile is { objectType: TileObjectType.TrashPile };

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
            _digPowerSubscription?.Dispose();

            base.OnDespawn();
        }
    }
}