using Cysharp.Threading.Tasks;
using Eco.Scripts.ItemCollecting;
using Eco.Scripts.Pooling;
using Eco.Scripts.World;
using PrimeTween;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.Trash
{
    public class TrashItem : MonoBehaviour, ICartItem, ITileItem, IPooledObject
    {
        [SerializeField] string itemName = "trash";
        [SerializeField] private int prefabTypeId;
        [SerializeField] private TrashType trashType;
        [SerializeField] private Color color = Color.wheat;
        [SerializeField] private int weight = 1;
        [SerializeField] private Rigidbody rb;
        [SerializeField] ParticleSystem particleEffect;
        [SerializeField] TrailRenderer trailRenderer;

        private bool _isCollected;
        private Tile _tile;
        private bool _isBeingPickedUp;

        public TrashType TrashType => trashType;
        public static readonly Subject<Tile> OnItemRecycled = new();

        // Optional callback that, when set, will be used to return this item to a custom pool.
        // If null, the code will fall back to using PoolManager.Instance.ReturnItem(this).
        public System.Action<TrashItem> ReturnToPoolCallback { get; set; }

        // Notifies an external owner that this item was returned to any pool (called from OnDespawn).
        // Owners should use this to update tracking state but should NOT attempt to return the
        // item to a pool again from inside this callback.
        public System.Action<TrashItem> OnReturnedToPool { get; set; }

        public bool IsBeingPickedUp
        {
            get => _isBeingPickedUp;
            set => _isBeingPickedUp = value;
        }
        
        public void Initialize(Tile tile)
        {
            _tile = tile;

            var vfx = particleEffect.main;
            vfx.startColor = color;
            ChangeState(false);
        }

        public void OnPickUp(Transform parent)
        {
            if (_isCollected)
            {
                return;
            }

            _isCollected = true;
            transform.parent = parent;
        }

        private void ChangeState(bool inCart)
        {
            const int cartLayer = 8;
            const int groundLayer = 7;

            gameObject.layer = inCart ? cartLayer : groundLayer;
        }

        public void OnFallenOut()
        {
            ResetTile();
            SetPickedUpStatus(false);
            ChangeState(false);
            _isCollected = false;
            transform.parent = null;
        }

        public void Recycle()
        {
            RecycleAsync().Forget();
        }

        public async UniTask RecycleAsync()
        {
            MakeKinematic(true);
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
                OnItemRecycled?.OnNext(_tile);
                _tile = null;
            }
        }

        public void SetInCartState(bool inCart)
        {
            ChangeState(inCart);
        }

        public void MakeKinematic(bool isKinematic)
        {
            rb.isKinematic = isKinematic;
        }

        public void SetPickedUpStatus(bool status)
        {
            _isBeingPickedUp = status;
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

            _isBeingPickedUp = false;
            _isCollected = false;
        }

        public void OnDespawn()
        {
            // Notify owner that this item has been returned to a pool.
            // Use null-conditional invocation instead of swallowing exceptions with try/catch.
            OnReturnedToPool?.Invoke(this);
        }
    }
}