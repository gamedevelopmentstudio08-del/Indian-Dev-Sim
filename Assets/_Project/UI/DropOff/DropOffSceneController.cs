using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class DropOffSceneController : MonoBehaviour
{
    private Canvas canvas;
    private RawImage minimapImage;
    private MinimapFollow minimapFollow;
    private TextMeshProUGUI coinsText;
    private TextMeshProUGUI passengersText;
    private TextMeshProUGUI weatherText;
    private TextMeshProUGUI fuelText;
    private TextMeshProUGUI sleepText;
    private TextMeshProUGUI satisfactionText;
    private TextMeshProUGUI objectiveText;
    private TextMeshProUGUI phaseText;
    private TextMeshProUGUI passengerListText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreateInDropOffScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName != "DropOffScene" && sceneName != "PickupScene")
        {
            return;
        }

        if (FindObjectOfType<DropOffSceneController>() != null)
        {
            return;
        }

        GameObject go = new GameObject("DropOffSceneController");
        go.AddComponent<DropOffSceneController>();
    }

    private void Awake()
    {
        SceneUiHelper.EnsureEventSystem();
        canvas = SceneUiHelper.EnsureOverlayCanvas("DropOffCanvas");
        BuildUi();
        SetupMinimap();
        EnsurePickupStation();
        RefreshUi();
    }

    private void Update()
    {
        TryBindMinimapTarget();
        RefreshUi();
    }

    private void BuildUi()
    {
        if (canvas == null)
        {
            return;
        }

        CreateHeader(canvas.transform);
        CreateStatsPanel(canvas.transform);
        CreatePassengerPanel(canvas.transform);
        CreateMinimapFrame(canvas.transform);
    }

    private void CreateHeader(Transform parent)
    {
        GameObject panel = CreatePanel(parent, "DropOffHeader", new Vector2(24f, -24f), new Vector2(560f, 92f));
        objectiveText = CreateText(panel.transform, "ObjectiveText", GameData.DropOffObjective, 30, new Vector2(20f, 18f), Color.white);
        phaseText = CreateText(panel.transform, "PhaseText", "Passengers Ready", 20, new Vector2(20f, -18f), new Color(0.85f, 0.95f, 1f));
    }

    private void CreateStatsPanel(Transform parent)
    {
        GameObject panel = CreatePanel(parent, "JourneyStatsPanel", new Vector2(24f, -140f), new Vector2(360f, 330f));
        CreateText(panel.transform, "PanelTitle", "Journey Stats", 26, new Vector2(20f, 18f), new Color(0.95f, 0.85f, 0.35f));
        coinsText = CreateText(panel.transform, "CoinsText", "Coins: 0", 22, new Vector2(20f, -26f), Color.white);
        weatherText = CreateText(panel.transform, "WeatherText", "Weather: Clear", 22, new Vector2(20f, -68f), Color.white);
        fuelText = CreateText(panel.transform, "FuelText", "Fuel: 100%", 22, new Vector2(20f, -110f), Color.white);
        sleepText = CreateText(panel.transform, "SleepText", "Sleep: 100%", 22, new Vector2(20f, -152f), Color.white);
        satisfactionText = CreateText(panel.transform, "SatisfactionText", "Satisfaction: 100%", 22, new Vector2(20f, -194f), Color.white);
        passengersText = CreateText(panel.transform, "PassengersText", "Passengers: 0", 22, new Vector2(20f, -236f), Color.white);
    }

    private void CreatePassengerPanel(Transform parent)
    {
        GameObject panel = CreatePanel(parent, "PassengerListPanel", new Vector2(24f, -500f), new Vector2(360f, 280f));
        CreateText(panel.transform, "PanelTitle", "Passengers", 26, new Vector2(20f, 18f), new Color(0.95f, 0.85f, 0.35f));
        passengerListText = CreateText(panel.transform, "PassengerListText", "No passengers loaded", 20, new Vector2(20f, -28f), Color.white);
        passengerListText.rectTransform.sizeDelta = new Vector2(300f, 220f);
        passengerListText.enableWordWrapping = true;
    }

    private void CreateMinimapFrame(Transform parent)
    {
        GameObject panel = CreatePanel(parent, "DropOffMinimapPanel", new Vector2(-20f, -24f), new Vector2(260f, 260f));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-20f, -24f);

        GameObject minimapObject = new GameObject("MinimapImage", typeof(RectTransform), typeof(RawImage));
        minimapObject.transform.SetParent(panel.transform, false);
        RectTransform minimapRect = minimapObject.GetComponent<RectTransform>();
        minimapRect.anchorMin = Vector2.zero;
        minimapRect.anchorMax = Vector2.one;
        minimapRect.offsetMin = new Vector2(8f, 8f);
        minimapRect.offsetMax = new Vector2(-8f, -8f);

        minimapImage = minimapObject.GetComponent<RawImage>();
        minimapImage.color = Color.white;
    }

    private void SetupMinimap()
    {
        Camera minimapCamera = new GameObject("DropOffMinimapCamera").AddComponent<Camera>();
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = 90f;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.08f, 0.10f, 0.12f, 1f);
        minimapCamera.nearClipPlane = 0.1f;
        minimapCamera.farClipPlane = 3000f;
        minimapCamera.cullingMask = LayerMask.GetMask("Default", "Bus", "Traffic", "Environment", "Ground", "Minimap");
        minimapCamera.targetTexture = new RenderTexture(512, 512, 16);

        minimapFollow = minimapCamera.gameObject.AddComponent<MinimapFollow>();
        TryBindMinimapTarget();

        minimapCamera.Render();
        if (minimapImage != null)
        {
            minimapImage.texture = minimapCamera.targetTexture;
        }
    }

    private static void EnsurePickupStation()
    {
        if (SceneManager.GetActiveScene().name != "PickupScene")
        {
            return;
        }

        if (GameObject.Find("BusStandPlatform") != null || GameObject.Find("PickupZone") != null)
        {
            return;
        }

        GameObject root = new GameObject("BusStop");
        root.transform.position = Vector3.zero;

        GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stand.name = "BusStandPlatform";
        stand.transform.SetParent(root.transform, false);
        stand.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        stand.transform.localScale = new Vector3(8f, 0.5f, 8f);
        Renderer standRenderer = stand.GetComponent<Renderer>();
        if (standRenderer != null)
        {
            standRenderer.material.color = new Color(0.45f, 0.45f, 0.45f, 1f);
        }

        GameObject pickupZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pickupZone.name = "PickupZone";
        pickupZone.transform.SetParent(root.transform, false);
        pickupZone.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        pickupZone.transform.localScale = new Vector3(7f, 0.2f, 7f);
        Renderer pickupRenderer = pickupZone.GetComponent<Renderer>();
        if (pickupRenderer != null)
        {
            pickupRenderer.material.color = new Color(0f, 1f, 0f, 0.35f);
        }

        BoxCollider collider = pickupZone.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = pickupZone.AddComponent<BoxCollider>();
        }

        collider.isTrigger = true;

        if (pickupZone.GetComponent<PickupZone>() == null)
        {
            pickupZone.AddComponent<PickupZone>();
        }
    }

    private void TryBindMinimapTarget()
    {
        if (minimapFollow == null)
        {
            return;
        }

        Transform bus = ResolveBusTransform();
        if (bus != null)
        {
            minimapFollow.SetTarget(bus);
        }
    }

    private static Transform ResolveBusTransform()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.transform;
        }

        SimpleBusController controller = FindObjectOfType<SimpleBusController>();
        return controller != null ? controller.transform : null;
    }

    private void RefreshUi()
    {
        if (objectiveText != null)
        {
            objectiveText.text = GameData.DropOffObjective;
        }

        if (phaseText != null)
        {
            phaseText.text = GameData.IsDropOffPhase ? "Passengers Picked Up" : "Drive to Pickup Point";
        }

        if (coinsText != null)
        {
            coinsText.text = "Coins: " + GameData.Coins.ToString("N0");
        }

        if (weatherText != null)
        {
            weatherText.text = "Weather: " + GameData.CurrentWeatherLabel;
        }

        if (fuelText != null)
        {
            fuelText.text = "Fuel: " + Mathf.Clamp(GameData.Fuel, 0f, 100f).ToString("0") + "%";
        }

        if (sleepText != null)
        {
            sleepText.text = "Sleep: " + Mathf.Clamp(GameData.Sleep, 0f, 100f).ToString("0") + "%";
        }

        if (satisfactionText != null)
        {
            satisfactionText.text = "Satisfaction: " + Mathf.Clamp(GameData.Satisfaction, 0f, 100f).ToString("0") + "%";
        }

        if (passengersText != null)
        {
            passengersText.text = "Passengers: " + GameData.PassengerNames.Count.ToString();
        }

        if (passengerListText != null)
        {
            List<string> passengers = GameData.PassengerNames;
            if (passengers == null || passengers.Count == 0)
            {
                passengerListText.text = "No passengers loaded";
            }
            else
            {
                passengerListText.text = string.Join("\n", passengers.Select((name, index) => (index + 1) + ". " + name));
            }
        }
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.05f, 0.08f, 0.12f, 0.82f);
        return panel;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, int size, Vector2 anchoredPosition, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(300f, 34f);

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Left;
        TMP_FontAsset font = GetFont();
        if (font != null)
        {
            tmp.font = font;
        }

        return tmp;
    }

    private static TMP_FontAsset GetFont()
    {
        try
        {
            return TMP_Settings.defaultFontAsset;
        }
        catch
        {
            return null;
        }
    }
}
