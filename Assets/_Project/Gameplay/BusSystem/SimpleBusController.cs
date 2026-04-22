using UnityEngine;

public class SimpleBusController : MonoBehaviour
{
    public float speed = 28f;
    public float reverseSpeed = 14f;
    public float maxForwardSpeed = 120f / 3.6f;
    public float maxReverseSpeed = 12f;
    public float hardMaxSpeedKmh = 120f;
    public float overSpeedBrakeKmh = 112f;
    public bool useWheelColliderDrive = false;
    public float rotationSpeed = 20f;
    public float brakingForce = 10f;
    public float coastDrag = 0.985f;
    public float wheelMotorTorque = 8600f;
    public float wheelBrakeTorque = 5200f;
    public float idleBrakeTorque = 980f;
    public float wheelSteerAngle = 8.8f;
    public float highSpeedWheelSteerAngle = 3.3f;
    public float straightLineAssist = 8f;
    public float lateralGrip = 9.5f;
    public float steeringResponse = 8.4f;
    public float steeringReturnSpeed = 11.4f;
    public float highSpeedSteeringDamping = 4.2f;
    public float yawStability = 3.4f;
    public float lowSpeedSteeringAssist = 3.2f;
    public float lowSpeedSteeringAssistLimit = 14f;
    public float hillAssistTorque = 2600f;
    public float hillAssistSpeedThreshold = 24f;
    public float hillMaxSpeedKmh = 55f;
    [Header("Ground Stability")]
    public float groundCheckDistance = 1.5f;
    public float downforceStrength = 5000f;
    public float driveForce = 1200f;
    public string groundLayerName = "Ground";
    public int currentGear = 0;
    public string gearText = "N";
    public float CurrentSpeedKmh => Mathf.Abs(currentForwardSpeed) * 3.6f;

    private Rigidbody rb;
    private WheelCollider[] wheelColliders;
    private float throttle;
    private float steering;
    private float steerInput;
    private float currentForwardSpeed;
    private float currentYaw;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Collider busCollider;
    private int groundMask;
    private bool isGrounded;
    private bool lastGroundedState;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.mass = 3500f;
        rb.drag = 0f;
        rb.angularDrag = 0.05f;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.maxAngularVelocity = 1.5f;
        maxForwardSpeed = 120f / 3.6f;
        hardMaxSpeedKmh = 120f;
        overSpeedBrakeKmh = 112f;

        busCollider = GetComponent<Collider>();
        if (busCollider == null)
        {
            Debug.LogWarning("SimpleBusController: Bus collider missing. BusSpawner should add one before gameplay starts.");
        }

        Transform centerOfMass = transform.Find("CenterOfMass");
        if (centerOfMass != null)
        {
            rb.centerOfMass = centerOfMass.localPosition;
        }
        else
        {
            rb.centerOfMass = new Vector3(0f, -1f, 0f);
        }
        wheelColliders = GetComponentsInChildren<WheelCollider>();
        groundMask = LayerMask.GetMask(groundLayerName);
        if (groundMask == 0)
        {
            groundMask = Physics.DefaultRaycastLayers;
        }
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        currentYaw = transform.eulerAngles.y;

