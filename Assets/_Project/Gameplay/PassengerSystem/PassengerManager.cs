using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class PassengerManager : MonoBehaviour
{
    private int passengerCount;
    private bool transitionQueued;

    public void PickupPassengers()
    {
        if (transitionQueued)
        {
            return;
        }

        Transform playerBus = ResolveBus();
        if (playerBus != null)
        {
            GameData.SetSceneSpawn(playerBus.position, playerBus.rotation);
        }

        passengerCount += 10;
        GameData.BeginDropOffPhase(CreatePassengerManifest(passengerCount));
        GameData.AddCoins(250);
        GameData.AdjustFuel(-5f);
        GameData.AdjustSleep(-2f);
        GameData.AdjustSatisfaction(4f);
        Debug.Log("Passengers Picked Up");
        transitionQueued = true;
        StartCoroutine(LoadDropOffSceneRoutine());
    }

    private IEnumerator LoadDropOffSceneRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        SceneLoader.LoadSceneAsync(GameData.DropOffSceneName);
    }

    private static List<string> CreatePassengerManifest(int count)
    {
        string[] sampleNames =
        {
            "Aarav", "Diya", "Ishaan", "Meera", "Kabir",
            "Ananya", "Rohan", "Saanvi", "Arjun", "Priya"
        };

        List<string> manifest = new List<string>();
        int total = Mathf.Max(3, Mathf.Min(count, sampleNames.Length));
        for (int i = 0; i < total; i++)
        {
            manifest.Add(sampleNames[i]);
        }

        return manifest;
    }

    private static Transform ResolveBus()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.transform;
        }

        SimpleBusController controller = FindObjectOfType<SimpleBusController>();
        return controller != null ? controller.transform : null;
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
