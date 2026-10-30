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
            var bar = _uiDocument.rootVisualElement.Q<VisualElement>("WetnessBar");
            var fill = bar.Q<VisualElement>("WetnessFill");

            var builder = new DisposableBuilder();
            _player.Wetness.Unlocked
                .Subscribe(unlocked => bar.style.display = unlocked ? DisplayStyle.Flex : DisplayStyle.None)
                .AddTo(ref builder);
            _player.Wetness.Fill.Subscribe(amount => fill.style.width = Length.Percent(amount * 100)).AddTo(ref builder);
            _subscription = builder.Build();
        }

        public void Clear()
        {
            _subscription?.Dispose();
        }
    }
}
