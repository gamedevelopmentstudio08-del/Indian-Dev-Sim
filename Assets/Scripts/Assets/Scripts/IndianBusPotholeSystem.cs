using UnityEngine;

// Indian Bus Simulator — v1.0
// Document ID: IBS-PHYS-001 | Section 4.1
// Purpose: Simulates realistic suspension response when wheels encounter potholes on Indian roads.

public class IndianBusPotholeSystem : MonoBehaviour
{
    [Header("Ref: IBS-PHYS-001 Page 11")]
    public float bumpAmplitude = 1.5f; // Vertical displacement 
    public float springRate = 34000f; // From Rear Wheel Config
    public float maxJoltForce = 50000f;

    [Header("Detection Settings")]
    public LayerMask potholeLayer;
    public float detectionRadius = 0.5f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = GetComponentInParent<Rigidbody>();
    }

    // Called via Unity Physics triggers or custom raycast detection
    public void OnPotholeImpact(float depth, float speedKph)
    {
        // Physics logic from Page 12 of IBS-PHYS-001
        float impactSpeed = speedKph / 3.6f; // Convert to m/s
        float compressionForce = depth * springRate * impactSpeed;

        // Apply upward force to simulate the jolt
        Vector3 force = transform.up * compressionForce;
        rb.AddForceAtPosition(force, transform.position, ForceMode.Impulse);

        // Calculate jolt intensity for animations/Haptics (0.0 to 1.0)
        float joltIntensity = Mathf.Clamp01(compressionForce / maxJoltForce);
        
        TriggerFeedback(joltIntensity);
    }

    private void TriggerFeedback(float intensity)
    {
        // Integrated with Camera Shake (Art Bible 7.1)
        Debug.Log($"Pothole Impact: {intensity * 100}% Intensity. Triggering Camera Shake.");
        
        if (intensity > 0.6f)
        {
            // Satisfaction penalty logic (Ref: GDD 6.2)
            Debug.Log("Hard jolt! Passenger satisfaction reduced.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
