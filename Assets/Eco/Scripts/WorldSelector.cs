using Eco.Scripts.World;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace Eco.Scripts
{
    public class WorldSelector : MonoBehaviour
    {
        [SerializeField] UIDocument uiDocument;
        [SerializeField] WorldPreset[] worldPresets;

        public static WorldSelector Instance { get; private set; }

        // Currently selected preset and selection event
        public WorldPreset SelectedPreset { get; private set; }
        public event Action<WorldPreset> PresetSelected;

        private const string GroupName = "Worlds";
        private const string ContainerName = "Container";
        private const string CloseButtonName = "CloseButton";
        private const string HiddenStateClass = "hiddenScreen";

        private VisualElement _container;
        private Button _closeButton;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            var root = uiDocument.rootVisualElement;

            _container = root.Q(ContainerName);
            _closeButton = _container.Q<Button>(CloseButtonName);
            _closeButton.clicked += Close;
            
            // The RadioButtonGroup named GroupName is guaranteed to exist in the document.
            RadioButtonGroup group = root.Q<RadioButtonGroup>(GroupName);
            RadioButton lastSelected = null;

            var lastWorldId = SaveManager.GetLastWorldId();
            if (string.IsNullOrEmpty(lastWorldId))
            {
                lastWorldId = worldPresets[0].WorldId;
            }
            
            Debug.LogError(lastWorldId);
            
            // Clear existing and add radio buttons
            group.Clear();
            foreach (var preset in worldPresets)
            {
                var rb = new RadioButton(preset.WorldId);
                // register callback to handle selection (user action -> reload)
                rb.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                    {
                        SelectPreset(preset, reload: true);
                    }
                });
                
                group.Add(rb);

                if (lastWorldId == preset.WorldId)
                {
                    lastSelected = rb;
                }
            }

            lastSelected!.value = true;
        }

        private void Close()
        {
            _container.AddToClassList(HiddenStateClass);
        }
        
        public void Open()
        {
            _container.RemoveFromClassList(HiddenStateClass);
        }
        
        private void SelectPreset(WorldPreset preset, bool reload = true)
        {
            SelectedPreset = preset;
            PresetSelected?.Invoke(preset);

            if (reload)
            {
                LoadSceneAsync().Forget();
            }
        }
        
        private async UniTask LoadSceneAsync()
        {
            var loadOp = SceneManager.LoadSceneAsync(0);
            while (!loadOp.isDone)
            {
                await UniTask.Yield();
            }
            
            var gameController = FindFirstObjectByType<GameController>();
            gameController.StartGame();
        }

        private void OnDestroy()
        {
            if(_closeButton != null)
            {
                _closeButton.clicked -= Close;
            }
        }
    }
}
