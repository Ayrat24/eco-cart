using Eco.Scripts.ItemCollecting;
using Eco.Scripts.Pooling;
using Eco.Scripts.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.Trash
{
    public class TrashItem : MonoBehaviour, ICartItem, ITileItem
    {
        [SerializeField] string itemName = "trash";
        [SerializeField] private int prefabTypeId;
        [SerializeField] private TrashType trashType;
        [SerializeField] private Color color;
        public TrashType TrashType => trashType;
    
        private bool _isCollected;
        private Rigidbody _rigidbody;
        private Field.Tile _tile;
        private bool _isBeingPickedUp;

        public void Initialize(Field.Tile tile)
        {
            _tile = tile;
            _rigidbody = GetComponent<Rigidbody>();
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
            SetPickedUpStatus(false);
            
            _tile.status = Field.TileStatus.Empty;
            _tile = null;
            
            PoolManager.Instance.ReturnItem(this);
        }

        public void SetInCartState(bool inCart)
        {
            ChangeState(true);
        }

        public void MakeKinematic(bool isKinematic)
        {
            _rigidbody.isKinematic = isKinematic;
        }

        public bool IsBeingPickedUp()
        {
            return _isBeingPickedUp;
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
            return $"{itemName}\n({trashType})";
        }

        public int GetPrefabId()
        {
            return prefabTypeId;
        }

        public bool CanBeRecycled => !_isBeingPickedUp && !_isCollected;
    }
}
