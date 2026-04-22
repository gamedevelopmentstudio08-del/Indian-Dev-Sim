using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RouteSelectionManager : MonoBehaviour
{
    [SerializeField] private List<RouteData> availableRoutes = new List<RouteData>();
    [SerializeField] private TMP_Text selectedRouteLabel;
    [SerializeField] private Button driveButton;
    [SerializeField] private string loadingSceneName = "LoadingScene";
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    private void Awake()
    {
        SceneUiHelper.EnsureEventSystem();
        EnsureSceneCamera();
        EnsureRoutes();
        EnsureBasicUiIfMissing();
        AutoWireUiIfNeeded();
        UpdateSelectedRouteLabel();
    }

    private void Start()
    {
        Debug.Log("Route Selection Scene Loaded");
    }

    public void SelectRoute(RouteData route)
    {
        if (route == null)
        {
            Debug.LogWarning("RouteSelectionManager: route is null.");
            return;
        }

        GameData.SetRoute(route);
        Debug.Log("Route Selected: " + ToRouteKey(route.routeName));
        UpdateSelectedRouteLabel();
    }

    public void SelectRoute(string routeName)
    {
        if (string.IsNullOrWhiteSpace(routeName))
        {
            Debug.LogWarning("RouteSelectionManager: route name is empty.");
            return;
        }

        for (int i = 0; i < availableRoutes.Count; i++)
        {
            RouteData route = availableRoutes[i];
            if (route == null)
            {
                continue;
            }

            if (string.Equals(ToRouteKey(route.routeName), routeName, System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(route.routeName, routeName, System.StringComparison.OrdinalIgnoreCase))
            {
                SelectRoute(route);
                return;
            }
        }

        Debug.LogWarning("RouteSelectionManager: route not found for name " + routeName);
    }

    public void SelectRouteByIndex(int index)
    {
        if (index < 0 || index >= availableRoutes.Count)
        {
            Debug.LogWarning("RouteSelectionManager: invalid route index.");
            return;
        }

        SelectRoute(availableRoutes[index]);
    }

    public void StartDrive()
    {
        if (!GameData.HasRoute())
        {
            Debug.LogWarning("RouteSelectionManager: No route selected.");
            return;
        }

        Debug.Log("Loading Scene...");
        SceneManager.LoadScene(loadingSceneName);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void EnsureRoutes()
    {
        if (availableRoutes != null && availableRoutes.Count >= 3)
        {
            return;
        }

        availableRoutes = new List<RouteData>
        {
            CreateRoute(
                "Chennai -> Bangalore",
                "Chennai",
                "Bangalore",
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, 5500f),
                new List<Vector3>
                {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(0f, 0f, 900f),
                    new Vector3(0f, 0f, 2200f),
                    new Vector3(0f, 0f, 3900f),
                    new Vector3(0f, 0f, 5500f)
                }
            ),
            CreateRoute(
                "Chennai -> Coimbatore",
                "Chennai",
                "Coimbatore",
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, 4800f),
                new List<Vector3>
                {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(0f, 0f, 700f),
                    new Vector3(0f, 0f, 1800f),
                    new Vector3(0f, 0f, 3000f),
                    new Vector3(0f, 0f, 4800f)
                }
            ),
            CreateRoute(
                "Bangalore -> Mysore",
                "Bangalore",
                "Mysore",
                new Vector3(0f, 0f, 1200f),
                new Vector3(0f, 0f, 3600f),
                new List<Vector3>
                {
                    new Vector3(0f, 0f, 1200f),
                    new Vector3(0f, 0f, 1800f),
                    new Vector3(0f, 0f, 2500f),
                    new Vector3(0f, 0f, 3600f)
                }
            )
        };

    }

    private static RouteData CreateRoute(
        string routeName,
        string startName,
        string endName,
        Vector3 startPosition,
        Vector3 endPosition,
        List<Vector3> pathPoints)
    {
        RouteData route = ScriptableObject.CreateInstance<RouteData>();
        route.routeName = routeName;
        route.startLocationName = startName;
        route.endLocationName = endName;
        route.startPosition = startPosition;
        route.endPosition = endPosition;
        route.pathPoints = pathPoints ?? new List<Vector3>();
        return route;
    }

    private void UpdateSelectedRouteLabel()
    {
        if (selectedRouteLabel == null)
        {
            return;
        }

        if (!GameData.HasRoute())
        {
            selectedRouteLabel.text = "Selected Route: None";
            return;
        }

        selectedRouteLabel.text = "Selected Route: " + GameData.SelectedRouteName;
    }

    private void AutoWireUiIfNeeded()
    {
        EnsureCanvasReady();

        if (selectedRouteLabel == null)
        {
            GameObject label = GameObject.Find("SelectedRouteText");
            if (label != null)
            {
                selectedRouteLabel = label.GetComponent<TMP_Text>();
            }
        }

        if (driveButton == null)
        {
            GameObject driveObj = GameObject.Find("DriveButton");
            if (driveObj != null)
            {
                driveButton = driveObj.GetComponent<Button>();
            }
        }

        if (driveButton != null)
        {
            driveButton.onClick.RemoveAllListeners();
            driveButton.onClick.AddListener(StartDrive);
        }

        WireRouteButton("ChennaiToBangalore", 0);
        WireRouteButton("CityRoute", 1);
    }

    private void WireRouteButton(string objectName, int routeIndex)
    {
        if (routeIndex < 0 || routeIndex >= availableRoutes.Count)
        {
            return;
        }

        GameObject obj = GameObject.Find(objectName);
        if (obj == null)
        {
            return;
        }

        Button button = obj.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectRouteByIndex(routeIndex));
    }

    private void EnsureBasicUiIfMissing()
    {
        if (GameObject.Find("DriveButton") != null &&
            GameObject.Find("SelectedRouteText") != null &&
            GameObject.Find("ChennaiToBangalore") != null &&
            GameObject.Find("CityRoute") != null &&
            (GameObject.Find("MapBackground") != null || GameObject.Find("RouteSelectionPanel") != null))
        {
            return;
        }

        Canvas canvas = EnsureCanvasReady();

        GameObject panel = new GameObject("RouteSelectionPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.11f, 0.16f, 0.96f);
        panelImage.raycastTarget = true;

        CreateLabel(panel.transform, "Title", "Route Selection", new Vector2(0f, -80f), 52);
        CreateLabel(panel.transform, "MapText", "Map Panel", new Vector2(0f, -150f), 24);
        CreateLabel(panel.transform, "SelectedRouteText", "Selected Route: None", new Vector2(0f, -240f), 34);

        CreateRouteButton(panel.transform, "ChennaiToBangalore", "Chennai -> Bangalore", new Vector2(0f, -360f), 0);
        CreateRouteButton(panel.transform, "CityRoute", "City Route", new Vector2(0f, -460f), 1);
        CreateActionButton(panel.transform, "DriveButton", "Drive", new Vector2(0f, -720f), StartDrive);
    }

    private static Canvas EnsureCanvasReady()
    {
        return SceneUiHelper.EnsureOverlayCanvas("RouteSelectionCanvas");
    }

    private static void EnsureSceneCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.orthographic = true;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.transform.rotation = Quaternion.identity;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.09f, 0.10f, 0.13f, 1f);
    }

    private void CreateRouteButton(Transform parent, string objectName, string label, Vector2 anchoredPos, int index)
    {
        Button button = CreateActionButton(parent, objectName, label, anchoredPos, () => SelectRouteByIndex(index));
        button.name = objectName;
    }

    private static TMP_Text CreateLabel(Transform parent, string objectName, string text, Vector2 anchoredPos, float fontSize)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(900f, 80f);
        TMP_Text tmp = obj.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        return tmp;
    }

    private static Button CreateActionButton(Transform parent, string objectName, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(520f, 70f);
        Image image = obj.GetComponent<Image>();
        image.color = new Color(0.15f, 0.20f, 0.26f, 0.95f);

        TMP_Text txt = CreateLabel(obj.transform, "Label", label, new Vector2(0f, 0f), 28f);
        RectTransform txtRect = txt.rectTransform;
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.pivot = new Vector2(0.5f, 0.5f);
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        Button btn = obj.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(onClick);
        return btn;
    }

    private static string ToRouteKey(string routeName)
    {
        if (string.IsNullOrWhiteSpace(routeName))
        {
            return "Unknown_Route";
        }

        return routeName.Replace(" -> ", "_").Replace(" ", string.Empty);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreateInRouteSelectionScene()
    {
        if (SceneManager.GetActiveScene().name != "RouteSelectionScene")
        {
            return;
        }

        if (FindObjectOfType<RouteSelectionManager>() != null)
        {
            return;
        }

        GameObject go = new GameObject("RouteSelectionManager");
        go.AddComponent<RouteSelectionManager>();
    }
}
