using UnityEngine;
using System.Collections.Generic;

// Indian Bus Simulator — v1.0 (Unity)
// Purpose: Realistic handling based on IBS-PHYS-001 (Category B: City Bus)
// Mass: 12,500kg | Torque: 750Nm

[RequireComponent(typeof(Rigidbody))]
public class IndianBusController : MonoBehaviour
{
    [Header("Bus Physics Profile")]
    public float busMass = 12500f; 
    public float maxTorque = 750f; 
    public float brakeTorque = 18000f; 
    public float maxSteeringAngle = 32f;
    
    [Header("Transmission (6-Speed manual)")]
    public float[] gearRatios = { 6.5f, 3.8f, 2.4f, 1.6f, 1.0f, 0.78f };
    public float finalDriveRatio = 5.3f;
    public int currentGear = 0;

    [Header("Wheels")]
    public WheelCollider[] frontWheels;
    public WheelCollider[] rearWheels;

    private Rigidbody rb;
    private float verticalInput;
    private float horizontalInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = busMass;
        // Higher Center of Mass for Body Roll (LDD Ref)
        rb.centerOfMass = new Vector3(0, 0.35f, 0); 
    }

    void Update()
    {
        verticalInput = Input.GetAxis("Vertical");
        horizontalInput = Input.GetAxis("Horizontal");

        if (Input.GetKeyDown(KeyCode.E) && currentGear < gearRatios.Length - 1) currentGear++;
        if (Input.GetKeyDown(KeyCode.Q) && currentGear > 0) currentGear--;
    }

    void FixedUpdate()
    {
        ApplySteering();
        ApplyDrive();
    }

    private void ApplySteering()
    {
        float speedFactor = rb.velocity.magnitude * 3.6f / 50f; 
        float steeringAngle = horizontalInput * Mathf.Lerp(maxSteeringAngle, maxSteeringAngle * 0.4f, speedFactor);
        
        foreach (var wheel in frontWheels) wheel.steerAngle = steeringAngle;
    }

    private void ApplyDrive()
    {
        float motorForce = verticalInput * (maxTorque * gearRatios[currentGear] * finalDriveRatio);

        foreach (var wheel in rearWheels)
        {
            if (verticalInput >= 0)
            {
                wheel.motorTorque = motorForce;
                wheel.brakeTorque = 0;
            }
            else
            {
                wheel.motorTorque = 0;
                wheel.brakeTorque = brakeTorque; 
            }
        }
    }
}
