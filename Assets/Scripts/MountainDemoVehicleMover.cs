using UnityEngine;

public class MountainDemoVehicleMover : MonoBehaviour
{
    public Vector3[] waypoints;
    public float speed = 8f;
    public float minSpeed = 4.5f;
    public float maxSpeed = 12f;
    public float curveSlowdownStrength = 0.55f;
    public float uphillSlowdownStrength = 0.35f;
    public float downhillSlowdownStrength = 0.18f;
    public int startIndex = 0;
    public int moveDirection = 1;
    public float waypointReachDistance = 2.4f;
    public float laneOffset = 1.35f;

    private int _targetIndex;
    private float _currentSpeed;

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
        _currentSpeed = Mathf.Clamp(speed, minSpeed, maxSpeed);
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

        float desiredSpeed = ComputeDesiredSpeed(_targetIndex);
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, desiredSpeed, 6f * Time.deltaTime);

        Vector3 step = toTarget.normalized * _currentSpeed * Time.deltaTime;
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

    private float ComputeDesiredSpeed(int targetIndex)
    {
        float baseSpeed = Mathf.Clamp(speed, minSpeed, maxSpeed);
        int current = GetWrappedIndex(targetIndex - (moveDirection >= 0 ? 1 : -1));
        int next = GetWrappedIndex(targetIndex + (moveDirection >= 0 ? 1 : -1));
        int next2 = GetWrappedIndex(next + (moveDirection >= 0 ? 1 : -1));

        Vector3 a = waypoints[current];
        Vector3 b = waypoints[next];
        Vector3 c = waypoints[next2];

        Vector3 ab = (b - a);
        Vector3 bc = (c - b);
        ab.y = 0f;
        bc.y = 0f;

        float turnAngle = 0f;
        if (ab.sqrMagnitude > 0.01f && bc.sqrMagnitude > 0.01f)
        {
            turnAngle = Vector3.Angle(ab.normalized, bc.normalized);
        }

        float curveFactor = 1f - Mathf.Clamp01(turnAngle / 80f) * curveSlowdownStrength;

        float slope = 0f;
        float dist = Vector3.Distance(waypoints[current], waypoints[next]);
        if (dist > 0.5f)
        {
            slope = (waypoints[next].y - waypoints[current].y) / dist;
        }

        float uphill = Mathf.Clamp01(slope / 0.12f);
        float downhill = Mathf.Clamp01((-slope) / 0.16f);
        float slopeFactor = 1f - uphill * uphillSlowdownStrength - downhill * downhillSlowdownStrength;
        slopeFactor = Mathf.Clamp(slopeFactor, 0.55f, 1f);

        return Mathf.Clamp(baseSpeed * curveFactor * slopeFactor, minSpeed, maxSpeed);
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
