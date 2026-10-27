using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Jelly;
using Eco.Scripts.Profiling;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Eco.Scripts.ItemCollecting
{
    /// <summary>
    /// The jelly's belly, which took over from the cart. A tentacle reserves room for an item as it reaches for it
    /// and then pushes it in through the mouth; inside, items float in the goo and the body grows by their size.
    /// Recycling dissolves them one by one and the body shrinks back. Tuning is in <see cref="StomachSettings"/>.
    /// The stomach's capacity is weight from the upgrades and doesn't depend on size.
    /// Lives on a child of the creature root with an identity local pose; swallowed items are parented to it.
    /// </summary>
    [DefaultExecutionOrder(105)]
    public class Stomach : MonoBehaviour
    {
        [SerializeField] private JellyBody body;
        [SerializeField] private StomachSettings settings;

        // Swallowed items move to this layer: the renderers draw it only inside the jelly (JellyInsideFeature)
        private const string SwallowedLayerName = "Swallowed Item";

        private const float MaxSpeed = 4f;

        private readonly List<ICartItem> _items = new();
        private readonly List<FloatingItem> _floating = new();
        private readonly List<Vector3> _forces = new();

        private bool _isEmptying;
        private int _swallowedLayer;
        private CancellationTokenSource _cancellationTokenSource;
        private ItemRecycler _itemRecycler;
        private ItemCollector _itemCollector;
        private CartBuyUpgrade.CartData _stats;

        public readonly Subject<ICartItem> OnItemAdded = new();
        public readonly Subject<ICartItem> OnItemRemoved = new();
        public readonly Subject<StomachState> OnStatusChanged = new();

        public bool IsFull => CurrentWeight >= _stats.carryingCapacity;
        public bool CanAddItems => !IsFull && !_isEmptying;
        public bool IsEmptying => _isEmptying;
        public int StorageSize => _stats.carryingCapacity;
        public string Id => _stats.id;
        public CartBuyUpgrade.CartData Stats => _stats;
        public JellyBody Body => body;

        private int CurrentWeight
        {
            get
            {
                int weight = 0;
                foreach (var item in _items)
                {
                    weight += item.GetWeight();
                }

                return weight;
            }
        }

        private class FloatingItem
        {
            public ICartItem Item;
            public Transform Transform;
            public Vector3 Velocity;
            public Vector3 SpinAxis;
            public float Radius;
            public float Volume;
            public float Seed;
            public bool Dissolving;
        }

        public void Init(ItemRecycler recycler, ItemCollector itemCollector)
        {
            _itemRecycler = recycler;
            _itemCollector = itemCollector;

            _swallowedLayer = LayerMask.NameToLayer(SwallowedLayerName);
            if (_swallowedLayer < 0)
            {
                Debug.LogError($"Layer '{SwallowedLayerName}' is missing: swallowed items will draw outside the jelly.");
            }
        }

        public void SetStats(CartBuyUpgrade.CartData stats)
        {
            _stats = stats;
            OnStatusChanged.OnNext(CurrentState());
        }

        public bool CanFitItem(ICartItem item)
        {
            return CurrentWeight + item.GetWeight() <= _stats.carryingCapacity;
        }

        /// <summary>Counts an item in as soon as a tentacle goes for it, so nothing else claims the room.</summary>
        public bool Reserve(ICartItem item)
        {
            using var marker = EcoProfilerMarkers.ReserveInStomach.Auto();

            if (item == null || _items.Contains(item))
            {
                return false;
            }

            _items.Add(item);
            OnItemAdded.OnNext(item);
            OnStatusChanged.OnNext(IsFull ? StomachState.Full : StomachState.HasItems);
            return true;
        }

        /// <summary>Gives back the room of a reserved item that never made it in.</summary>
        public bool Release(ICartItem item)
        {
            if (item == null || !_items.Remove(item))
            {
                return false;
            }

            OnItemRemoved.OnNext(item);
            OnStatusChanged.OnNext(CurrentState());
            return true;
        }

        /// <summary>Uniform local scale the item should be squeezed to by the time it goes through the mouth.</summary>
        public float GetSwallowedScale(ICartItem item)
        {
            return item is Component component
                ? component.transform.localScale.x * settings.ItemScale
                : settings.ItemScale;
        }

        /// <summary>The item's two longest sides once swallowed, in metres; the mouth opens to fit them.</summary>
        public Vector2 GetSwallowedSize(ICartItem item)
        {
            if (item is not Component component)
            {
                return Vector2.zero;
            }

            // The largest renderer, measured along its own axes so the way the item lies doesn't matter
            var largest = Vector3.zero;
            foreach (var renderer in component.GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer or TrailRenderer)
                {
                    continue;
                }

                var size = Vector3.Scale(renderer.localBounds.size, renderer.transform.lossyScale);
                size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
                if (size.x * size.y * size.z > largest.x * largest.y * largest.z)
                {
                    largest = size;
                }
            }

            float longest = MaxComponent(largest);
            float shortest = Mathf.Min(largest.x, Mathf.Min(largest.y, largest.z));
            float middle = largest.x + largest.y + largest.z - longest - shortest;
            return new Vector2(longest, middle) * settings.ItemScale;
        }

        /// <summary>Takes an item that a tentacle pushed through the mouth; it floats inside from now on.</summary>
        public void Swallow(ICartItem item)
        {
            if (item is not Component component)
            {
                return;
            }

            item.OnPickUp(transform);
            if (_swallowedLayer >= 0)
            {
                item.SetRendererLayer(_swallowedLayer);
            }

            _floating.Add(new FloatingItem
            {
                Item = item,
                Transform = component.transform,
                Velocity = transform.InverseTransformDirection(-body.MouthNormal + Vector3.down * 0.3f) *
                           settings.EntrySpeed,
                SpinAxis = Random.onUnitSphere,
                Radius = MaxComponent(GetBounds(component).extents) * 0.8f,
                Volume = GetVolume(component),
                Seed = Random.Range(0f, 100f)
            });

            UpdateGrowth();
            body.Poke(body.MouthPosition, 0.05f);
            body.Squash(-0.6f);
        }

        public void Recycle()
        {
            if (_isEmptying || _items.Count == 0)
            {
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            RecycleAsync(_cancellationTokenSource.Token).Forget();
        }

        private async UniTask RecycleAsync(CancellationToken token)
        {
            _isEmptying = true;
            OnStatusChanged.OnNext(StomachState.Recycling);
            body.SetDigesting(true);

            await UniTask.WaitUntil(() => _itemCollector.AllHandsAreFree(), cancellationToken: token);
            await UniTask.WaitForSeconds(0.1f, cancellationToken: token);

            await _itemRecycler.EmptyAsync(_items.ToList(), OnItemRecycled, token);

            ClearItems();
            body.SetDigesting(false);

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _isEmptying = false;
            OnStatusChanged.OnNext(StomachState.Empty);
        }

        private void OnItemRecycled(ICartItem item)
        {
            _items.Remove(item);

            foreach (var floating in _floating)
            {
                if (floating.Item == item)
                {
                    floating.Dissolving = true;
                    body.Poke(floating.Transform.position, 0.03f);
                    break;
                }
            }

            UpdateGrowth();
            OnItemRemoved.OnNext(item);
        }

        private void ClearItems()
        {
            _items.Clear();
            body.SetGrowth(1f);

            OnItemRemoved.OnNext(null);
            OnStatusChanged.OnNext(StomachState.Empty);
        }

        private StomachState CurrentState()
        {
            if (_isEmptying)
            {
                return StomachState.Recycling;
            }

            if (_items.Count == 0)
            {
                return StomachState.Empty;
            }

            return IsFull ? StomachState.Full : StomachState.HasItems;
        }

        // The body takes on volume for every item inside: growth in size is the cube root of that
        private void UpdateGrowth()
        {
            float volume = 0f;
            foreach (var floating in _floating)
            {
                if (!floating.Dissolving)
                {
                    volume += floating.Volume;
                }
            }

            float growth = Mathf.Pow(1f + settings.GrowthPerItemVolume * volume / body.BaseVolume, 1f / 3f);
            body.SetGrowth(Mathf.Min(settings.MaxGrowth, growth));
        }

        private void LateUpdate()
        {
            // Dissolved items go back to their pool, which takes them out of here
            for (int i = _floating.Count - 1; i >= 0; i--)
            {
                var itemTransform = _floating[i].Transform;
                if (itemTransform == null || itemTransform.parent != transform)
                {
                    _floating.RemoveAt(i);
                }
            }

            JellyInsideFeature.IsEmpty = _floating.Count == 0;

            float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
            if (_floating.Count == 0 || dt <= 0f)
            {
                return;
            }

            using var marker = EcoProfilerMarkers.StomachFloat.Auto();

            float scale = body.WorldScale;
            float stretch = body.Stretch;
            float cavityHeight = settings.CavityHeight;
            var cavitySize = settings.CavitySize;
            var center = body.RestToParent(new Vector3(0f, cavityHeight * body.RestHeight, 0f));
            float radius = body.RestRadiusAt(cavityHeight) * scale / Mathf.Sqrt(stretch);
            var radii = new Vector3(radius * cavitySize.x, body.RestHeight * scale * stretch * cavitySize.y,
                radius * cavitySize.z);

            var push = new Vector3(0f, -settings.Sinking, 0f) -
                       transform.InverseTransformDirection(body.Acceleration) * settings.Slosh;
            float drift = settings.Drift;
            float time = Time.timeSinceLevelLoad * 0.5f;

            _forces.Clear();
            foreach (var item in _floating)
            {
                var force = push + new Vector3(
                    Mathf.PerlinNoise(time, item.Seed) - 0.5f,
                    Mathf.PerlinNoise(item.Seed, time) - 0.5f,
                    Mathf.PerlinNoise(time + 31f, item.Seed + 17f) - 0.5f) * (2f * drift);

                // Keep inside the cavity, an ellipsoid shrunk by the item's own radius
                var inner = Vector3.Max(radii - Vector3.one * item.Radius, Vector3.one * 0.05f);
                var offset = item.Transform.localPosition - center;
                var scaled = new Vector3(offset.x / inner.x, offset.y / inner.y, offset.z / inner.z);
                float outside = scaled.magnitude - 1f;
                if (outside > 0f)
                {
                    var wallNormal = new Vector3(scaled.x / inner.x, scaled.y / inner.y, scaled.z / inner.z).normalized;
                    force -= wallNormal * (outside * Mathf.Min(inner.x, Mathf.Min(inner.y, inner.z)) * settings.WallStiffness);
                    float outward = Vector3.Dot(item.Velocity, wallNormal);
                    if (outward > 0f)
                    {
                        item.Velocity -= wallNormal * (outward * 0.5f);
                    }
                }

                _forces.Add(force);
            }

            // Items push each other apart; dissolving ones make way
            for (int i = 0; i < _floating.Count; i++)
            {
                if (_floating[i].Dissolving)
                {
                    continue;
                }

                for (int j = i + 1; j < _floating.Count; j++)
                {
                    if (_floating[j].Dissolving)
                    {
                        continue;
                    }

                    var delta = _floating[j].Transform.localPosition - _floating[i].Transform.localPosition;
                    float distance = delta.magnitude;
                    float minDistance = (_floating[i].Radius + _floating[j].Radius) * 0.85f;
                    if (distance >= minDistance)
                    {
                        continue;
                    }

                    var direction = distance > 1e-4f ? delta / distance : Random.onUnitSphere;
                    var separation = direction * ((minDistance - distance) * settings.SeparationStiffness);
                    _forces[i] -= separation;
                    _forces[j] += separation;
                }
            }

            float damping = Mathf.Exp(-settings.Viscosity * dt);
            for (int i = 0; i < _floating.Count; i++)
            {
                var item = _floating[i];
                item.Velocity = Vector3.ClampMagnitude((item.Velocity + _forces[i] * dt) * damping, MaxSpeed);
                item.Transform.localPosition += item.Velocity * dt;

                item.SpinAxis = (item.SpinAxis + Random.insideUnitSphere * dt).normalized;
                item.Transform.localRotation =
                    Quaternion.AngleAxis(settings.Tumble * dt, item.SpinAxis) * item.Transform.localRotation;
            }
        }

        private static Bounds GetBounds(Component component)
        {
            var bounds = new Bounds(component.transform.position, Vector3.zero);
            bool found = false;
            foreach (var renderer in component.GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer or TrailRenderer)
                {
                    continue;
                }

                if (found)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    found = true;
                }
            }

            return bounds;
        }

        // Bounding box volume at the item's current size, whichever way it is turned
        private static float GetVolume(Component component)
        {
            float volume = 0f;
            foreach (var renderer in component.GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer or TrailRenderer)
                {
                    continue;
                }

                var size = Vector3.Scale(renderer.localBounds.size, renderer.transform.lossyScale);
                volume += Mathf.Abs(size.x * size.y * size.z);
            }

            return volume;
        }

        private static float MaxComponent(Vector3 v)
        {
            return Mathf.Max(v.x, Mathf.Max(v.y, v.z));
        }

        private void OnDestroy()
        {
            JellyInsideFeature.IsEmpty = true;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        public enum StomachState
        {
            Empty,
            HasItems,
            Full,
            Recycling
        }
    }
}
