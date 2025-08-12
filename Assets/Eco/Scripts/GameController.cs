using UnityEngine;
using VContainer;

namespace Eco.Scripts
{
    public class GameController : MonoBehaviour
    {
        private SaveManager _saveManager;
        private WorldController _worldController;

        [Inject]
        void Initialize(SaveManager saveManager, WorldController worldController)
        {
            _saveManager = saveManager;
            _worldController = worldController;
        }

        private void Start()
        {
            StartGame();
        }

        private void StartGame()
        {
            TerrainPainter.ClearTerrain();
            _saveManager.LoadFieldTiles();
            _worldController.SpawnWorld();
        }

        [ContextMenu("Save Progress")]
        private void EndGame()
        {
            _worldController.SaveWorld();
            _saveManager.SaveFieldTiles();
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