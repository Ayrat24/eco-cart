using Eco.Scripts.Pooling;
using Eco.Scripts.World;
using UnityEngine;

namespace Eco.Scripts.Trash
{
    public class TrashItem : MonoBehaviour, ICartItem, ITileItem
    {
        [SerializeField] private int prefabTypeId;
        [SerializeField] private TrashType trashType;
        public TrashType TrashType => trashType;
    
        public bool isCollected = false;
        Rigidbody rb;
        private Field.Tile _tile;

        public void Initialize(Field.Tile tile)
        {
            _tile = tile;
            rb = GetComponent<Rigidbody>();
            ChangeState(false);
        }

        public void OnPickUp(Transform parent)
        {
            if (isCollected)
            {
                return;
            }
        
            isCollected = true;
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
            ChangeState(false);
            isCollected = false;
            transform.parent = null;
        }

        public void Recycle()
        {
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
            rb.isKinematic = isKinematic;
        }

        public int GetPrefabId()
        {
            return prefabTypeId;
        }
    }
}
