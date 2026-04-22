using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SceneLoader : MonoBehaviour
{
    private static SceneLoader instance;

    private Canvas loadingCanvas;
    private Slider progressSlider;
    private TextMeshProUGUI progressLabel;

    public static SceneLoader Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject loaderObject = new GameObject("SceneLoader");
            instance = loaderObject.AddComponent<SceneLoader>();
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateLoadingUI();
            SetLoadingUiVisible(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void LoadSceneAsync(string sceneName)
    {
        Instance.StartSceneLoad(sceneName);
    }

    private void StartSceneLoad(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("SceneLoader: scene name is null or empty.");
            return;
        }

        if (!SceneExistsInBuildSettings(sceneName))
        {
            Debug.LogError($"SceneLoader: scene '{sceneName}' is not in Build Settings.");
            return;
        }

        ShowLoadingUI();
        Debug.Log("Loading Started");
        StopAllCoroutines();
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        // Let one frame render so the loading UI is visible before starting load.
        yield return null;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        if (asyncLoad == null)
        {
            Debug.LogError($"SceneLoader: failed to start async loading for '{sceneName}'.");
            SetLoadingUiVisible(false);
            yield break;
        }

        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            SetProgress(progress);

            if (asyncLoad.progress >= 0.9f)
            {
                SetProgress(1f);
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        SetLoadingUiVisible(false);
    }

    private bool SceneExistsInBuildSettings(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string name = Path.GetFileNameWithoutExtension(scenePath);
            if (string.Equals(name, sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void CreateLoadingUI()
    {
        if (loadingCanvas != null && progressSlider != null)
        {
            return;
        }

        BindExistingLoadingUi();
        if (loadingCanvas != null && progressSlider != null)
        {
            return;
        }

        if (loadingCanvas == null)
        {
            GameObject canvasObject = new GameObject("LoadingCanvas");
            canvasObject.transform.SetParent(transform, false);

            loadingCanvas = canvasObject.AddComponent<Canvas>();
            loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            loadingCanvas.sortingOrder = 999;
            canvasObject.AddComponent<GraphicRaycaster>();
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;
        }

        GameObject panelObject = CreatePanel(loadingCanvas.transform);
        CreateLoadingText(panelObject.transform);
        CreateProgressSlider(panelObject.transform);
        Debug.Log("Loading UI Created");
    }

    private void BindExistingLoadingUi()
    {
        GameObject existingCanvasObj = GameObject.Find("LoadingCanvas");
        if (existingCanvasObj == null)
        {
            return;
        }

        loadingCanvas = existingCanvasObj.GetComponent<Canvas>();
        if (loadingCanvas == null)
        {
            return;
        }

        progressSlider = existingCanvasObj.GetComponentInChildren<Slider>(true);
        progressLabel = existingCanvasObj.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private GameObject CreatePanel(Transform parent)
    {
        GameObject panelObject = new GameObject("Panel");
        panelObject.transform.SetParent(parent, false);
        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = Color.black;

        return panelObject;
    }

    private void CreateLoadingText(Transform parent)
    {
        if (progressLabel != null)
        {
            return;
        }

        GameObject textObject = new GameObject("LoadingText");
        textObject.transform.SetParent(parent, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, 90f);
        textRect.sizeDelta = new Vector2(360f, 80f);

        TextMeshProUGUI loadingText = textObject.AddComponent<TextMeshProUGUI>();
        loadingText.text = "Loading...";
        loadingText.color = Color.white;
        loadingText.alignment = TextAlignmentOptions.Center;
        loadingText.fontSize = 36;

        progressLabel = loadingText;
    }

    private void CreateProgressSlider(Transform parent)
    {
        GameObject sliderObject = new GameObject("ProgressSlider");
        sliderObject.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(0f, 0f);
        sliderRect.sizeDelta = new Vector2(700f, 60f);

        progressSlider = sliderObject.AddComponent<Slider>();
        progressSlider.minValue = 0f;
        progressSlider.maxValue = 1f;
        progressSlider.value = 0f;

        GameObject background = CreateSliderPart("Background", sliderObject.transform, new Color(0.15f, 0.15f, 0.15f, 1f));
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(10f, 10f);
        fillAreaRect.offsetMax = new Vector2(-10f, -10f);

        GameObject fill = CreateSliderPart("Fill", fillArea.transform, new Color(0.2f, 0.85f, 0.3f, 1f));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        progressSlider.fillRect = fillRect;
        progressSlider.targetGraphic = fill.GetComponent<Image>();
        progressSlider.direction = Slider.Direction.LeftToRight;
    }

    private static GameObject CreateSliderPart(string name, Transform parent, Color color)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.AddComponent<RectTransform>();
        Image image = part.AddComponent<Image>();
        image.color = color;
        return part;
    }

    private void SetLoadingUiVisible(bool visible)
    {
        if (loadingCanvas != null)
        {
            loadingCanvas.gameObject.SetActive(visible);
        }
    }

    private void ShowLoadingUI()
    {
        CreateLoadingUI();
        SetProgress(0f);
        SetLoadingUiVisible(true);
    }

    private void SetProgress(float progress)
    {
        if (progressSlider != null)
        {
            progressSlider.value = progress;
        }

        if (progressLabel != null)
        {
            int percent = Mathf.RoundToInt(progress * 100f);
            progressLabel.text = $"Loading... {percent}%";
        }
    }
}
