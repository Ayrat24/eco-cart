using UnityEngine;

public class TriggerBroadcaster : MonoBehaviour
{
    public event System.Action<Collider> OnTriggered;

    private void OnTriggerEnter(Collider other)
    {
        OnTriggered?.Invoke(other);
    }
}