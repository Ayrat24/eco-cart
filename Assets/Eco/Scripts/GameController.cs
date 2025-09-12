using Cysharp.Threading.Tasks;
using Eco.Scripts.Helpers;
using Eco.Scripts.Trees;
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
        [SerializeField] CameraController cameraController;

        private SaveManager _saveManager;
        private WorldController _worldController;
        private Settings _settings;
        private HelperManager _helperManager;
        private Player _player;
        private UpgradesCollection _upgradeCollection;
        private CurrencyManager _currencyManager;
        private TreeManager _treeManager;

        [Inject]
        void Initialize(SaveManager saveManager, WorldController worldController, Settings settings,
            HelperManager helperManager, Player player, UpgradesCollection upgradesCollection,
            CurrencyManager currencyManager, TreeManager treeManager)
        {
            _saveManager = saveManager;
            _worldController = worldController;
            _settings = settings;
            _helperManager = helperManager;
            _player = player;
            _upgradeCollection = upgradesCollection;
            _currencyManager = currencyManager;
            _treeManager = treeManager;
        }

        private void Start()
        {
            StartGameAsync().Forget();
        }

        private async UniTask StartGameAsync()
        {
            _saveManager.LoadPlayerProgress();

            _settings.Load();
            TerrainPainter.ClearTerrain();
            _saveManager.LoadFieldTiles();
            _worldController.SpawnWorld();

            _saveManager.LoadPlayerProgress();
            _upgradeCollection.Load(_saveManager);
            
            await UniTask.NextFrame();
            gameUI.Init(_upgradeCollection, _currencyManager, _player);
            cameraController.Init(_player);
            await UniTask.NextFrame();
            
            _player.Spawn(_saveManager);

            _helperManager.LoadHelpers();
            _currencyManager.Init(_saveManager);
            _treeManager.Init();
        }

        [ContextMenu("Save Progress")]
        private void EndGame()
        {
            _player.SavePosition(_saveManager);
            _currencyManager.Save(_saveManager);
            _upgradeCollection.Save(_saveManager);
            _worldController.SaveWorld();

            _saveManager.SaveFieldTiles();
            _saveManager.SavePlayerProgress();

            _treeManager.Clear();
            gameUI.Clear();
        }

        private void OnApplicationQuit()
        {
            EndGame();
        }

        [ContextMenu("Delete Progress")]
        public void DeleteProgress()
        {
            _saveManager = new SaveManager();
            _saveManager.DeleteProgress();
        }
    }
}