using Eco.Scripts.World;
using System;
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
            }

            // Optionally select the first
            if (group.childCount > 0)
            {
                var first = group[0] as RadioButton;
                if (first != null)
                {
                    first.SetValueWithoutNotify(true);
                    // set initial selection without reloading the scene
                    SelectPreset(worldPresets[0], reload: false);
                }
            }
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
                // Reload the current active scene so the new world preset takes effect
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
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
