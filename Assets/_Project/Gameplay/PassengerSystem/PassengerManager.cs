using UnityEngine;

public sealed class PassengerManager : MonoBehaviour
{
    private int passengerCount;

    public void PickupPassengers()
    {
        passengerCount += 10;
        Debug.Log("Passengers Picked Up");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreateInGameplayScene()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "GameScene")
        {
            return;
        }

        if (FindObjectOfType<PassengerManager>() != null)
        {
            return;
        }

        GameObject go = new GameObject("PassengerManager");
        go.AddComponent<PassengerManager>();
    }
}
