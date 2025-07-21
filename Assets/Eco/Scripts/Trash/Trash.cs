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

    public void OnPickUp(Transform parent, Vector3 localOffset)
    {
        if (isCollected) return;
        isCollected = true;

        ChangeState(true);
        rb.isKinematic = true;
        
        StartCoroutine(FlyToPosition(parent, localOffset));
    }

    private void ChangeState(bool inCart)
    {
        const int cartLayer = 8;
        const int groundLayer = 7;
        
        _collider.gameObject.layer = inCart ? cartLayer : groundLayer;
    }

    IEnumerator FlyToPosition(Transform parent, Vector3 localOffset)
    {
        transform.SetParent(parent);
        float duration = 0.5f;
        float t = 0f;

        Tween.LocalPosition(transform, localOffset, duration, Ease.Linear);

        yield return new WaitForSeconds(duration);
        transform.localPosition = localOffset;
        
        _collider.enabled = true;
        rb.isKinematic = false;
    }

    public void OnFallenOut()
    {
        ChangeState(false);
        isCollected = false;
        transform.parent = null;
        //rb.AddExplosionForce(100, transform.position, 500f);
    }

    public void Recycle()
    {
        Destroy(gameObject);
    }
}
