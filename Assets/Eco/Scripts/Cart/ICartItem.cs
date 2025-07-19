using UnityEngine;

public interface ICartItem
{
    public void OnPickUp(Transform parent, Vector3 localOffset);
    public void OnFallenOut();
    public void Recycle();
}