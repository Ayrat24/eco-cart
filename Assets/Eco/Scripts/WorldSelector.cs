using Eco.Scripts.World;
using System;
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
        [SerializeField] VisualTreeAsset worldItemTemplate;

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
        private RadioButtonGroup _worldGroup;

        // Cached UI elements for each world to avoid rebuilding
        private class WorldItemUI
        {
            public ProgressBar ClearProgress;
            public Label ClearLabel;
            public ProgressBar GreenProgress;
            public Label GreenLabel;
        }

        private WorldItemUI[] _worldItemsUI;

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
            _worldGroup = root.Q<RadioButtonGroup>(GroupName);

            BuildWorldItems();
        }

        private void BuildWorldItems()
        {
            RadioButton lastSelected = null;

            var lastWorldId = SaveManager.GetLastWorldId();
            if (string.IsNullOrEmpty(lastWorldId))
            {
                lastWorldId = worldPresets[0].WorldId;
            }

            // Initialize cache array
            _worldItemsUI = new WorldItemUI[worldPresets.Length];

            // Clear existing and add custom world items with progress info
            _worldGroup.Clear();
            for (var i = 0; i < worldPresets.Length; i++)
            {
                var preset = worldPresets[i];

                // Create world item from template
                var worldItem = worldItemTemplate.CloneTree();
                var itemRoot = worldItem.Q<VisualElement>("WorldItem");

                // Get the radio button
                var rb = itemRoot.Q<RadioButton>("WorldRadio");
                rb.value = false;

                var number = itemRoot.Q<Label>("Number");
                number.text = (i + 1).ToString();

                // Cache UI elements for later updates
                _worldItemsUI[i] = new WorldItemUI
                {
                    ClearProgress = itemRoot.Q<ProgressBar>("ClearProgress"),
                    ClearLabel = itemRoot.Q<Label>("ClearLabel"),
                    GreenProgress = itemRoot.Q<ProgressBar>("GreenProgress"),
                    GreenLabel = itemRoot.Q<Label>("GreenLabel")
                };

                // Update difficulty stars (this doesn't change)
                var difficultyLabel = itemRoot.Q<Label>("DifficultyLabel");
                difficultyLabel.text = GetDifficultyStars(preset.Difficulty);

                // Register callback to handle selection (user action -> reload)
                rb.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                    {
                        SelectPreset(preset, reload: true);
                    }
                });

                // Add the entire item to the group
                _worldGroup.Add(itemRoot);

                if (lastWorldId == preset.WorldId)
                {
                    lastSelected = rb;
                }
            }

            if (lastSelected != null)
            {
                lastSelected.value = true;
            }

            // Update progress values
            UpdateWorldProgress();
        }

        private void UpdateWorldProgress()
        {
            if (_worldItemsUI == null) return;

            for (var i = 0; i < worldPresets.Length; i++)
            {
                var preset = worldPresets[i];
                var progressData = WorldProgressData.LoadForWorld(preset.WorldId);
                var ui = _worldItemsUI[i];

                // Update progress bars and labels
                ui.ClearProgress.value = progressData.ClearPercentage * 100f;
                ui.ClearLabel.text = $"{progressData.ClearPercentage:P0}";

                ui.GreenProgress.value = progressData.GreenPercentage * 100f;
                ui.GreenLabel.text = $"{progressData.GreenPercentage:P0}";
            }
        }

        private string GetDifficultyStars(int difficulty)
        {
            // Clamp difficulty between 1 and 5
            difficulty = Mathf.Clamp(difficulty, 1, 5);

            string stars = "";
            for (int i = 0; i < difficulty; i++)
            {
                stars += "★";
            }

            // Add empty stars for the remaining
            for (int i = difficulty; i < 5; i++)
            {
                stars += "☆";
            }

            return stars;
        }

        private void Close()
        {
            _container.AddToClassList(HiddenStateClass);
        }

        public void Open()
        {
            // Only update progress values, don't rebuild the entire UI
            UpdateWorldProgress();
            _container.RemoveFromClassList(HiddenStateClass);
        }

        private void SelectPreset(WorldPreset preset, bool reload = true, bool save = true)
        {
            if(save)
            {
                var oldGameController = FindFirstObjectByType<GameController>();
                if (oldGameController != null && oldGameController.Initialized)
                {
                    oldGameController.EndGame();
                }
            }

            SelectedPreset = preset;
            PresetSelected?.Invoke(preset);

            if (reload)
            {
                LoadSceneAsync().Forget();
            }
        }

        public void LoadWorld(int worldId, bool save = true)
        {
            SelectPreset(worldPresets[worldId], save: save);
        }

        private async UniTask LoadSceneAsync()
        {
            // Fade out with transition
            if (SceneTransition.Instance != null)
            {
                await SceneTransition.Instance.FadeOut();
                await UniTask.WaitForSeconds(SceneTransition.FadeDuration);
            }

            var loadOp = SceneManager.LoadSceneAsync(0);
            while (!loadOp.isDone)
            {
                await UniTask.Yield();
            }

            Close();

            var gameController = FindFirstObjectByType<GameController>();
            gameController.StartGame();
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.clicked -= Close;
            }
        }
    }
}