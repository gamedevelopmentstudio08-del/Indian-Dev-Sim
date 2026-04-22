using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LoadingManager : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "GameScene";
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
        Debug.Log("Loading Started");

        if (statusText != null)
        {
            statusText.text = "Preparing route...";
        }

        yield return new WaitForSeconds(minimumLoadingDelay);

        if (statusText != null)
        {
            statusText.text = "Loading gameplay...";
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(gameplaySceneName);
        if (operation == null)
        {
            Debug.LogError("LoadingManager: Failed to load GameScene.");
            yield break;
        }

        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            Debug.Log("Loading Progress: " + Mathf.RoundToInt(progress * 100f) + "%");

            if (statusText != null)
            {
                statusText.text = "Loading gameplay... " + Mathf.RoundToInt(progress * 100f) + "%";
            }

            if (operation.progress >= 0.9f)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        Debug.Log("GameScene Loaded");
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
