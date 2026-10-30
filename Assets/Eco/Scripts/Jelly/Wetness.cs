using System;
using Eco.Scripts.World;
using R3;
using UnityEngine;

namespace Eco.Scripts.Jelly
{
    /// <summary>
    /// How wet the jelly is. It fills up while the jelly is in the water and is spent on the metres of
    /// <see cref="SlimeTrail"/> it lays, so a dry jelly leaves no trail. Walking where no trail goes (grass, the
    /// shore) costs nothing. Tuning is in <see cref="WetnessSettings"/>.
    /// </summary>
    public class Wetness : MonoBehaviour
    {
        [SerializeField] private WetnessSettings settings;
        [SerializeField] private SlimeTrail trail;
        [SerializeField] private WorldData world;

        private readonly ReactiveProperty<float> _amount = new();
        private IDisposable _subscription;

        /// <summary>How full the wetness bar is, 0 to 1.</summary>
        public ReadOnlyReactiveProperty<float> Amount => _amount;

        public void Init(float amount)
        {
            _amount.Value = Mathf.Clamp01(amount);

            var builder = new DisposableBuilder();
            _amount.Subscribe(value => trail.CanPaint = value > 0f).AddTo(ref builder);
            trail.OnPainted.Subscribe(Spend).AddTo(ref builder);
            _subscription = builder.Build();
        }

        /// <summary>Fills the bar at once.</summary>
        public void Refill()
        {
            _amount.Value = 1f;
        }

        private void Update()
        {
            if (WorldTerrain.GroundHeight(transform.position) < world.WaterHeight)
            {
                _amount.Value = Mathf.Min(_amount.Value + Time.deltaTime / settings.FillSeconds, 1f);
            }
        }

        private void Spend(float metres)
        {
            _amount.Value = Mathf.Max(_amount.Value - metres / settings.TrailLength, 0f);
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
            _amount.Dispose();
        }
    }
}
