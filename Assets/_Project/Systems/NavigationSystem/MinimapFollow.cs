using UnityEngine;

public sealed class MinimapFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float height = 120f;
    [SerializeField] private Vector3 rotationEuler = new Vector3(90f, 0f, 0f);

    public void SetTarget(Transform value)
    {
        target = value;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 pos = target.position;
        transform.position = new Vector3(pos.x, height, pos.z);
        transform.rotation = Quaternion.Euler(rotationEuler);
    }
}
