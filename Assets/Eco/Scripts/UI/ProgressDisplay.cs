using System;
using Eco.Scripts.World;
using UnityEngine.UIElements;
using R3;
using UnityEngine;

public class ProgressDisplay
{
    private readonly UIDocument _uiDocument;
    private readonly WorldProgress _worldProgress;

    private IDisposable _subscription;
    private ProgressBar _progressBar;

    public ProgressDisplay(UIDocument uiDocument, WorldProgress worldProgress)
    {
        _uiDocument = uiDocument;
        _worldProgress = worldProgress;
    }

    public void Init()
    {
        if (_uiDocument == null || _worldProgress == null)
        {
            return;
        }

        var root = _uiDocument.rootVisualElement;

        // Try to find a ProgressBar named "ClearProgress" first
        _progressBar = root.Q<ProgressBar>("ClearProgress");

        // Subscribe to clear percentage changes
        _subscription = _worldProgress.ClearPercentage.Subscribe(UpdateProgress);

        // Initialize UI with current value
        UpdateProgress(_worldProgress.ClearPercentage.Value);
    }

    private void UpdateProgress(float percentage)
    {
        _progressBar.value = percentage * 100;
    }

    public void Clear()
    {
        _subscription?.Dispose();
        _subscription = null;
    }
}




