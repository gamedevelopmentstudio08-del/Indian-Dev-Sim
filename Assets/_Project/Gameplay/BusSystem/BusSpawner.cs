using UnityEngine;

public sealed class BusSpawner : MonoBehaviour
{
    private const string DefaultBusResourcePath = "Runtime/BusPrefab";

    [SerializeField] private string spawnPointName = "BusSpawnPoint";
    [SerializeField] private string busLayerName = "Bus";
    [SerializeField] private float minimumSpawnHeight = 1.5f;

    private GameObject spawnedBus;
    private SimpleBusController spawnedController;

    public SimpleBusController SpawnBusFromResources(Vector3 fallbackPosition, Quaternion fallbackRotation, string overrideSpawnPointName = null)
    {
        GameObject busPrefab = LoadBusPrefab();
        if (busPrefab == null)
        {
            return null;
        }

        return SpawnBus(busPrefab, fallbackPosition, fallbackRotation, overrideSpawnPointName);
    }

    public SimpleBusController SpawnBus(GameObject busPrefab, Vector3 fallbackPosition, Quaternion fallbackRotation, string overrideSpawnPointName = null)
    {
        if (spawnedBus != null)
        {
            return spawnedController;
        }

        if (busPrefab == null)
        {
            Debug.LogError("BusPrefab not found");
            return null;
        }

        string targetSpawnPointName = string.IsNullOrWhiteSpace(overrideSpawnPointName) ? spawnPointName : overrideSpawnPointName;
        Transform spawnPoint = FindSpawnPoint(targetSpawnPointName);
        if (spawnPoint == null)
        {
            GameObject spawnPointObject = new GameObject(targetSpawnPointName);
            spawnPointObject.transform.position = fallbackPosition;
            spawnPointObject.transform.rotation = fallbackRotation;
            spawnPoint = spawnPointObject.transform;
        }

        Vector3 spawnPosition = spawnPoint.position;
        spawnPosition.y = Mathf.Max(spawnPosition.y, minimumSpawnHeight);
        Quaternion spawnRotation = Quaternion.Euler(0f, 0f, 0f);

        spawnedBus = Instantiate(busPrefab, spawnPosition, spawnRotation);
        spawnedBus.name = "PlayerBus";
        spawnedBus.transform.position = spawnPosition;
        spawnedBus.transform.rotation = spawnRotation;

        try
        {
            spawnedBus.tag = "Player";
        }
        catch (UnityException)
        {
            Debug.LogWarning("BusSpawner: Tag 'Player' is missing. Add it if camera or gameplay logic depends on the player tag.");
        }

        SetLayerRecursively(spawnedBus, busLayerName);
        EnsurePhysics(spawnedBus);

        spawnedController = spawnedBus.GetComponent<SimpleBusController>();
        if (spawnedController == null)
        {
            spawnedController = spawnedBus.AddComponent<SimpleBusController>();
        }

        Debug.Log("Bus Spawned Successfully");
        Debug.Log("Spawn Position: " + spawnedBus.transform.position);
        return spawnedController;
    }

    private GameObject LoadBusPrefab()
    {
        GameObject busPrefab = Resources.Load<GameObject>(DefaultBusResourcePath);
        if (busPrefab == null)
        {
            Debug.LogError("BusPrefab not found");
            return null;
        }

        Debug.Log("Bus prefab loaded successfully");
        return busPrefab;
    }

    private Transform FindSpawnPoint(string targetName)
    {
        GameObject existing = GameObject.Find(targetName);
        if (existing != null)
        {
            return existing.transform;
        }

        return null;
    }

    private void EnsurePhysics(GameObject busObject)
    {
        Rigidbody rigidbody = busObject.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = busObject.AddComponent<Rigidbody>();
        }

        rigidbody.mass = 3000f;
        rigidbody.drag = 0f;
        rigidbody.angularDrag = 0.05f;
        rigidbody.useGravity = true;
        rigidbody.isKinematic = false;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rigidbody.maxAngularVelocity = 1.5f;

        Rigidbody[] childRigidbodies = busObject.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < childRigidbodies.Length; i++)
        {
            if (childRigidbodies[i] != null && childRigidbodies[i].gameObject != busObject)
            {
                Object.Destroy(childRigidbodies[i]);
            }
        }

        Collider[] existingColliders = busObject.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < existingColliders.Length; i++)
        {
            Object.Destroy(existingColliders[i]);
        }

        Bounds bounds = CalculateRendererBounds(busObject);
        if (bounds.size.sqrMagnitude > 0.0001f)
        {
            CreateCenterOfMass(busObject, bounds);
            AddMainCapsuleCollider(busObject, bounds);
            return;
        }

        CreateCenterOfMass(busObject, new Bounds(Vector3.zero, new Vector3(2.5f, 3f, 8f)));
        AddMainCapsuleCollider(busObject, new Bounds(Vector3.zero, new Vector3(2.5f, 3f, 8f)));
    }

    private void CreateCenterOfMass(GameObject busObject, Bounds bounds)
    {
        Transform centerOfMass = busObject.transform.Find("CenterOfMass");
        if (centerOfMass == null)
        {
            GameObject comObject = new GameObject("CenterOfMass");
            comObject.transform.SetParent(busObject.transform, false);
            comObject.transform.localPosition = new Vector3(0f, -1f, 0f);
            centerOfMass = comObject.transform;
        }

        Rigidbody rigidbody = busObject.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.centerOfMass = centerOfMass.localPosition;
        }
    }

    private void AddMainCapsuleCollider(GameObject busObject, Bounds bounds)
    {
        CapsuleCollider capsuleCollider = busObject.AddComponent<CapsuleCollider>();
        capsuleCollider.direction = 2;
        capsuleCollider.height = Mathf.Max(bounds.size.z, bounds.size.y);
        capsuleCollider.radius = Mathf.Max(0.5f, Mathf.Min(bounds.size.x, bounds.size.y) * 0.45f);
        capsuleCollider.center = new Vector3(0f, 1f, 0f);
        capsuleCollider.isTrigger = false;
    }

    private static Bounds CalculateRendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            return new Bounds(root.transform.position, new Vector3(2.5f, 3f, 8f));
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private void SetLayerRecursively(GameObject root, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            return;
        }

        ApplyLayer(root.transform, layer);
    }

    private void ApplyLayer(Transform node, int layer)
    {
        node.gameObject.layer = layer;
        for (int i = 0; i < node.childCount; i++)
        {
            ApplyLayer(node.GetChild(i), layer);
        }
    }
}
