using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.ItemCollecting
{
    public class GainedScoreText
    {
        private float hideDelay = 2f;
        private string spawnStyleClass = "base-label";
        private string hiddenStyleClass = "hidden";

        private Queue<Label> _labelPool = new();
        private List<Label> _activeLabels = new();


        private Camera _camera;
        private VisualElement _itemContainer;
        private CancellationTokenSource _cancellationTokenSource;

        public void Init(UIDocument uiDocument)
        {
            _camera = Camera.main;
            _itemContainer = uiDocument.rootVisualElement.Q("Panel");

            var root = uiDocument.rootVisualElement;
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
            label.AddToClassList(spawnStyleClass);

            label.style.display = DisplayStyle.Flex;

            _activeLabels.Add(label);
        }

        private async UniTask HideAfterDelay(Label label, float delay, CancellationToken token)
        {
            await UniTask.WaitForSeconds(1f, cancellationToken: token);

            if (label != null)
            {
                label.AddToClassList(hiddenStyleClass);
            }
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
                await UniTask.WaitForSeconds(0.05f, cancellationToken: token);
                HideAfterDelay(label, 1.2f, token).Forget();
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