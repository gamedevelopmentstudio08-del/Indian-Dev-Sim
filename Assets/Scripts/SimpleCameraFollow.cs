using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 3.6f, -8.5f);
    public Vector3 lookOffset = new Vector3(0f, 1.8f, 4.5f);
    public float smoothness = 7.5f;
    public bool rotateWithBus = true;
    public float perspectiveFieldOfView = 58f;

    private bool hasSnappedToTarget;

    private void Start()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = rotateWithBus ? target.TransformPoint(offset) : target.position + offset;
        Vector3 lookPoint = rotateWithBus ? target.TransformPoint(lookOffset) : target.position + lookOffset;

        if (!hasSnappedToTarget)
        {
            transform.position = targetPosition;
            hasSnappedToTarget = true;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothness * Time.deltaTime);
        }

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(lookPoint - transform.position, Vector3.up),
            smoothness * Time.deltaTime);
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = rotateWithBus ? target.TransformPoint(offset) : target.position + offset;
        Vector3 lookPoint = rotateWithBus ? target.TransformPoint(lookOffset) : target.position + lookOffset;
        transform.position = targetPosition;
        transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
        hasSnappedToTarget = true;
    }
}
