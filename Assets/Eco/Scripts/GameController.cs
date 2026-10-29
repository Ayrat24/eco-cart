using Cysharp.Threading.Tasks;
using Eco.Scripts.Helpers;
using Eco.Scripts.Pooling;
using Eco.Scripts.ProgressionScreen;
using Eco.Scripts.UI;
using Eco.Scripts.Upgrades;
using Eco.Scripts.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace Eco.Scripts
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] GameUI gameUI;
        [SerializeField] private Map map;
        [SerializeField] private ScoreGainedPopup scoreGainedPopup;
        [Tooltip("Start the game when the scene loads. Off in test scenes that only need the scene's objects.")]
        [SerializeField] private bool startOnLoad = true;

        private SaveManager _saveManager;
        private WorldController _worldController;
        private Settings _settings;
        private HelperManager _helperManager;
        private Player _player;
        private UpgradeManager _upgradeManager;
        private CurrencyManager _currencyManager;

        public bool Initialized { get; private set; }

        [Inject]
        void Initialize(SaveManager saveManager, WorldController worldController, Settings settings,
            HelperManager helperManager, Player player, UpgradeManager upgradeManager,
            CurrencyManager currencyManager)
        {
            _saveManager = saveManager;
            _worldController = worldController;
            _settings = settings;
            _helperManager = helperManager;
            _player = player;
            _upgradeManager = upgradeManager;
            _currencyManager = currencyManager;
        }

        private void Start()
        {
            if (startOnLoad)
            {
                StartGame();
            }
        }

        public void StartGame()
        {
            StartGameAsync().Forget();
        }

        private async UniTask StartGameAsync()
        {
            var worldData = _worldController.WorldData;
            _saveManager.Load();

            PoolManager.Instance.InitializePools(worldData.ScrapItems);

            _settings.Load();
            _upgradeManager.Load(_saveManager);

            scoreGainedPopup.Init();

            await UniTask.NextFrame();

            TerrainPainter.ResetTerrain(worldData.GrassLayer);
            var spawnPosition = _worldController.SpawnWorld(_saveManager.Progress.playerPosition?.ToVector3());

            await UniTask.NextFrame();
            gameUI.Init(_upgradeManager, _currencyManager, _player, ResetProgress);
            await UniTask.NextFrame();

            _player.Spawn(_saveManager, spawnPosition);

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
            if (!Initialized)
            {
                return;
            }

            Initialized = false;

            _player.Save(_saveManager);
            _helperManager.Save(_saveManager);
            _currencyManager.Save(_saveManager);
            _upgradeManager.Save(_saveManager);

            _worldController.SaveWorld();

            _saveManager.Save();

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

        // Settings → reset progress: wipe the save and start over in a freshly loaded scene
        private void ResetProgress()
        {
            if (!Initialized)
            {
                return;
            }

            // Nothing of this game may be saved over the wiped files
            Initialized = false;
            SaveManager.DeleteProgress();
            gameUI.Clear();
            UnlockTracker.Clear();
            ReloadSceneAsync().Forget();
        }

        private static async UniTaskVoid ReloadSceneAsync()
        {
            if (SceneTransition.Instance != null)
            {
                await SceneTransition.Instance.FadeOut();
                await UniTask.WaitForSeconds(SceneTransition.FadeDuration);
            }

            // The new scene's GameController starts the game
            await SceneManager.LoadSceneAsync(0);
        }
    }
}
