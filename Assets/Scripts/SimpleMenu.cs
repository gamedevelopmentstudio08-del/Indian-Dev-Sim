using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SimpleMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "RouteSelectionScene";

    private void Awake()
    {
        SceneUiHelper.EnsureEventSystem();
        WireButton("PlayButton", PlayGame);
        WireButton("ExitButton", ExitGame);
    }

    public void PlayGame()
    {
        Debug.Log("Opening Route Selection Scene");
        SceneManager.LoadScene(gameSceneName);
    }

    public void ExitGame()
    {
        Debug.Log("SimpleMenu: ExitGame()");
        Application.Quit();
    }

    private static void WireButton(string objectName, UnityEngine.Events.UnityAction handler)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj == null)
        {
            Debug.LogWarning($"SimpleMenu: '{objectName}' not found in scene.");
            return;
        }

        Button button = obj.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogWarning($"SimpleMenu: '{objectName}' has no Button component.");
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(handler);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreateInMainMenuScene()
    {
        if (SceneManager.GetActiveScene().name != "MainMenuScene")
        {
            return;
        }

        if (FindObjectOfType<SimpleMenu>() != null)
        {
            return;
        }

        GameObject go = new GameObject("SimpleMenu");
        go.AddComponent<SimpleMenu>();
    }
}
