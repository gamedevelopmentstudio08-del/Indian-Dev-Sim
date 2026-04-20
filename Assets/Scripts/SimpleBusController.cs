using UnityEngine;

public class SimpleBusController : MonoBehaviour
{
    public float speed = 28f;
    public float reverseSpeed = 14f;
    public float maxForwardSpeed = 90f / 3.6f;
    public float maxReverseSpeed = 8f;
    public float rotationSpeed = 18f;
    public float brakingForce = 10f;
    public float coastDrag = 0.985f;
    public float wheelMotorTorque = 8200f;
    public float wheelBrakeTorque = 5200f;
    public float wheelSteerAngle = 6.5f;
    public float highSpeedWheelSteerAngle = 2.2f;
    public float straightLineAssist = 8f;
    public float lateralGrip = 9f;
    public float steeringResponse = 6.5f;
    public float steeringReturnSpeed = 9f;
    public float highSpeedSteeringDamping = 4.5f;
    public float yawStability = 3f;
    public int currentGear = 0;
    public string gearText = "N";

    private Rigidbody rb;
    private WheelCollider[] wheelColliders;
    private float throttle;
    private float steering;
    private float steerInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 2200f;
        rb.drag = 0.22f;
        rb.angularDrag = 4.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.centerOfMass = new Vector3(0f, -1.05f, 0f);
        rb.maxAngularVelocity = 2.5f;
        wheelColliders = GetComponentsInChildren<WheelCollider>();

        foreach (WheelCollider wheel in wheelColliders)
        {
            wheel.ConfigureVehicleSubsteps(8f, 12, 15);
        }
    }

    private void Update()
    {
        throttle = 0f;
        steerInput = 0f;

        if (Input.GetKey(KeyCode.W)) throttle = 1f;
        if (Input.GetKey(KeyCode.S)) throttle = -1f;
        if (Input.GetKey(KeyCode.A)) steerInput = -1f;
        if (Input.GetKey(KeyCode.D)) steerInput = 1f;

        if (Input.GetKeyDown(KeyCode.R))
        {
            ReloadBus();
        }
    }

    private void FixedUpdate()
    {
        float steerRate = Mathf.Abs(steerInput) > 0.01f ? steeringResponse : steeringReturnSpeed;
        steering = Mathf.MoveTowards(steering, steerInput, steerRate * Time.fixedDeltaTime);

        if (wheelColliders != null && wheelColliders.Length >= 4)
        {
            DriveWithWheelColliders();
            UpdateGear();
            return;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, brakingForce * Time.fixedDeltaTime);
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, brakingForce * Time.fixedDeltaTime);
            UpdateGear();
            return;
        }

        if (throttle > 0f)
        {
            rb.AddForce(transform.forward * speed, ForceMode.Acceleration);
        }
        else if (throttle < 0f)
        {
            rb.AddForce(-transform.forward * reverseSpeed, ForceMode.Acceleration);
        }

        ApplySmoothSpeedLimit();
        ApplyCoasting();
        ApplyDirectionalStability();

        float movingAmount = Mathf.Clamp01(rb.velocity.magnitude / 2f);
        float turn = steering * rotationSpeed * movingAmount * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));
        UpdateGear();
    }

    private void DriveWithWheelColliders()
    {
        float speedRatio = Mathf.Clamp01(rb.velocity.magnitude / maxForwardSpeed);
        float steerAngle = Mathf.Lerp(wheelSteerAngle, highSpeedWheelSteerAngle, speedRatio);
        float brakeTorque = Input.GetKey(KeyCode.Space) ? wheelBrakeTorque : 0f;
        float motorTorque = brakeTorque > 0f ? 0f : throttle * wheelMotorTorque;
        float speedKmh = rb.velocity.magnitude * 3.6f;

        if (throttle > 0f && speedKmh < 25f)
        {
            motorTorque *= 1.35f;
        }
        else if (throttle > 0f && speedKmh < 60f)
        {
            motorTorque *= 1.18f;
        }

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
    }

    private void ApplySmoothSpeedLimit()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);

        if (localVelocity.z > maxForwardSpeed)
        {
            localVelocity.z = Mathf.Lerp(localVelocity.z, maxForwardSpeed, 4f * Time.fixedDeltaTime);
        }
        else if (localVelocity.z < -maxReverseSpeed)
        {
            localVelocity.z = Mathf.Lerp(localVelocity.z, -maxReverseSpeed, 4f * Time.fixedDeltaTime);
        }

        rb.velocity = transform.TransformDirection(localVelocity);
    }

    private void ApplyCoasting()
    {
        if (Mathf.Abs(throttle) > 0.01f)
        {
            return;
        }

        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
        localVelocity.z *= coastDrag;
        rb.velocity = transform.TransformDirection(localVelocity);
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
        transform.position = new Vector3(0f, 1.2f, -430f);
        transform.rotation = Quaternion.identity;
        steering = 0f;
        throttle = 0f;
        steerInput = 0f;
    }
}
