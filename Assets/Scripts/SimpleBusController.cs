using UnityEngine;

public class SimpleBusController : MonoBehaviour
{
    public float speed = 28f;
    public float reverseSpeed = 14f;
    public float maxForwardSpeed = 95f / 3.6f;
    public float maxReverseSpeed = 8f;
    public float hardMaxSpeedKmh = 200f;
    public float overSpeedBrakeKmh = 185f;
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
    public int currentGear = 0;
    public string gearText = "N";

    private Rigidbody rb;
    private WheelCollider[] wheelColliders;
    private float throttle;
    private float steering;
    private float steerInput;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 7600f;
        rb.drag = 0.12f;
        rb.angularDrag = 2.1f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.centerOfMass = new Vector3(0f, -1.25f, 0f);
        rb.maxAngularVelocity = 2.2f;
        wheelColliders = GetComponentsInChildren<WheelCollider>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

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
        if (useWheelColliderDrive && wheelColliders != null && wheelColliders.Length >= 4)
        {
            float steerRate = Mathf.Abs(steerInput) > 0.01f ? steeringResponse : steeringReturnSpeed;
            steering = Mathf.MoveTowards(steering, steerInput, steerRate * Time.fixedDeltaTime);
            DriveWithWheelColliders();
            ApplyLowSpeedSteeringAssist();
            ApplyHighSpeedProtection();
            UpdateGear();
            return;
        }

        DriveArcade();
        UpdateGear();
    }

    private void DriveArcade()
    {
        float speedLimit = hardMaxSpeedKmh / 3.6f;
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
        float desiredForwardSpeed = 0f;
        float acceleration = brakingForce;

        if (Input.GetKey(KeyCode.Space))
        {
            desiredForwardSpeed = 0f;
            acceleration = brakingForce * 3f;
        }
        else if (throttle > 0f)
        {
            desiredForwardSpeed = maxForwardSpeed;
            acceleration = speed * GetHillAssistMultiplier();
        }
        else if (throttle < 0f)
        {
            desiredForwardSpeed = -maxReverseSpeed;
            acceleration = reverseSpeed;
        }

        localVelocity.z = Mathf.MoveTowards(
            localVelocity.z,
            desiredForwardSpeed,
            acceleration * Time.fixedDeltaTime
        );
        localVelocity.z = Mathf.Clamp(localVelocity.z, -speedLimit, speedLimit);
        localVelocity.x = Mathf.Lerp(localVelocity.x, 0f, 12f * Time.fixedDeltaTime);
        rb.velocity = transform.TransformDirection(localVelocity);

        float speedRatio = Mathf.Clamp01(Mathf.Abs(localVelocity.z) / maxForwardSpeed);
        float steerMultiplier = Mathf.Lerp(1f, 0.35f, speedRatio);
        float turn = steerInput * rotationSpeed * steerMultiplier * Time.fixedDeltaTime;

        if (Mathf.Abs(steerInput) > 0.01f)
        {
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));
        }

        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, 14f * Time.fixedDeltaTime);
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
    }

    private void ApplySmoothSpeedLimit()
    {
        float maxForward = Mathf.Min(maxForwardSpeed, hardMaxSpeedKmh / 3.6f);
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
        forwardSpeed = Mathf.Clamp(forwardSpeed, -maxReverseSpeed, maxForward);
        rb.velocity = transform.forward * forwardSpeed + Vector3.up * rb.velocity.y;
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
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
        float speedKmh = Mathf.Abs(forwardSpeed) * 3.6f;
        if (speedKmh <= overSpeedBrakeKmh)
        {
            return;
        }

        if (speedKmh > hardMaxSpeedKmh)
        {
            rb.velocity = transform.forward * Mathf.Sign(forwardSpeed) * (hardMaxSpeedKmh / 3.6f);
            rb.angularVelocity *= 0.45f;
            return;
        }

        float overspeedRatio = Mathf.InverseLerp(overSpeedBrakeKmh, hardMaxSpeedKmh, speedKmh);
        Vector3 targetVelocity = transform.forward * Mathf.Sign(forwardSpeed) * (hardMaxSpeedKmh / 3.6f);
        rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, overspeedRatio * Time.fixedDeltaTime * 5f);
        rb.angularVelocity *= Mathf.Lerp(1f, 0.72f, overspeedRatio);
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
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
        float speedKmh = rb.velocity.magnitude * 3.6f;

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
        else if (speedKmh < 35f)
        {
            currentGear = 2;
            gearText = "2";
        }
        else if (speedKmh < 50f)
        {
            currentGear = 3;
            gearText = "3";
        }
        else if (speedKmh < 70f)
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
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        steering = 0f;
        throttle = 0f;
        steerInput = 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (rb == null || collision == null || collision.contactCount == 0)
        {
            return;
        }

        Vector3 impulseDirection = -collision.contacts[0].normal;
        float impactStrength = Mathf.Clamp(collision.relativeVelocity.magnitude, 0f, 20f);
        if (impactStrength < 1.2f)
        {
            return;
        }

        rb.velocity *= 0.74f;
        rb.angularVelocity *= 0.65f;
        if (useWheelColliderDrive)
        {
            rb.AddForce(impulseDirection * impactStrength * 1.4f, ForceMode.VelocityChange);
        }
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