        foreach (WheelCollider wheel in wheelColliders)
        {
            wheel.ConfigureVehicleSubsteps(8f, 12, 15);
            wheel.enabled = useWheelColliderDrive;
        }
    }

    private void Update()
    {
        throttle = 0f;
        steerInput = 0f;

        if (IsKeyHeld(KeyCode.W) || IsKeyHeld(KeyCode.UpArrow)) throttle = 1f;
        if (IsKeyHeld(KeyCode.S) || IsKeyHeld(KeyCode.DownArrow)) throttle = -1f;
        if (IsKeyHeld(KeyCode.A) || IsKeyHeld(KeyCode.LeftArrow)) steerInput = -1f;
        if (IsKeyHeld(KeyCode.D) || IsKeyHeld(KeyCode.RightArrow)) steerInput = 1f;

        if (IsKeyDown(KeyCode.R))
        {
            ReloadBus();
        }
    }

    private void FixedUpdate()
    {
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        UpdateGroundedState();
        DriveArcade();
        UpdateGear();
    }

    private void DriveArcade()
    {
        float speedLimit = hardMaxSpeedKmh / 3.6f;
        float desiredForwardSpeed = 0f;
        float acceleration = Mathf.Lerp(3.5f, 8.5f, Mathf.InverseLerp(0f, 5f, currentGear));

        if (Input.GetKey(KeyCode.Space))
        {
            desiredForwardSpeed = 0f;
            acceleration = brakingForce * 2.2f;
        }
        else if (throttle > 0f)
        {
            desiredForwardSpeed = GetGearSpeedCap(currentGear);
            acceleration = Mathf.Max(acceleration, 2.5f * GetHillAssistMultiplier());
        }
        else if (throttle < 0f)
        {
            desiredForwardSpeed = -maxReverseSpeed;
            acceleration = Mathf.Max(2.5f, reverseSpeed * 0.3f);
        }

        currentForwardSpeed = Mathf.MoveTowards(
            currentForwardSpeed,
            desiredForwardSpeed,
            acceleration * Time.fixedDeltaTime
        );

        currentForwardSpeed = Mathf.Clamp(currentForwardSpeed, -speedLimit, speedLimit);
        if (throttle == 0f && !Input.GetKey(KeyCode.Space))
        {
            currentForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, 0f, brakingForce * 0.5f * Time.fixedDeltaTime);
        }

        float speedRatio = Mathf.Clamp01(Mathf.Abs(currentForwardSpeed) / maxForwardSpeed);
        float steerMultiplier = Mathf.Lerp(1f, 0.30f, speedRatio);
        float turn = steerInput * rotationSpeed * steerMultiplier * Time.fixedDeltaTime;
        currentYaw += turn;

        Quaternion targetRotation = Quaternion.Euler(0f, currentYaw, 0f);
        rb.MoveRotation(targetRotation);

        Vector3 move = targetRotation * Vector3.forward * currentForwardSpeed;
        rb.velocity = new Vector3(move.x, Mathf.Min(rb.velocity.y, 0f), move.z);
        rb.angularVelocity = Vector3.zero;

        if (isGrounded)
        {
            if (Mathf.Abs(currentForwardSpeed) > 0.1f)
            {
                rb.AddForce(Vector3.down * downforceStrength, ForceMode.Force);
            }
        }

        ClampVerticalVelocity();
    }

    private float GetGearSpeedCap(int gear)
    {
        switch (gear)
        {
            case -1:
                return maxReverseSpeed;
            case 0:
            case 1:
                return 25f / 3.6f;
            case 2:
                return 45f / 3.6f;
            case 3:
                return 65f / 3.6f;
            case 4:
                return 90f / 3.6f;
            default:
                return maxForwardSpeed;
        }
    }

    private void DriveWithWheelColliders()
    {
        float speedRatio = Mathf.Clamp01(rb.velocity.magnitude / maxForwardSpeed);
        float steerAngle = Mathf.Lerp(wheelSteerAngle, highSpeedWheelSteerAngle, speedRatio);
        float brakeTorque = Input.GetKey(KeyCode.Space) ? wheelBrakeTorque : 0f;
        float motorTorque = brakeTorque > 0f ? 0f : throttle * wheelMotorTorque;
        float speedKmh = rb.velocity.magnitude * 3.6f;

        if (brakeTorque <= 0f && Mathf.Abs(throttle) < 0.01f && speedKmh < 1.5f)
        {
            // Prevent slow creeping/rolling when the player is not giving input.
            brakeTorque = idleBrakeTorque;
        }

        if (throttle > 0f && speedKmh < 25f)
        {
            motorTorque *= 1.35f;
        }
        else if (throttle > 0f && speedKmh < 60f)
        {
            motorTorque *= 1.18f;
        }

        motorTorque *= GetHillAssistMultiplier();

        for (int i = 0; i < wheelColliders.Length; i++)
        {
            WheelCollider wheel = wheelColliders[i];
            wheel.brakeTorque = brakeTorque;
            wheel.motorTorque = i >= 2 ? motorTorque : motorTorque * 0.15f;
            wheel.steerAngle = i < 2 ? steering * steerAngle : 0f;
        }

        ApplySmoothSpeedLimit();
        ApplyCoasting();
        ApplyWheelStability(speedRatio);
        ApplyHighSpeedProtection();
        ClampVerticalVelocity();
    }

    private void ApplySmoothSpeedLimit()
    {
        float maxForward = Mathf.Min(maxForwardSpeed, hardMaxSpeedKmh / 3.6f);
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
        forwardSpeed = Mathf.Clamp(forwardSpeed, -maxReverseSpeed, maxForward);
        rb.velocity = transform.forward * forwardSpeed + Vector3.up * rb.velocity.y;
        ClampVerticalVelocity();
    }

    private void ApplyCoasting()
    {
        if (Mathf.Abs(throttle) > 0.01f || Input.GetKey(KeyCode.Space))
        {
            return;
        }

        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
        forwardSpeed *= coastDrag;
        rb.velocity = transform.forward * forwardSpeed + Vector3.up * rb.velocity.y;
        ClampVerticalVelocity();
    }

    private void ApplyDirectionalStability()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
        float speed = Mathf.Abs(localVelocity.z);

        if (speed < 0.5f)
        {
            return;
        }

        localVelocity.x = Mathf.Lerp(localVelocity.x, 0f, lateralGrip * Time.fixedDeltaTime);
        rb.velocity = transform.TransformDirection(localVelocity);
        ClampVerticalVelocity();

        if (Mathf.Abs(steerInput) < 0.01f)
        {
            rb.angularVelocity = Vector3.Lerp(
                rb.angularVelocity,
                new Vector3(0f, 0f, 0f),
                straightLineAssist * Time.fixedDeltaTime
            );
        }
    }

    private void ApplyWheelStability(float speedRatio)
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);

        if (Mathf.Abs(steerInput) < 0.1f)
        {
            localVelocity.x = Mathf.Lerp(localVelocity.x, 0f, lateralGrip * Time.fixedDeltaTime);
        }
        else
        {
            float damping = Mathf.Lerp(0.5f, highSpeedSteeringDamping, speedRatio);
            localVelocity.x = Mathf.Lerp(localVelocity.x, 0f, damping * Time.fixedDeltaTime);
        }

        rb.velocity = transform.TransformDirection(localVelocity);
        ClampVerticalVelocity();

        Vector3 angularVelocity = rb.angularVelocity;

        if (Mathf.Abs(steerInput) < 0.05f)
        {
            angularVelocity.y = Mathf.Lerp(angularVelocity.y, 0f, straightLineAssist * Time.fixedDeltaTime);
        }
        else if (speedRatio > 0.45f)
        {
            float maxYawRate = Mathf.Lerp(1.2f, 0.45f, speedRatio);
            angularVelocity.y = Mathf.Clamp(angularVelocity.y, -maxYawRate, maxYawRate);
            angularVelocity.y = Mathf.Lerp(angularVelocity.y, angularVelocity.y * 0.92f, yawStability * Time.fixedDeltaTime);
        }

        rb.angularVelocity = new Vector3(0f, angularVelocity.y, 0f);
    }

    private void UpdateGroundedState()
    {
        Vector3 origin = busCollider != null ? new Vector3(busCollider.bounds.center.x, busCollider.bounds.min.y + 0.1f, busCollider.bounds.center.z) : transform.position;
        bool grounded = groundMask != 0 && Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
        Debug.DrawRay(origin, Vector3.down * groundCheckDistance, Color.red);

        isGrounded = grounded;
        if (isGrounded != lastGroundedState)
        {
            Debug.Log("Grounded: " + grounded);
            Debug.Log(isGrounded ? "Bus grounded" : "Bus airborne");
            lastGroundedState = isGrounded;
        }
    }

    private void ClampVerticalVelocity()
    {
        if (rb.velocity.y > 0f)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        }
    }

    private void ApplyLowSpeedSteeringAssist()
    {
        if (Mathf.Abs(steerInput) < 0.05f)
        {
            return;
        }

        float speedKmh = rb.velocity.magnitude * 3.6f;
        if (speedKmh > lowSpeedSteeringAssistLimit)
        {
            return;
        }

        float assist = Mathf.Lerp(lowSpeedSteeringAssist, 0f, Mathf.Clamp01(speedKmh / lowSpeedSteeringAssistLimit));
        rb.AddTorque(Vector3.up * steerInput * assist, ForceMode.Acceleration);
    }

    private void ApplyHighSpeedProtection()
    {
        float forwardSpeed = currentForwardSpeed;
        float speedKmh = Mathf.Abs(forwardSpeed) * 3.6f;
        if (speedKmh <= overSpeedBrakeKmh)
        {
            return;
        }

        if (speedKmh > hardMaxSpeedKmh)
        {
            currentForwardSpeed = Mathf.Sign(forwardSpeed) * (hardMaxSpeedKmh / 3.6f);
            return;
        }

        float overspeedRatio = Mathf.InverseLerp(overSpeedBrakeKmh, hardMaxSpeedKmh, speedKmh);
        float targetSpeed = Mathf.Sign(forwardSpeed) * (hardMaxSpeedKmh / 3.6f);
        currentForwardSpeed = Mathf.Lerp(currentForwardSpeed, targetSpeed, overspeedRatio * Time.fixedDeltaTime * 5f);
    }

    private float GetHillAssistMultiplier()
    {
        float uphillAlignment = Mathf.Max(0f, Vector3.Dot(transform.forward.normalized, Vector3.up));
        float speedKmh = rb != null ? rb.velocity.magnitude * 3.6f : 0f;

        if (throttle > 0f && speedKmh < hillAssistSpeedThreshold && uphillAlignment > 0.08f)
        {
            return 1f + hillAssistTorque / 10000f;
        }

        return 1f;
    }

    private void UpdateGear()
    {
        float forwardSpeed = currentForwardSpeed;
        float speedKmh = CurrentSpeedKmh;

        if (forwardSpeed < -0.8f)
        {
            currentGear = -1;
            gearText = "R";
        }
        else if (speedKmh < 1f)
        {
            currentGear = 0;
            gearText = "N";
        }
        else if (speedKmh < 20f)
        {
            currentGear = 1;
            gearText = "1";
        }
        else if (speedKmh < 40f)
        {
            currentGear = 2;
            gearText = "2";
        }
        else if (speedKmh < 65f)
        {
            currentGear = 3;
            gearText = "3";
        }
        else if (speedKmh < 90f)
        {
            currentGear = 4;
            gearText = "4";
        }
        else
        {
            currentGear = 5;
            gearText = "5";
        }
    }

    private void ReloadBus()
    {
        if (!rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        currentForwardSpeed = 0f;
        currentYaw = spawnRotation.eulerAngles.y;
        steering = 0f;
        throttle = 0f;
        steerInput = 0f;

        SimpleCameraFollow follow = Camera.main != null ? Camera.main.GetComponent<SimpleCameraFollow>() : null;
        if (follow != null)
        {
            follow.SnapAfterBusReset();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (rb == null || collision == null || collision.contactCount == 0)
        {
            return;
        }

        Debug.Log("Collision detected with: " + collision.gameObject.name);
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
        localVelocity.x = 0f;
        localVelocity.y = Mathf.Min(localVelocity.y, 0f);
        rb.velocity = transform.TransformDirection(localVelocity);
        rb.angularVelocity = Vector3.zero;
    }

    private static bool IsKeyHeld(KeyCode key)
    {
        if (Input.GetKey(key))
        {
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        return IsKeyHeldInputSystem(key);
#else
        return false;
#endif
    }

    private static bool IsKeyDown(KeyCode key)
    {
        if (Input.GetKeyDown(key))
        {
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        return IsKeyDownInputSystem(key);
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static bool IsKeyHeldInputSystem(KeyCode key)
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        switch (key)
        {
            case KeyCode.W: return keyboard.wKey.isPressed;
            case KeyCode.A: return keyboard.aKey.isPressed;
            case KeyCode.S: return keyboard.sKey.isPressed;
            case KeyCode.D: return keyboard.dKey.isPressed;
            case KeyCode.UpArrow: return keyboard.upArrowKey.isPressed;
            case KeyCode.DownArrow: return keyboard.downArrowKey.isPressed;
            case KeyCode.LeftArrow: return keyboard.leftArrowKey.isPressed;
            case KeyCode.RightArrow: return keyboard.rightArrowKey.isPressed;
            case KeyCode.Space: return keyboard.spaceKey.isPressed;
            case KeyCode.R: return keyboard.rKey.isPressed;
            default: return false;
        }
    }

    private static bool IsKeyDownInputSystem(KeyCode key)
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        switch (key)
        {
            case KeyCode.W: return keyboard.wKey.wasPressedThisFrame;
            case KeyCode.A: return keyboard.aKey.wasPressedThisFrame;
            case KeyCode.S: return keyboard.sKey.wasPressedThisFrame;
            case KeyCode.D: return keyboard.dKey.wasPressedThisFrame;
            case KeyCode.UpArrow: return keyboard.upArrowKey.wasPressedThisFrame;
            case KeyCode.DownArrow: return keyboard.downArrowKey.wasPressedThisFrame;
            case KeyCode.LeftArrow: return keyboard.leftArrowKey.wasPressedThisFrame;
            case KeyCode.RightArrow: return keyboard.rightArrowKey.wasPressedThisFrame;
            case KeyCode.Space: return keyboard.spaceKey.wasPressedThisFrame;
            case KeyCode.R: return keyboard.rKey.wasPressedThisFrame;
            default: return false;
        }
    }
#endif
}
