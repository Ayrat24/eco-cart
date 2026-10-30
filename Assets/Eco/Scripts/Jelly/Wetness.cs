using System;
using Eco.Scripts.Upgrades;
using Eco.Scripts.World;
using R3;
using UnityEngine;

namespace Eco.Scripts.Jelly
{
    /// <summary>
    /// The jelly's water bank, in metres of trail. It fills while the jelly is in the water and is spent on the
    /// metres of <see cref="SlimeTrail"/> it lays, so a dry jelly leaves no trail. Walking where no trail goes
    /// (grass, the shore) costs nothing. All of it is locked until <see cref="UnlockableUpgradeType.Trail"/> is
    /// bought, which starts it with a full bank. The bank's size and fill speed are in <see cref="PlayerStats"/>.
    /// </summary>
    public class Wetness : MonoBehaviour
    {
        [SerializeField] private SlimeTrail trail;
        [SerializeField] private WorldData world;

        private readonly ReactiveProperty<float> _fill = new();
        private readonly ReactiveProperty<bool> _unlocked = new();
        private PlayerStats _stats;
        private float _metres;
        private IDisposable _subscription;

        /// <summary>How full the bank is, 0 to 1.</summary>
        public ReadOnlyReactiveProperty<float> Fill => _fill;

        public ReadOnlyReactiveProperty<bool> Unlocked => _unlocked;

        /// <summary>Metres of trail left in the bank.</summary>
        public float Metres => _metres;

        public void Init(PlayerStats stats, float metres)
        {
            _stats = stats;
            _unlocked.Value = UnlockTracker.IsUpgradeUnlocked(UnlockableUpgradeType.Trail);
            SetMetres(_unlocked.Value ? metres : 0f);

            var builder = new DisposableBuilder();
            stats.WetnessCapacity.Subscribe(_ => SetMetres(_metres)).AddTo(ref builder);
            stats.TrailWidthScale.Subscribe(scale => trail.WidthScale = scale).AddTo(ref builder);
            trail.OnPainted.Subscribe(spent => SetMetres(_metres - spent)).AddTo(ref builder);
            UnlockTracker.OnUnlocked.Subscribe(OnUnlocked).AddTo(ref builder);
            UnlockTracker.OnLocked.Subscribe(OnLocked).AddTo(ref builder);
            _subscription = builder.Build();
        }

        /// <summary>Fills the bank at once.</summary>
        public void Refill()
        {
            SetMetres(_stats.WetnessCapacity.CurrentValue);
        }

        private void Update()
        {
            if (_unlocked.Value && WorldTerrain.GroundHeight(transform.position) < world.WaterHeight)
            {
                SetMetres(_metres + _stats.WetnessFillSpeed.CurrentValue * Time.deltaTime);
            }
        }

        private void SetMetres(float metres)
        {
            float capacity = _stats.WetnessCapacity.CurrentValue;
            _metres = Mathf.Clamp(metres, 0f, capacity);
            _fill.Value = _metres / capacity;
            trail.CanPaint = _metres > 0f;
        }

        private void OnUnlocked(UnlockableUpgradeType type)
        {
            if (type == UnlockableUpgradeType.Trail)
            {
                _unlocked.Value = true;
                Refill();
            }
        }

        private void OnLocked(UnlockableUpgradeType type)
        {
            if (type == UnlockableUpgradeType.Trail)
            {
                _unlocked.Value = false;
                SetMetres(0f);
            }
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
            _fill.Dispose();
            _unlocked.Dispose();
        }
    }
}
