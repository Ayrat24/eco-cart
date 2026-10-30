using System;
using R3;
using UnityEngine.UIElements;

namespace Eco.Scripts.UI
{
    public class WetnessDisplay
    {
        private readonly Player _player;
        private readonly UIDocument _uiDocument;

        private IDisposable _subscription;

        public WetnessDisplay(UIDocument uiDocument, Player player)
        {
            _player = player;
            _uiDocument = uiDocument;
        }

        public void Init()
        {
            var fill = _uiDocument.rootVisualElement.Q<VisualElement>("WetnessFill");
            _subscription = _player.Wetness.Amount.Subscribe(amount => fill.style.width = Length.Percent(amount * 100));
        }

        public void Clear()
        {
            _subscription?.Dispose();
        }
    }
}
