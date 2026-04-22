using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class RouteGuidanceSystem : MonoBehaviour
{
    private const string RouteRootName = "RouteGuidanceRoot";
    private const string RouteLineName = "RouteLine";
    private const string PickupMarkerName = "PickupMarker";
    private const string DropMarkerName = "DropMarker";
    private const string MinimapLayerName = "Minimap";

    [SerializeField] private float routeLineWidth = 0.45f;
    [SerializeField] private float routeLineHeightOffset = 0.35f;
    [SerializeField] private float markerHeightOffset = 0.5f;
    [SerializeField] private float markerScale = 1.8f;
    [SerializeField] private Color pickupRouteColor = new Color(0.55f, 0.06f, 0.06f, 1f);
    [SerializeField] private Color dropRouteColor = new Color(0.12f, 0.9f, 0.25f, 1f);

    private LineRenderer routeLine;
    private GameObject pickupMarker;
    private GameObject dropMarker;
    private readonly List<Vector3> routePoints = new List<Vector3>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "GameScene" && sceneName != "PickupScene" && sceneName != "DropOffScene")
        {
            return;
        }

        if (FindObjectOfType<RouteGuidanceSystem>() != null)
        {
            return;
        }

        GameObject go = new GameObject(RouteRootName);
        go.AddComponent<RouteGuidanceSystem>();
    }

    private void Awake()
    {
        EnsureVisuals();
        RefreshRoute();
    }

    private void OnEnable()
    {
        GameData.RouteChanged += HandleRouteChanged;
        GameData.RoutePhaseChanged += HandleRoutePhaseChanged;
        EnsureVisuals();
        RefreshRoute();
    }

    private void OnDisable()
    {
        GameData.RouteChanged -= HandleRouteChanged;
        GameData.RoutePhaseChanged -= HandleRoutePhaseChanged;
    }

    private void HandleRouteChanged()
    {
        RefreshRoute();
    }

    private void HandleRoutePhaseChanged(bool isDropOffPhase)
    {
        RefreshRoute();
    }

    private void EnsureVisuals()
    {
        EnsureRouteLine();
        EnsureMarkers();
    }

    private void RefreshRoute()
    {
        EnsureVisuals();
        BuildRoutePoints();
        ApplyRouteVisuals();
    }

    private void EnsureRouteLine()
    {
        if (routeLine != null)
        {
            return;
        }

        Transform existing = transform.Find(RouteLineName);
        GameObject lineObject = existing != null ? existing.gameObject : new GameObject(RouteLineName);
        if (lineObject.transform.parent != transform)
        {
            lineObject.transform.SetParent(transform, false);
        }

        SetLayerRecursively(lineObject, MinimapLayerName);

        routeLine = lineObject.GetComponent<LineRenderer>();
        if (routeLine == null)
        {
            routeLine = lineObject.AddComponent<LineRenderer>();
        }

        routeLine.useWorldSpace = true;
        routeLine.loop = false;
        routeLine.alignment = LineAlignment.View;
        routeLine.widthMultiplier = routeLineWidth;
        routeLine.numCapVertices = 4;
        routeLine.numCornerVertices = 4;
        routeLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        routeLine.receiveShadows = false;
        routeLine.textureMode = LineTextureMode.Stretch;
        routeLine.material = CreateLineMaterial();
    }

    private void EnsureMarkers()
    {
        pickupMarker = EnsureMarker(PickupMarkerName, new Color(0.12f, 0.86f, 0.22f, 1f));
        dropMarker = EnsureMarker(DropMarkerName, new Color(0.92f, 0.16f, 0.16f, 1f));
    }

    private GameObject EnsureMarker(string markerName, Color color)
    {
        Transform existing = transform.Find(markerName);
        GameObject marker = existing != null ? existing.gameObject : null;
        if (marker == null)
        {
            marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = markerName;
            marker.transform.SetParent(transform, false);
            marker.transform.localScale = Vector3.one * markerScale;

            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
        }

        SetLayerRecursively(marker, MinimapLayerName);

        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateMarkerMaterial(color);
        }

        return marker;
    }

    private void BuildRoutePoints()
    {
        routePoints.Clear();

        Vector3 startPosition = GameData.StartPosition;
        Vector3 pickupPosition = GameData.PickupPosition;
        Vector3 dropPosition = GameData.EndPosition;
        if (GameData.RouteWaypoints != null && GameData.RouteWaypoints.Count >= 3)
        {
            startPosition = GameData.RouteWaypoints[0].Position;
            pickupPosition = GameData.RouteWaypoints[1].Position;
            dropPosition = GameData.RouteWaypoints[2].Position;
        }

        if (!GameData.IsDropOffPhase)
        {
            AddRouteSegment(startPosition, pickupPosition);
        }
        else
        {
            AddRouteSegment(pickupPosition, dropPosition);
        }

        PositionMarker(pickupMarker, pickupPosition, !GameData.IsDropOffPhase);
        PositionMarker(dropMarker, dropPosition, GameData.IsDropOffPhase);
    }

    private void AddRouteSegment(Vector3 startPosition, Vector3 endPosition)
    {
        if (GameData.PathPoints != null && GameData.PathPoints.Count >= 2)
        {
            int startIndex = FindClosestPointIndex(startPosition);
            int endIndex = FindClosestPointIndex(endPosition);
            AddOrderedPathSegment(startIndex, endIndex);
            AddUniquePoint(startPosition);
            AddUniquePoint(endPosition);
            return;
        }

        AddUniquePoint(startPosition);
        AddUniquePoint(endPosition);
    }

    private void AddOrderedPathSegment(int startIndex, int endIndex)
    {
        if (GameData.PathPoints == null || GameData.PathPoints.Count == 0)
        {
            return;
        }

        int step = startIndex <= endIndex ? 1 : -1;
        for (int i = startIndex; ; i += step)
        {
            AddUniquePoint(GameData.PathPoints[i]);
            if (i == endIndex)
            {
                break;
            }
        }
    }

    private int FindClosestPointIndex(Vector3 point)
    {
        if (GameData.PathPoints == null || GameData.PathPoints.Count == 0)
        {
            return 0;
        }

        int closestIndex = 0;
        float closestDistance = float.MaxValue;
        for (int i = 0; i < GameData.PathPoints.Count; i++)
        {
            float distance = Vector3.Distance(GameData.PathPoints[i], point);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private void AddUniquePoint(Vector3 point)
    {
        Vector3 liftedPoint = new Vector3(point.x, point.y + routeLineHeightOffset, point.z);
        if (routePoints.Count == 0 || Vector3.Distance(routePoints[routePoints.Count - 1], liftedPoint) > 0.25f)
        {
            routePoints.Add(liftedPoint);
        }
    }

    private void ApplyRouteVisuals()
    {
        if (routeLine == null)
        {
            return;
        }

        routeLine.positionCount = routePoints.Count;
        for (int i = 0; i < routePoints.Count; i++)
        {
            routeLine.SetPosition(i, routePoints[i]);
        }

        Color routeColor = GameData.IsDropOffPhase ? dropRouteColor : pickupRouteColor;
        routeLine.startColor = routeColor;
        routeLine.endColor = routeColor;
        routeLine.enabled = routePoints.Count >= 2;

        if (pickupMarker != null)
        {
            pickupMarker.SetActive(!GameData.IsDropOffPhase);
        }

        if (dropMarker != null)
        {
            dropMarker.SetActive(GameData.IsDropOffPhase);
        }
    }

    private void PositionMarker(GameObject marker, Vector3 position, bool enabled)
    {
        if (marker == null)
        {
            return;
        }

        marker.transform.position = new Vector3(position.x, position.y + markerHeightOffset, position.z);
        marker.SetActive(enabled);
    }

    private static Material CreateLineMaterial()
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        return new Material(shader);
    }

    private static Material CreateMarkerMaterial(Color color)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private static void SetLayerRecursively(GameObject root, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            layer = 0;
        }

        ApplyLayer(root.transform, layer);
    }

    private static void ApplyLayer(Transform node, int layer)
    {
        node.gameObject.layer = layer;
        for (int i = 0; i < node.childCount; i++)
        {
            ApplyLayer(node.GetChild(i), layer);
        }
    }
}
