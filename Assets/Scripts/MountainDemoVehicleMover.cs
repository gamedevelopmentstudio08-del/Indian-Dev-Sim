using UnityEngine;

public class MountainDemoVehicleMover : MonoBehaviour
{
    public Vector3[] waypoints;
    public float speed = 8f;
    public int startIndex = 0;
    public int moveDirection = 1;
    public float waypointReachDistance = 2.4f;
    public float laneOffset = 1.35f;

    private int _targetIndex;

    private void Start()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            enabled = false;
            return;
        }

        int clampedStart = Mathf.Clamp(startIndex, 0, waypoints.Length - 1);
        transform.position = GetLanePoint(clampedStart);
        _targetIndex = GetWrappedIndex(clampedStart + (moveDirection >= 0 ? 1 : -1));
        FaceTarget();
    }

    private void Update()
    {
        if (waypoints == null || waypoints.Length < 2)
        {
            return;
        }

        Vector3 flatTarget = GetLanePoint(_targetIndex);
        Vector3 toTarget = flatTarget - transform.position;
        float distance = toTarget.magnitude;

        if (distance <= waypointReachDistance)
        {
            _targetIndex = GetWrappedIndex(_targetIndex + (moveDirection >= 0 ? 1 : -1));
            FaceTarget();
            return;
        }

        Vector3 step = toTarget.normalized * speed * Time.deltaTime;
        if (step.magnitude > distance)
        {
            step = toTarget;
        }

        transform.position += step;
        if (step.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(step.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 6f * Time.deltaTime);
        }
    }

    private void FaceTarget()
    {
        Vector3 flatTarget = GetLanePoint(_targetIndex);
        Vector3 direction = flatTarget - transform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private Vector3 GetLanePoint(int index)
    {
        int current = GetWrappedIndex(index);
        int next = GetWrappedIndex(current + (moveDirection >= 0 ? 1 : -1));
        Vector3 point = waypoints[current];
        Vector3 nextPoint = waypoints[next];
        Vector3 tangent = (nextPoint - point).normalized;
        Vector3 side = new Vector3(-tangent.z, 0f, tangent.x) * laneOffset;
        return point + side + Vector3.up * 0.8f;
    }

    private int GetWrappedIndex(int index)
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return 0;
        }

        int count = waypoints.Length;
        index %= count;
        if (index < 0)
        {
            index += count;
        }
        return index;
    }
}
