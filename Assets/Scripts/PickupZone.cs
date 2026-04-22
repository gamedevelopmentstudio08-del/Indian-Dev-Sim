using UnityEngine;

public sealed class PickupZone : MonoBehaviour
{
    private bool picked;

    private void OnTriggerEnter(Collider other)
    {
        if (picked || other == null || !other.CompareTag("Player"))
        {
            return;
        }

        picked = true;
        PassengerManager manager = FindObjectOfType<PassengerManager>();
        if (manager != null)
        {
            manager.PickupPassengers();
            return;
        }

        Debug.Log("Passengers Picked Up");
    }
}
