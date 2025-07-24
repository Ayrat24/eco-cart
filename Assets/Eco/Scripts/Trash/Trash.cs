using System.Collections;
using PrimeTween;
using UnityEngine;

public class Trash : MonoBehaviour, ICartItem
{
    [SerializeField] private TrashType trashType;
    public TrashType TrashType => trashType;
    
    public bool isCollected = false;
    Rigidbody rb;
    private Collider _collider;

    private void Start()
    {
        rb = GetComponentInChildren<Rigidbody>();
        _collider = GetComponentInChildren<Collider>();
        
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
        
        _collider.gameObject.layer = inCart ? cartLayer : groundLayer;
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
