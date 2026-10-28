using Cysharp.Threading.Tasks;
using Eco.Scripts.Helpers;
using Eco.Scripts.Pooling;
using Eco.Scripts.ProgressionScreen;
using Eco.Scripts.UI;
using Eco.Scripts.Upgrades;
using Eco.Scripts.World;
using UnityEngine;
using VContainer;

namespace Eco.Scripts
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] GameUI gameUI;
        [SerializeField] private Map map;
        [SerializeField] WorldPreset worldPreset;
        [SerializeField] private ScoreGainedPopup scoreGainedPopup;

        private SaveManager _saveManager;
        private WorldController _worldController;
        private Settings _settings;
        private HelperManager _helperManager;
        private Player _player;
        private UpgradeManager _upgradeManager;
        private CurrencyManager _currencyManager;
        private ProgressTracker _progressTracker;

        public bool Initialized { get; private set; }

        [Inject]
        void Initialize(SaveManager saveManager, WorldController worldController, Settings settings,
            HelperManager helperManager, Player player, UpgradeManager upgradeManager,
            CurrencyManager currencyManager, ProgressTracker progressTracker)
        {
            _saveManager = saveManager;
            _worldController = worldController;
            _settings = settings;
            _helperManager = helperManager;
            _player = player;
            _upgradeManager = upgradeManager;
            _currencyManager = currencyManager;
            _progressTracker = progressTracker;
        }

        public void StartGame()
        {
            StartGameAsync().Forget();
        }

        private async UniTask StartGameAsync()
        {
            worldPreset = WorldSelector.Instance.SelectedPreset;
            _saveManager.Load(worldPreset.WorldId);

            PoolManager.Instance.InitializePools(worldPreset.AllowedScrapItems);

            _settings.Load();
            _upgradeManager.Load(_saveManager);

            scoreGainedPopup.Init();
            
            await UniTask.NextFrame();
            
            TerrainPainter.ClearTerrain();
            _worldController.SpawnWorld(worldPreset);
            _progressTracker.Init(worldPreset.ScrapPerChunk, worldPreset.WorldId);

           
            
            await UniTask.NextFrame();
            gameUI.Init(_upgradeManager, _currencyManager, _player, _progressTracker);
            await UniTask.NextFrame();

            _player.Spawn(_saveManager);

            _helperManager.LoadHelpers(_saveManager);
            _currencyManager.Init(_saveManager);

            map.Initialize(_worldController, _saveManager, _player);
            
            // Fade in after everything is loaded
            await UniTask.NextFrame();
            if (SceneTransition.Instance != null)
            {
                await SceneTransition.Instance.FadeIn();
            }
            
            Initialized = true;
        }

        [ContextMenu("Save Progress")]
        public void EndGame()
        {
            _player.Save(_saveManager);
            _helperManager.Save(_saveManager);
            _currencyManager.Save(_saveManager);
            _upgradeManager.Save(_saveManager);

            _worldController.SaveWorld();

            _saveManager.Save();
            _progressTracker.SaveProgress();

            gameUI.Clear();

            UnlockTracker.Clear();
        }

        private void OnApplicationQuit()
        {
            EndGame();
        }

        [ContextMenu("Delete Progress")]
        public void DeleteProgress()
        {
            SaveManager.DeleteProgress();
        }
    }
}