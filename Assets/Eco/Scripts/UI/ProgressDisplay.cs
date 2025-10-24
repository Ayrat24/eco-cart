using System;
using Eco.Scripts.World;
using R3;
using UnityEngine.UIElements;

namespace Eco.Scripts.UI
{
    public class ProgressDisplay
    {
        private readonly UIDocument _uiDocument;
        private readonly ProgressTracker _progressTracker;

        private Button _button;

        private IDisposable _subscription;
        private ProgressBar _clearProgressBar;
        private ProgressBar _greenProgressBar;

        public ProgressDisplay(UIDocument uiDocument, ProgressTracker progressTracker)
        {
            _uiDocument = uiDocument;
            _progressTracker = progressTracker;
        }

        public void Init()
        {
            var root = _uiDocument.rootVisualElement;

            _button = root.Q<Button>("SelectWorldButton");
            _button.clicked += OpenWorldSelector;
            
            // Find ProgressBars by name
            _clearProgressBar = root.Q<ProgressBar>("ClearProgress");
            _greenProgressBar = root.Q<ProgressBar>("GreenProgress");

            // Combine subscriptions into a single disposable using DisposableBuilder
            var builder = new DisposableBuilder();
            _progressTracker.ClearPercentage.Subscribe(UpdateClearProgress).AddTo(ref builder);
            _progressTracker.GreenPercentage.Subscribe(UpdateGreenProgress).AddTo(ref builder);
            _subscription = builder.Build();

            // Initialize UI with current values
            UpdateClearProgress(_progressTracker.ClearPercentage.Value);
            UpdateGreenProgress(_progressTracker.GreenPercentage.Value);
        }

        private void UpdateClearProgress(float percentage)
        {
            _clearProgressBar.value = percentage * 100;
        }

        private void UpdateGreenProgress(float percentage)
        {
            _greenProgressBar.value = percentage * 100;
        }

        private void OpenWorldSelector()
        {
            WorldSelector.Instance.Open();
        }

        public void Clear()
        {
            _subscription?.Dispose();
            _subscription = null;
            
            _button.clicked -= OpenWorldSelector;
        }
    }
}
