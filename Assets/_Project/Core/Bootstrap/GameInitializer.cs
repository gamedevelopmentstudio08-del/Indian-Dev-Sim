using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameInitializer : MonoBehaviour
{
    [SerializeField] private GameObject busPrefab;
    [SerializeField] private float routeLineHeight = 0.25f;

    private System.Collections.IEnumerator Start()
    {
        Debug.Log("GameScene Loaded");

        float waitTime = 0f;
        Transform busTransform = null;
        while (busTransform == null && waitTime < 3f)
        {
            busTransform = ResolveBus();
            if (busTransform != null)
            {
                break;
            }

            waitTime += Time.deltaTime;
            yield return null;
        }

        InitializeGameplay(busTransform);
    }

    private void InitializeGameplay(Transform busTransform)
    {
        if (busTransform == null)
        {
            Debug.LogError("GameInitializer: Bus not found and no prefab assigned.");
            return;
        }

        if (!GameData.HasRoute())
        {
            Debug.LogWarning("GameInitializer: No route selected. Using default start and end.");
            GameData.SelectedRouteName = "Default Route";
            GameData.StartPosition = busTransform.position;
            GameData.EndPosition = busTransform.position + Vector3.forward * 500f;
            GameData.PathPoints = new System.Collections.Generic.List<Vector3>
            {
                GameData.StartPosition,
                GameData.EndPosition
            };
        }

        Vector3 spawnPosition = new Vector3(GameData.StartPosition.x, busTransform.position.y, GameData.StartPosition.z);
        busTransform.position = spawnPosition;
        busTransform.rotation = GetBusRotation();

        Rigidbody rb = busTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        CreateBusStand(spawnPosition);
        DrawRouteLine();
        SetupMinimap(busTransform);
    }

    private Transform ResolveBus()
    {
        GameObject existingBus = null;
        try
        {
            existingBus = GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
        }

        if (existingBus != null)
        {
            return existingBus.transform;
        }

        if (busPrefab != null)
        {
            GameObject instance = Instantiate(busPrefab);
            try
            {
                instance.tag = "Player";
            }
            catch (UnityException)
            {
            }

            return instance.transform;
        }

        SimpleBusController controller = FindObjectOfType<SimpleBusController>();
        return controller != null ? controller.transform : null;
    }

    private Quaternion GetBusRotation()
    {
        Vector3 direction = GameData.EndPosition - GameData.StartPosition;
        if (GameData.PathPoints != null && GameData.PathPoints.Count >= 2)
        {
            direction = GameData.PathPoints[1] - GameData.PathPoints[0];
        }

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = Vector3.forward;
        }

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void CreateBusStand(Vector3 spawnPosition)
    {
        GameObject root = new GameObject("BusStop");
        root.transform.position = new Vector3(spawnPosition.x, 0f, spawnPosition.z);

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

    private void DrawRouteLine()
    {
        if (GameData.PathPoints == null || GameData.PathPoints.Count == 0)
        {
            return;
        }

        GameObject lineObject = new GameObject("RouteLine");
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.widthMultiplier = 1f;
        line.positionCount = GameData.PathPoints.Count;
        line.startColor = new Color(0.1f, 0.75f, 1f, 1f);
        line.endColor = new Color(1f, 0.85f, 0.2f, 1f);

        for (int i = 0; i < GameData.PathPoints.Count; i++)
        {
            Vector3 point = GameData.PathPoints[i];
            line.SetPosition(i, new Vector3(point.x, point.y + routeLineHeight, point.z));
        }
    }

    private void SetupMinimap(Transform busTransform)
    {
        Camera minimapCamera = new GameObject("MinimapCamera").AddComponent<Camera>();
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = 90f;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.08f, 0.10f, 0.12f, 1f);
        minimapCamera.targetTexture = new RenderTexture(512, 512, 16);

        MinimapFollow follow = minimapCamera.gameObject.AddComponent<MinimapFollow>();
        follow.SetTarget(busTransform);

        Canvas canvas = SceneUiHelper.EnsureOverlayCanvas("GameplayCanvas");
        RawImage minimap = CreateOrGetMinimapImage(canvas.transform);
        minimap.texture = minimapCamera.targetTexture;
    }

    private static RawImage CreateOrGetMinimapImage(Transform parent)
    {
        GameObject existing = GameObject.Find("MinimapRawImage");
        if (existing != null)
        {
            return existing.GetComponent<RawImage>();
        }

        GameObject imageObj = new GameObject("MinimapRawImage", typeof(RectTransform), typeof(RawImage));
        imageObj.transform.SetParent(parent, false);
        RectTransform rect = imageObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-20f, -20f);
        rect.sizeDelta = new Vector2(220f, 220f);
        return imageObj.GetComponent<RawImage>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreateInGameScene()
    {
        if (SceneManager.GetActiveScene().name != "GameScene")
        {
            return;
        }

        if (FindObjectOfType<GameInitializer>() != null)
        {
            return;
        }

        GameObject go = new GameObject("GameInitializer");
        go.AddComponent<GameInitializer>();
    }
}
