using UnityEngine;
using System.Collections.Generic;

// Indian Bus Simulator — v1.0
// Unity Version: 2022.3.62f3 (LTS)
// Ref: LDD Section 5.2 (Bus Stop Components) & GDD Section 6.3
// Handles passenger spawning and boarding logic.

public class IndianBusStop : MonoBehaviour
{
    [Header("Ref: LDD 5.3 (Stop Name Format)")]
    public string stopName = "Lajpat Nagar | लाजपत नगर";
    
    [Header("Passenger Stats")]
    public int waitingPassengersCount;
    public Transform[] spawnPoints;
    public GameObject passengerPrefab;

    [Header("Stopping Accuracy (Ref: GDD 6.3)")]
    public Transform platformCenter;
    public float perfectStopThreshold = 2.0f;

    void Start() {
        waitingPassengersCount = Random.Range(5, 25);
        SpawnPassengers();
    }

    void SpawnPassengers() {
        for (int i = 0; i < waitingPassengersCount; i++) {
            Vector3 randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
            Instantiate(passengerPrefab, randomPoint, Quaternion.identity, transform);
        }
    }

    // Called when player opens bus doors
    public void StartBoarding(Vector3 busPosition) {
        float distance = Vector3.Distance(busPosition, platformCenter.position);
        
        if (distance <= perfectStopThreshold) {
            Debug.Log("PERFECT STOP! Passenger satisfaction +10%");
            // Logic to move passengers into bus...
        } else {
            Debug.Log("Poor stop alignment. Boarding delayed.");
        }
    }
}
