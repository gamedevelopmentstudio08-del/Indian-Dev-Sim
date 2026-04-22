using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    public enum CameraView
    {
        Chase = 0,
        Driver = 1
    }

    public Transform target;
    public Transform driverSeat;
    public CameraView currentView = CameraView.Chase;
    public Vector3 offset = new Vector3(0f, 3.5f, -8f);
    public Vector3 lookOffset = new Vector3(0f, 1.8f, 4.5f);
    public float smoothness = 5f;
    public bool rotateWithBus = true;
    public float perspectiveFieldOfView = 60f;
    public float driverFieldOfView = 66f;
    public KeyCode cycleViewKey = KeyCode.C;
    public KeyCode chaseViewKey = KeyCode.Alpha1;
    public KeyCode driverViewKey = KeyCode.Alpha2;

    private bool hasSnappedToTarget;
    private bool skipSmoothingThisFrame;
    private Camera cameraComponent;

    private void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        ApplyViewSettings(currentView);
    }

    private void Update()
    {
        if (IsKeyDown(cycleViewKey))
        {
            SetView((int)currentView + 1);
        }
        else if (IsKeyDown(chaseViewKey))
        {
            SetView((int)CameraView.Chase);
        }
        else if (IsKeyDown(driverViewKey))
        {
            SetView((int)CameraView.Driver);
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            target = ResolveTarget();
            if (target == null)
            {
                return;
            }
        }

        if (skipSmoothingThisFrame)
        {
            skipSmoothingThisFrame = false;
            SnapToTarget();
            return;
        }

        Vector3 targetPosition = GetTargetPosition();
        Vector3 lookPoint = GetLookPoint();

        if (!hasSnappedToTarget)
        {
            transform.position = targetPosition;
            hasSnappedToTarget = true;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothness * Time.deltaTime);
        }

        Vector3 lookDirection = lookPoint - transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookDirection, Vector3.up),
                smoothness * Time.deltaTime);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        hasSnappedToTarget = false;
        skipSmoothingThisFrame = true;
    }

    public void SetView(int viewIndex)
    {
        int normalizedIndex = ((viewIndex % 2) + 2) % 2;
        currentView = (CameraView)normalizedIndex;
        ApplyViewSettings(currentView);
        hasSnappedToTarget = false;
        SnapToTarget();
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = GetTargetPosition();
        Vector3 lookPoint = GetLookPoint();
        transform.position = targetPosition;
        Vector3 lookDirection = lookPoint - transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }

        hasSnappedToTarget = true;
    }

    private Vector3 GetTargetPosition()
    {
        if (currentView == CameraView.Driver && driverSeat != null)
        {
            return driverSeat.position;
        }

        return rotateWithBus ? target.position + target.rotation * offset : target.position + offset;
    }

    private Vector3 GetLookPoint()
    {
        if (currentView == CameraView.Driver && driverSeat != null)
        {
            return driverSeat.position + driverSeat.forward * 8f;
        }

        return rotateWithBus ? target.position + target.rotation * lookOffset : target.position + lookOffset;
    }

    private void ApplyViewSettings(CameraView view)
    {
        switch (view)
        {
            case CameraView.Driver:
                offset = new Vector3(0f, 2.15f, 1.9f);
                lookOffset = new Vector3(0f, 2.1f, 8.5f);
                rotateWithBus = true;
                SetFieldOfView(driverFieldOfView);
                break;
            case CameraView.Chase:
            default:
                offset = new Vector3(0f, 3.5f, -8f);
                lookOffset = new Vector3(0f, 1.8f, 4.5f);
                rotateWithBus = true;
                SetFieldOfView(perspectiveFieldOfView);
                break;
        }
    }

    public void SnapAfterBusReset()
    {
        hasSnappedToTarget = false;
        SnapToTarget();
        skipSmoothingThisFrame = true;
    }

    private Transform ResolveTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.transform;
        }

        SimpleBusController busController = FindObjectOfType<SimpleBusController>();
        return busController != null ? busController.transform : null;
    }

    private void SetFieldOfView(float value)
    {
        if (cameraComponent != null)
        {
            cameraComponent.fieldOfView = value;
        }
    }

    private static bool IsKeyDown(KeyCode key)
    {
        if (Input.GetKeyDown(key))
        {
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        switch (key)
        {
            case KeyCode.C: return keyboard.cKey.wasPressedThisFrame;
            case KeyCode.Alpha1: return keyboard.digit1Key.wasPressedThisFrame;
            case KeyCode.Alpha2: return keyboard.digit2Key.wasPressedThisFrame;
            case KeyCode.Alpha3: return keyboard.digit3Key.wasPressedThisFrame;
            case KeyCode.Alpha4: return keyboard.digit4Key.wasPressedThisFrame;
            default: return false;
        }
#else
        return false;
#endif
    }
}
