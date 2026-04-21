using UnityEngine;
using UnityEngine.UI;

public sealed class SimpleMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";

    private void Awake()
    {
        // Scene-safe wiring (no runtime UI creation): hooks up existing buttons by name.
        WireButton("PlayButton", PlayGame);
        WireButton("ExitButton", ExitGame);
    }

    public void PlayGame()
    {
        Debug.Log("SimpleMenu: PlayGame()");
        SceneLoader.LoadSceneAsync(gameSceneName);
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
}
