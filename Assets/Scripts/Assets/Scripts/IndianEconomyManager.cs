using UnityEngine;
using System.Collections;

// Indian Bus Simulator — v1.0
// Unity Version: 2022.3.62f3 (LTS)
// Ref: GDD Section 8 (Economy) & SRS Section 6.1
// Handles player progression, currency calculation, and shift earnings.

public class IndianEconomyManager : MonoBehaviour
{
    public static IndianEconomyManager Instance;

    [Header("Player Stats")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public long rupees = 500; // Local currency (Ref: GDD 8.1)
    public int goldTokens = 10; // Premium currency

    [Header("Shift Data")]
    public int passengersDelivered = 0;
    public int trafficFines = 0;
    public float satisfactionScore = 100f;

    void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    // Logic from GDD Page 9 (Income Sources)
    public void CompleteShift(bool onTime) {
        int baseRupees = passengersDelivered * Random.Range(10, 51);
        int bonus = onTime ? Random.Range(200, 501) : 0;
        
        int totalEarnings = (baseRupees + bonus) - trafficFines;
        rupees += totalEarnings;

        AddXP(50 + (onTime ? 25 : 0));
        
        Debug.Log($"Shift Complete! Earned: ₹{totalEarnings}. Total Rupees: ₹{rupees}");
        
        // Reset shift stats for next run
        ResetShiftStats();
    }

    public void AddFine(int amount, string reason) {
        trafficFines += amount;
        satisfactionScore -= 5f;
        Debug.LogWarning($"FINE ISSUED: ₹{amount} for {reason}");
    }

    private void AddXP(int amount) {
        currentXP += amount;
        if (currentXP >= GetXPForNextLevel()) {
            LevelUp();
        }
    }

    private int GetXPForNextLevel() {
        return currentLevel * 1000; // Simplified progression
    }

    private void LevelUp() {
        currentLevel++;
        currentXP = 0;
        goldTokens += 5; // Level up reward
        Debug.Log($"LEVEL UP! You are now Level {currentLevel}");
    }

    private void ResetShiftStats() {
        passengersDelivered = 0;
        trafficFines = 0;
        satisfactionScore = 100f;
    }
}
