using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Pooling;
using Eco.Scripts.World;
using UnityEngine;

namespace Eco.Scripts.Trees
{
    public class Tree : MonoBehaviour, ITileItem, IPooledObject
    {
        [SerializeField] private int prefabId;
        [SerializeField] private Transform model;

        private Vector3 _restScale;
        private Quaternion _restRotation;
        private CancellationTokenSource _growthCancellation;

        public Transform Model => model;

        /// <summary>Only on trees that can bear fruit.</summary>
        public FruitBearer Bearer { get; private set; }
        public bool CanBeRecycled => true;
        public int GetPrefabId() => prefabId;

        private void Awake()
        {
            _restScale = model.localScale;
            _restRotation = model.localRotation;
            Bearer = GetComponent<FruitBearer>();
        }

        public void ShowPlantAnimation(TreeGrowthSettings settings)
        {
            ResetGrowth();
            if (settings == null) return;
            _growthCancellation = new CancellationTokenSource();
            PlayGrowthAsync(settings, _growthCancellation.Token).Forget();
        }

        private async UniTask PlayGrowthAsync(TreeGrowthSettings settings, CancellationToken token)
        {
            float elapsed = 0f;
            while (elapsed < settings.Duration)
            {
                float progress = elapsed / settings.Duration;
                model.localScale = Vector3.Scale(_restScale, settings.ScaleAt(progress));
                model.localRotation = _restRotation * Quaternion.Euler(0f, 0f, settings.SwayAt(progress));
                await UniTask.NextFrame(cancellationToken: token);
                elapsed += Time.deltaTime;
            }
            ResetGrowth();
        }

        // Streaming or restarting an animation must never leave a pooled tree tiny or tilted.
        private void ResetGrowth()
        {
            _growthCancellation?.Cancel();
            _growthCancellation?.Dispose();
            _growthCancellation = null;
            if (model == null) return;
            model.localScale = _restScale;
            model.localRotation = _restRotation;
        }

        public void OnSpawn() => ResetGrowth();
        public void OnDespawn()
        {
            ResetGrowth();
            if (Bearer != null) Bearer.Clear();
        }

        private void OnDisable() => ResetGrowth();
        private void OnDestroy() => ResetGrowth();
    }
}
