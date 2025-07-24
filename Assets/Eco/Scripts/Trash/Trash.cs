using UnityEngine;

namespace Eco.Scripts.Trash
{
    public class Trash : MonoBehaviour, ICartItem
    {
        [SerializeField] private TrashType trashType;
        public TrashType TrashType => trashType;
    
        public bool isCollected = false;
        Rigidbody rb;

        private void Start()
        {
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
            Destroy(gameObject);
        }

        public void SetInCartState(bool inCart)
        {
            ChangeState(true);
        }

        public void MakeKinematic(bool isKinematic)
        {
            rb.isKinematic = isKinematic;
        }
    }
}
