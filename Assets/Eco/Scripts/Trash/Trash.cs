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
        rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
    }

    public void OnPickUp(Transform parent, Vector3 localOffset)
    {
        if (isCollected) return;
        isCollected = true;

        _collider.enabled = false;
        rb.isKinematic = true;
        
        StartCoroutine(FlyToPosition(parent, localOffset));
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
        isCollected = false;
        transform.parent = null;
        rb.AddExplosionForce(100, transform.position, 500f);
    }

    public void Recycle()
    {
        Destroy(gameObject);
    }
}
