using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LargeNumbers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.ItemCollecting
{
    public class GainedScoreText
    {
        private const string HiddenStyleClass = "hidden";
        private const float HideDelay = 1.0f;
        private const float HideSequenceDelay = 0.05f;

        private List<Label> _labels = new();
        private int _lastLabelIndex = -1;
        
        private Camera _camera;
        private VisualElement _itemContainer;
        private Label _totalScoreLabel;
        private AlphabeticNotation _totalScore; 
        private CancellationTokenSource _cancellationTokenSource;
        private bool _active;

        public void Init(UIDocument uiDocument)
        {
            _camera = Camera.main;
            _itemContainer = uiDocument.rootVisualElement.Q("Panel");
            _labels = _itemContainer.Query<Label>("GainLabel").ToList();
            _totalScoreLabel = _itemContainer.Q<Label>("TotalScore");
            _totalScore = new AlphabeticNotation(0);
            HideAllLabelsInstant();
            
        }

        private void HideAllLabelsInstant()
        {
            foreach (var label in _labels)
            {
                label.AddToClassList(HiddenStyleClass);
            }

            ResetScore();
        }

        private void ResetScore()
        {
            _totalScore = new AlphabeticNotation(0);
            UpdateTotalLabel(_totalScore);
            _lastLabelIndex = -1;
        }

        public void FaceCamera(Transform transform)
        {
            Vector3 targetPosition = transform.position + _camera.transform.rotation * Vector3.forward;
            Vector3 targetUp = _camera.transform.rotation * Vector3.up;
            transform.LookAt(targetPosition, targetUp);
        }

        public void StartNewRecycle()
        {
            if (_active)
            {
                Clear();
                HideAllLabelsInstant();
                ResetScore();
            }
            else
            {
                _itemContainer.RemoveFromClassList(HiddenStyleClass);
            }
            
            _active = true;
        }
        
        public void UpdateTotalLabel(AlphabeticNotation score)
        {
            _totalScore += score;
            _totalScoreLabel.text = $"+{_totalScore.ToString()}";
        }
        
        public void SpawnGainLabel(string text, Color color)
        {
            var label = GetLabel();
            label.text = text;

            color.a = 0.4f;
            label.style.backgroundColor = color;
        }

        private Label GetLabel()
        {
            _lastLabelIndex++;
            if (_lastLabelIndex < _labels.Count - 1)
            {
                var label = _labels[_lastLabelIndex];
                label.RemoveFromClassList(HiddenStyleClass);
                
                return label;
            }
            
            for (int i = 0; i < _labels.Count - 1; i++)
            {
                _labels[i].text = _labels[i + 1].text;
                _labels[i].style.backgroundColor = _labels[i + 1].style.backgroundColor;
            }
            
            return _labels[^1];
        }
        
        public void HideLabels()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            HideLabelsAsync(_cancellationTokenSource.Token).Forget();
        }
        
        private async UniTask HideLabelsAsync(CancellationToken token)
        {
            await UniTask.WaitForSeconds(2, cancellationToken: token);
            
            foreach (var label in _labels)
            {
                label.AddToClassList(HiddenStyleClass);
                await UniTask.WaitForSeconds(HideSequenceDelay, cancellationToken: token);
            }

            ResetScore();
            
            _itemContainer.AddToClassList(HiddenStyleClass);
            _active = false;
        }

        public void Clear()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
}