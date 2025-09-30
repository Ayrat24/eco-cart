using Cysharp.Threading.Tasks;
using Eco.Scripts.ItemCollecting;
using Eco.Scripts.Pooling;
using Eco.Scripts.World;
using PrimeTween;
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
        public TrashType TrashType => trashType;

        private bool _isCollected;
        private Tile _tile;
        private bool _isBeingPickedUp;

        public void Initialize(Tile tile)
        {
            _tile = tile;

            var vfx = particleEffect.main;
            vfx.startColor = color;
            //particleEffect.main = vfx;
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

            if(_tile != null)
            {
                _tile.status = TileStatus.Empty;
                _tile = null;
            }
            
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
            PoolManager.Instance.ReturnItem(this);
        }

        public void SetInCartState(bool inCart)
        {
            ChangeState(true);
        }

        public void MakeKinematic(bool isKinematic)
        {
            rb.isKinematic = isKinematic;
        }

        bool ICartItem.IsBeingPickedUp { get; set; }

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
        }

        public void OnDespawn()
        {
        }
    }
}