using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.ItemCollecting
{
    public class GainedScoreText
    {
        private const string SpawnStyleClass = "base-label";
        private const string HiddenStyleClass = "hidden";
        private const float HideDelay = 1.0f;
        private const float HideSequenceDelay = 0.05f;

        private readonly Queue<Label> _labelPool = new();
        private readonly List<Label> _activeLabels = new();
        
        private Camera _camera;
        private VisualElement _itemContainer;
        private CancellationTokenSource _cancellationTokenSource;

        public void Init(UIDocument uiDocument)
        {
            _camera = Camera.main;
            _itemContainer = uiDocument.rootVisualElement.Q("Panel");
        }

        public void FaceCamera(Transform transform)
        {
            Vector3 targetPosition = transform.position + _camera.transform.rotation * Vector3.forward;
            Vector3 targetUp = _camera.transform.rotation * Vector3.up;
            transform.LookAt(targetPosition, targetUp);
        }

        public void SpawnLabel(string text, Color color)
        {
            Label label = GetLabel();
            label.text = text;
            label.style.color = color;

            // Reset styles
            label.BringToFront();
            label.AddToClassList(SpawnStyleClass);

            label.style.display = DisplayStyle.Flex;

            _activeLabels.Add(label);
        }

        private async UniTask HideAfterDelay(Label label, CancellationToken token)
        {
            await UniTask.WaitForSeconds(HideDelay, cancellationToken: token);

            label.AddToClassList(HiddenStyleClass);
        }

        private Label GetLabel()
        {
            Label label;

            if (_labelPool.Count > 0)
            {
                label = _labelPool.Dequeue();
            }
            else
            {
                label = new Label();
                _itemContainer.Add(label);
            }

            return label;
        }

        private void RecycleLabel(Label label)
        {
            label.style.display = DisplayStyle.None;
            label.ClearClassList();
            _activeLabels.Remove(label);
            _labelPool.Enqueue(label);
        }

        public void HideLabels()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            HideLabelsAsync(_cancellationTokenSource.Token).Forget();
        }

        private async UniTask HideLabelsAsync(CancellationToken token)
        {
            foreach (var label in _activeLabels)
            {
                await UniTask.WaitForSeconds(HideSequenceDelay, cancellationToken: token);
                HideAfterDelay(label, token).Forget();
            }
        }

        public void Clear()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;

            for (var i = _activeLabels.Count - 1; i >= 0; i--)
            {
                var label = _activeLabels[i];
                RecycleLabel(label);
            }
        }
    }
}