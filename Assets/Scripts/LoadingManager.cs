using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LoadingManager : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "SampleScene";
    [SerializeField] private float minimumLoadingDelay = 1.5f;
    [SerializeField] private TMP_Text statusText;

    private bool hasStartedLoading;

    private void Awake()
    {
        SceneUiHelper.EnsureEventSystem();
        if (statusText == null)
        {
            GameObject statusObject = GameObject.Find("LoadingStatusText");
            if (statusObject != null)
            {
                statusText = statusObject.GetComponent<TMP_Text>();
            }
        }
    }

    private void Start()
    {
        if (hasStartedLoading)
        {
            return;
        }

        hasStartedLoading = true;
        Debug.Log("LoadingScene Started");
        StartCoroutine(LoadGameplayScene());
    }

    private IEnumerator LoadGameplayScene()
    {
        if (statusText != null)
        {
            statusText.text = "Preparing route...";
        }

        yield return new WaitForSeconds(minimumLoadingDelay);

        Debug.Log("Loading SampleScene");
        if (statusText != null)
        {
            statusText.text = "Loading gameplay...";
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(gameplaySceneName);
        if (operation == null)
        {
            Debug.LogError("LoadingManager: Failed to load SampleScene.");
            yield break;
        }

        while (!operation.isDone)
        {
            if (statusText != null)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                statusText.text = "Loading gameplay... " + Mathf.RoundToInt(progress * 100f) + "%";
            }

            yield return null;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreateInLoadingScene()
    {
        if (SceneManager.GetActiveScene().name != "LoadingScene")
        {
            return;
        }

        if (FindObjectOfType<LoadingManager>() != null)
        {
            return;
        }

        GameObject go = new GameObject("LoadingManager");
        go.AddComponent<LoadingManager>();
    }
}
