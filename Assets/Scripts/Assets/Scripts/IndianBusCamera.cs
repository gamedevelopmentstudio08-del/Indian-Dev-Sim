using UnityEngine;

// Indian Bus Simulator — v1.0 (Unity)
// Purpose: Cinematic third-person follow camera for heavy vehicles.

public class IndianBusCamera : MonoBehaviour
{
    public Transform targetBus;
    public Vector3 offset = new Vector3(0, 4.5f, -10f); 
    public float smoothSpeed = 0.125f;
    public float rotationSmoothSpeed = 5f;

    void LateUpdate()
    {
        if (!targetBus) return;

        // Position follow with smoothing
        Vector3 desiredPosition = targetBus.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Rotation look at the bus center of gravity
        Quaternion targetRotation = Quaternion.LookRotation(targetBus.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }
}
