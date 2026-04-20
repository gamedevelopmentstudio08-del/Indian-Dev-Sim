using UnityEngine;
using UnityEngine.AI;

// Indian Bus Simulator — v1.0 (Unity)
// Purpose: Simulates chaotic Indian traffic behavior including lane-splitting and honking.

[RequireComponent(typeof(NavMeshAgent))]
public class IndianTrafficAI : MonoBehaviour
{
    [Header("Behavior Settings")]
    public float detectionRange = 10f;
    public bool canLaneSplit = true; 
    public float hornProbability = 0.015f; 

    private NavMeshAgent agent;
    private LayerMask obstacleLayer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        obstacleLayer = LayerMask.GetMask("Vehicle", "Player");
        SetRandomDestination();
    }

    void Update()
    {
        DetectObstacles();
        
        if (Random.value < hornProbability) 
        {
            Debug.Log(gameObject.name + " honked in traffic chaos!");
        }

        if (agent.remainingDistance < 1.5f) SetRandomDestination();
    }

    void DetectObstacles()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out hit, detectionRange, obstacleLayer))
        {
            agent.isStopped = true;
            if (canLaneSplit) TryOvertake();
        }
        else
        {
            agent.isStopped = false;
        }
    }

    void TryOvertake()
    {
        // Indian road behavior: Try to find a gap on the side
        Vector3 sideOffset = transform.right * (Random.value > 0.5f ? 2.5f : -2.5f);
        agent.SetDestination(transform.position + sideOffset + transform.forward * 5f);
    }

    void SetRandomDestination()
    {
        // Placeholder for Map Node logic
    }
}
