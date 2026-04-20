using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class AutoBusReplacer : MonoBehaviour
{
    [Header("Optional Bus Prefab")]
    [SerializeField] private string resourcesBusPrefabName = "Bus";

    [Header("Procedural Bus")]
    [SerializeField] private Vector3 bodyScale = new Vector3(2.5f, 2f, 6f);
    [SerializeField] private Vector3 bodyLocalPosition = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private Vector3 wheelScale = new Vector3(0.7f, 0.32f, 0.7f);
    [SerializeField] private float wheelHeight = 0.45f;
    [SerializeField] private float wheelHalfTrack = 1.35f;
    [SerializeField] private float frontWheelZ = 2.15f;
    [SerializeField] private float rearWheelZ = -2.15f;

    [Header("Physics")]
    [SerializeField] private float busMass = 3000f;
    [SerializeField] private float busDrag = 0.5f;
    [SerializeField] private float busAngularDrag = 2f;
    [SerializeField] private Vector3 centerOfMass = new Vector3(0f, -1.2f, 0f);
    [SerializeField] private float uprightForce = 10f;
    [SerializeField] private float angularDamping = 2.5f;

    private const string VisualRootName = "AutoBusVisual";

    private GameObject _target;
    private Rigidbody _targetRb;

    private void Start()
    {
        _target = FindPlayerObject();
        if (_target == null)
        {
            Debug.LogWarning("AutoBusReplacer: Player object not found.");
            return;
        }

        PrepareTarget(_target);
        ReplaceVisualModel(_target);
        TuneMovementSensitivity(_target);
    }

    private void FixedUpdate()
    {
        if (_targetRb == null)
        {
            return;
        }

        StabilizeBody(_targetRb, _target.transform);
    }

    private GameObject FindPlayerObject()
    {
        if (IsLikelyPlayer(gameObject))
        {
            return gameObject;
        }

        GameObject taggedPlayer = FindByPlayerTag();
        if (taggedPlayer != null)
        {
            return taggedPlayer;
        }

        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            GameObject candidate = transforms[i].gameObject;
            if (IsLikelyPlayer(candidate))
            {
                return candidate;
            }
        }

        MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            GameObject candidate = renderers[i].gameObject;
            MeshFilter meshFilter = candidate.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                continue;
            }

            if (meshFilter.sharedMesh.name.ToLowerInvariant().Contains("cube"))
            {
                return candidate;
            }
        }

        return null;
    }

    private static GameObject FindByPlayerTag()
    {
        try
        {
            return GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
            return null;
        }
    }

    private static bool IsLikelyPlayer(GameObject candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        string lowerName = candidate.name.ToLowerInvariant();
        if (lowerName.Contains("bus") || lowerName.Contains("player"))
        {
            return true;
        }

        try
        {
            return candidate.CompareTag("Player");
        }
        catch (UnityException)
        {
            return false;
        }
    }

    private void PrepareTarget(GameObject target)
    {
        _targetRb = target.GetComponent<Rigidbody>();
        if (_targetRb == null)
        {
            _targetRb = target.AddComponent<Rigidbody>();
        }

        _targetRb.mass = busMass;
        _targetRb.drag = busDrag;
        _targetRb.angularDrag = busAngularDrag;
        _targetRb.interpolation = RigidbodyInterpolation.Interpolate;
        _targetRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _targetRb.centerOfMass = centerOfMass;
        _targetRb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void ReplaceVisualModel(GameObject target)
    {
        MeshRenderer cubeRenderer = target.GetComponent<MeshRenderer>();
        Transform existingVisual = target.transform.Find(VisualRootName);
        if (existingVisual != null)
        {
            Destroy(existingVisual.gameObject);
        }

        GameObject visualRoot = new GameObject(VisualRootName);
        visualRoot.transform.SetParent(target.transform, false);

        GameObject prefab = Resources.Load<GameObject>(resourcesBusPrefabName);
        if (prefab != null)
        {
            GameObject visual = Instantiate(prefab, visualRoot.transform);
            visual.name = "BusModel";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            DisableVisualColliders(visual);
            FinalizeVisualSwap(cubeRenderer, visualRoot);
            return;
        }

        CreateProceduralBus(visualRoot.transform);
        FinalizeVisualSwap(cubeRenderer, visualRoot);
    }

    private void CreateProceduralBus(Transform parent)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(parent, false);
        body.transform.localPosition = bodyLocalPosition;
        body.transform.localScale = bodyScale;
        body.GetComponent<MeshRenderer>().material.color = new Color(0.92f, 0.55f, 0.12f);
        Destroy(body.GetComponent<Collider>());

        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.name = "Cabin";
        cabin.transform.SetParent(parent, false);
        cabin.transform.localPosition = bodyLocalPosition + new Vector3(0f, 0.35f, 1.2f);
        cabin.transform.localScale = new Vector3(2.3f, 1.35f, 2.2f);
        cabin.GetComponent<MeshRenderer>().material.color = new Color(0.80f, 0.88f, 0.96f);
        Destroy(cabin.GetComponent<Collider>());

        CreateWheel(parent, "FrontLeftWheel", new Vector3(-wheelHalfTrack, wheelHeight, frontWheelZ));
        CreateWheel(parent, "FrontRightWheel", new Vector3(wheelHalfTrack, wheelHeight, frontWheelZ));
        CreateWheel(parent, "RearLeftWheel", new Vector3(-wheelHalfTrack, wheelHeight, rearWheelZ));
        CreateWheel(parent, "RearRightWheel", new Vector3(wheelHalfTrack, wheelHeight, rearWheelZ));
    }

    private void CreateWheel(Transform parent, string wheelName, Vector3 localPosition)
    {
        GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = wheelName;
        wheel.transform.SetParent(parent, false);
        wheel.transform.localPosition = localPosition;
        wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        wheel.transform.localScale = wheelScale;
        wheel.GetComponent<MeshRenderer>().material.color = new Color(0.08f, 0.08f, 0.08f);
        Destroy(wheel.GetComponent<Collider>());
    }

    private static void DisableVisualColliders(GameObject visualRoot)
    {
        Collider[] colliders = visualRoot.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    private static void FinalizeVisualSwap(MeshRenderer originalRenderer, GameObject visualRoot)
    {
        bool hasVisibleRenderer = visualRoot.GetComponentsInChildren<MeshRenderer>(true).Length > 0;
        if (hasVisibleRenderer)
        {
            if (originalRenderer != null)
            {
                originalRenderer.enabled = false;
            }

            return;
        }

        if (originalRenderer != null)
        {
            originalRenderer.enabled = true;
        }
    }

    private void TuneMovementSensitivity(GameObject target)
    {
        MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour == this)
            {
                continue;
            }

            ScaleFloatField(behaviour, "rotationSpeed", 0.78f, 18f);
            ScaleFloatField(behaviour, "wheelSteerAngle", 0.72f, 7f);
            ScaleFloatField(behaviour, "highSpeedWheelSteerAngle", 0.72f, 2.5f);
            ScaleFloatField(behaviour, "steeringStrength", 0.72f, 40f);
            ScaleFloatField(behaviour, "steeringResponse", 0.85f, 7f);
            ScaleFloatField(behaviour, "steeringAtMaxSpeed", 1.15f, 0.45f);
            SetFloatFieldIfExists(behaviour, "linearDrag", Mathf.Max(GetFloatFieldOrDefault(behaviour, "linearDrag", busDrag), busDrag));
            SetFloatFieldIfExists(behaviour, "yawStability", Mathf.Max(GetFloatFieldOrDefault(behaviour, "yawStability", 3f), 4f));
            SetFloatFieldIfExists(behaviour, "highSpeedSteeringDamping", Mathf.Max(GetFloatFieldOrDefault(behaviour, "highSpeedSteeringDamping", 4.5f), 5.5f));
            SetVector3FieldIfExists(behaviour, "centerOfMassOffset", centerOfMass);
        }
    }

    private static void ScaleFloatField(Object target, string fieldName, float multiplier, float maxValue)
    {
        FieldInfo field = FindField(target, fieldName);
        if (field == null || field.FieldType != typeof(float))
        {
            return;
        }

        float current = (float)field.GetValue(target);
        float updated = Mathf.Min(current * multiplier, maxValue);
        field.SetValue(target, updated);
    }

    private static void SetFloatFieldIfExists(Object target, string fieldName, float value)
    {
        FieldInfo field = FindField(target, fieldName);
        if (field != null && field.FieldType == typeof(float))
        {
            field.SetValue(target, value);
        }
    }

    private static void SetVector3FieldIfExists(Object target, string fieldName, Vector3 value)
    {
        FieldInfo field = FindField(target, fieldName);
        if (field != null && field.FieldType == typeof(Vector3))
        {
            field.SetValue(target, value);
        }
    }

    private static float GetFloatFieldOrDefault(Object target, string fieldName, float fallback)
    {
        FieldInfo field = FindField(target, fieldName);
        if (field != null && field.FieldType == typeof(float))
        {
            return (float)field.GetValue(target);
        }

        return fallback;
    }

    private static FieldInfo FindField(Object target, string fieldName)
    {
        return target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }

    private void StabilizeBody(Rigidbody body, Transform targetTransform)
    {
        Vector3 localAngularVelocity = targetTransform.InverseTransformDirection(body.angularVelocity);
        Vector3 dampingTorque = new Vector3(-localAngularVelocity.x * angularDamping, 0f, -localAngularVelocity.z * angularDamping);
        body.AddRelativeTorque(dampingTorque, ForceMode.Acceleration);

        float tiltAmount = Vector3.Angle(targetTransform.up, Vector3.up);
        if (tiltAmount > 1f)
        {
            Vector3 correctiveAxis = Vector3.Cross(targetTransform.up, Vector3.up);
            body.AddTorque(correctiveAxis * (uprightForce * Mathf.Clamp01(tiltAmount / 30f)), ForceMode.Acceleration);
        }
    }
}
