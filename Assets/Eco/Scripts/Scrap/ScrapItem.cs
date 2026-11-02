using Cysharp.Threading.Tasks;
using Eco.Scripts.ItemCollecting;
using Eco.Scripts.Profiling;
using Eco.Scripts.Pooling;
using Eco.Scripts.World;
using PrimeTween;
using R3;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace Eco.Scripts.Scrap
{
    public class ScrapItem : MonoBehaviour, ICartItem, ITileItem, IPooledObject
    {
        [SerializeField] string itemName = "scrap";
        [SerializeField] private int prefabTypeId;
        [SerializeField] private ScrapType scrapType;
        [SerializeField] private Color color = Color.wheat;
        [SerializeField] private int weight = 1;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private NavMeshObstacle navObstacle;
        [SerializeField] ParticleSystem particleEffect;
        [SerializeField] TrailRenderer trailRenderer;

        // Items taller than this share of their footprint would stand upright, so they get tipped over
        private const float TallItemRatio = 1.2f;
        // Metres an item sinks into the ground, which hides small gaps on uneven terrain
        private const float RestSink = 0.03f;

        private bool _isCollected;
        private Tile _tile;
        private bool _isBeingPickedUp;
        private bool _onGround;
        private bool _restPoseKnown;
        private Quaternion _restRotation;
        private float _restHeight;
        private bool _stainsGround;
        private Vector3 _stainPosition;
        private MeshFilter[] _meshes;
        private Renderer[] _renderers;
        private int[] _rendererLayers;

        public ScrapType ScrapType => scrapType;
        public static readonly Subject<Tile> OnItemRecycled = new();

        // Optional callback that, when set, will be used to return this item to a custom pool.
        // If null, the code will fall back to using PoolManager.Instance.ReturnItem(this).
        public System.Action<ScrapItem> ReturnToPoolCallback { get; set; }

        // Notifies an external owner that this item was returned to any pool (called from OnDespawn).
        // Owners should use this to update tracking state but should NOT attempt to return the
        // item to a pool again from inside this callback.
        public System.Action<ScrapItem> OnReturnedToPool { get; set; }

        public bool IsBeingPickedUp
        {
            get => _isBeingPickedUp;
            set => SetPickedUpStatus(value);
        }
        
        /// <summary>Lays the item at its current XZ, turned by <paramref name="yaw"/> degrees.</summary>
        public void Initialize(Tile tile, float yaw)
        {
            _tile = tile;

            var vfx = particleEffect.main;
            vfx.startColor = color;
            ChangeState(false);

            LayOnGround(yaw);
        }

        // Nothing moves an item on the ground until it is taken, so it is posed once and its stain never moves.
        // Scrap on a tile kills the ground under it; fruit that gets dropped doesn't.
        private void LayOnGround(float yaw)
        {
            if (!_restPoseKnown)
            {
                FindRestPose();
            }

            var ground = WorldTerrain.OnGround(transform.position);
            var slope = Quaternion.FromToRotation(Vector3.up, WorldTerrain.GroundNormal(ground));
            transform.SetPositionAndRotation(ground + Vector3.up * _restHeight,
                slope * Quaternion.Euler(0f, yaw, 0f) * _restRotation);

            _onGround = true;
            UpdateObstacle();

            if (_tile != null)
            {
                _stainsGround = true;
                _stainPosition = MeshBounds().center;
                ScrapStainMap.Add(_stainPosition, color);
            }
        }

        // Lies on its flattest side the way the art is modelled, or on its side if that would leave it standing up
        private void FindRestPose()
        {
            _meshes = GetComponentsInChildren<MeshFilter>();

            _restRotation = Quaternion.identity;
            transform.rotation = _restRotation;
            var bounds = MeshBounds();
            if (bounds.size.y > Mathf.Max(bounds.size.x, bounds.size.z) * TallItemRatio)
            {
                _restRotation = Quaternion.Euler(90f, 0f, 0f);
                transform.rotation = _restRotation;
                bounds = MeshBounds();
            }

            _restHeight = transform.position.y - bounds.min.y - RestSink;
            _restPoseKnown = true;
        }

        // Renderer bounds won't do: CurvedWorldBounds widens them for culling
        private Bounds MeshBounds()
        {
            var bounds = new Bounds(transform.position, Vector3.zero);
            bool any = false;
            foreach (var filter in _meshes)
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                var local = filter.sharedMesh.bounds;
                var matrix = filter.transform.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    var offset = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f,
                        (corner & 4) == 0 ? -1f : 1f);
                    var point = matrix.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, offset));
                    if (any)
                    {
                        bounds.Encapsulate(point);
                    }
                    else
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        any = true;
                    }
                }
            }

            return bounds;
        }

        private void RemoveStain()
        {
            if (_stainsGround)
            {
                _stainsGround = false;
                ScrapStainMap.Remove(_stainPosition);
            }
        }

        // Held up by the spot. It keeps its collider, which is how the collector finds it.
        public void Hang(Transform spot)
        {
            transform.SetParent(spot, false);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        public void OnPickUp(Transform parent)
        {
            if (_isCollected)
            {
                return;
            }

            _isCollected = true;
            transform.parent = parent;
            SetHeld(true);
        }

        private void ChangeState(bool inCart)
        {
            const int cartLayer = 8;
            const int groundLayer = 7;

            gameObject.layer = inCart ? cartLayer : groundLayer;
        }

        public void Recycle()
        {
            // Covers the synchronous part up to the first await: tile reset, save, tween setup.
            using var marker = EcoProfilerMarkers.RecycleScrap.Auto();
            RecycleAsync().Forget();
        }

        public async UniTask RecycleAsync()
        {
            SetHeld(true);
            SetPickedUpStatus(false);

            ResetTile();

            gameObject.layer = 0;
            transform.parent = null;

            var currentPosition = transform.localPosition;
            var endPosition =
                new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(5f, 7.9f), Random.Range(-2.5f, 2.5f)) +
                currentPosition;
            Tween.LocalPosition(transform, currentPosition, endPosition, 0.6f, Ease.InCubic);
            Tween.Scale(transform, 1, 1.3f, 0.6f, ease: Ease.OutElastic);
            Tween.EulerAngles(transform, Vector3.zero, Vector3.up * 180, 0.8f);

            trailRenderer.startColor = color;
            trailRenderer.gameObject.SetActive(true);

            await UniTask.WaitForSeconds(0.6f);
            Tween.Scale(transform, 1.3f, 0, 0.5f, ease: Ease.OutCirc);

            particleEffect.gameObject.SetActive(true);
            particleEffect.Play();

            await UniTask.WaitForSeconds(0.5f);
            ReturnToPool();
        }

        public void Dissolve()
        {
            using var marker = EcoProfilerMarkers.RecycleScrap.Auto();
            DissolveAsync().Forget();
        }

        // Melts away where it floats inside the jelly instead of flying off
        private async UniTask DissolveAsync()
        {
            const float duration = 0.7f;

            SetHeld(true);
            SetPickedUpStatus(false);
            ResetTile();
            gameObject.layer = 0;

            Tween.Scale(transform, transform.localScale, Vector3.zero, duration, Ease.InBack);
            await UniTask.WaitForSeconds(duration);
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            if (ReturnToPoolCallback != null)
            {
                ReturnToPoolCallback.Invoke(this);
            }
            else
            {
                PoolManager.Instance.ReturnItem(this);
            }
        }

        private void ResetTile()
        {
            if (_tile != null)
            {
                _tile.objectType = TileObjectType.Empty;
                _tile.containedScrap = true;
                _tile.parentChunk?.SaveTiles();
                OnItemRecycled?.OnNext(_tile);
                _tile = null;
            }
        }

        public void SetRendererLayer(int layer)
        {
            if (_renderers == null)
            {
                _renderers = GetComponentsInChildren<Renderer>(true);
                _rendererLayers = new int[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _rendererLayers[i] = _renderers[i].gameObject.layer;
                }
            }

            foreach (var itemRenderer in _renderers)
            {
                itemRenderer.gameObject.layer = layer;
            }
        }

        private void RestoreRendererLayers()
        {
            if (_renderers == null)
            {
                return;
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].gameObject.layer = _rendererLayers[i];
            }
        }

        public void SetInCartState(bool inCart)
        {
            ChangeState(inCart);
        }

        /// <summary>
        /// Taken off the ground, or put back on it where it is now. Held, swallowed or flying items don't bump into
        /// anything. Every way of taking an item comes through here, and its ground heals the moment it leaves.
        /// </summary>
        public void SetHeld(bool held)
        {
            rb.detectCollisions = !held;
            if (held)
            {
                _onGround = false;
                RemoveStain();
                UpdateObstacle();
            }
            else if (!_onGround)
            {
                transform.localScale = Vector3.one;
                LayOnGround(Random.Range(0f, 360f));
            }
        }

        public void SetPickedUpStatus(bool status)
        {
            _isBeingPickedUp = status;
            UpdateObstacle();
        }

        // Only loose scrap blocks the way: whoever has reserved an item must be able to walk up to it
        private void UpdateObstacle()
        {
            navObstacle.enabled = CanBeRecycled && _onGround;
        }

        public StyleColor GetColor()
        {
            return color;
        }

        public string GetName()
        {
            return $"{itemName}";
        }

        public int GetWeight()
        {
            return weight;
        }

        public int GetPrefabId()
        {
            return prefabTypeId;
        }

        public bool CanBeRecycled => !_isBeingPickedUp && !_isCollected;

        public void OnSpawn()
        {
            particleEffect.gameObject.SetActive(false);
            trailRenderer.gameObject.SetActive(false);
            
            transform.localScale = Vector3.one;
            ChangeState(false);
            // Physics never moves scrap; the kinematic body keeps its collider cheap to carry around
            rb.isKinematic = true;
            rb.detectCollisions = true;
            RestoreRendererLayers();

            _isBeingPickedUp = false;
            _isCollected = false;
            _onGround = false;
            _tile = null;
            UpdateObstacle();
        }

        public void OnDespawn()
        {
            RemoveStain();

            // Notify owner that this item has been returned to a pool.
            // Use null-conditional invocation instead of swallowing exceptions with try/catch.
            OnReturnedToPool?.Invoke(this);
        }
    }
}