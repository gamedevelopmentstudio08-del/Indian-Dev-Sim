using UnityEngine;

public class DemoVehicleMover : MonoBehaviour
{
    public float speed = 10f;
    public float startZ = -480f;
    public float endZ = 480f;
    public float laneX = 2.6f;
    public bool moveOnXAxis = false;
    public int moveDirection = 1;
    public float laneZ = 25f;
    public float startX = -480f;
    public float endX = 480f;
    public bool obeyTrafficLights = true;
    public float stopDistance = 12f;
    public float stopLineOffset = 5f;

    private readonly float[] mainRoadIntersections = { -300f, 0f, 25f, 300f };
    private readonly float[] crossRoadIntersections = { -250f, 0f, 250f };
    private const float GreenDuration = 12f;
    private const float YellowDuration = 3f;
    private const float RedDuration = 12f;

    private void Start()
    {
        if (moveOnXAxis)
        {
            float spawnX = moveDirection > 0 ? startX : endX;
            transform.position = new Vector3(spawnX, transform.position.y, laneZ);
            transform.rotation = Quaternion.Euler(0f, moveDirection > 0 ? 90f : -90f, 0f);
        }
        else
        {
            float spawnZ = moveDirection > 0 ? startZ : endZ;
            transform.position = new Vector3(laneX, transform.position.y, spawnZ);
            transform.rotation = Quaternion.Euler(0f, moveDirection > 0 ? 0f : 180f, 0f);
        }
    }

    private void Update()
    {
        if (moveOnXAxis)
        {
            if (!ShouldStopForTrafficLight())
            {
                transform.position += Vector3.right * moveDirection * speed * Time.deltaTime;
            }

            if (moveDirection > 0 && transform.position.x > endX)
            {
                transform.position = new Vector3(startX, transform.position.y, laneZ);
            }
            else if (moveDirection < 0 && transform.position.x < startX)
            {
                transform.position = new Vector3(endX, transform.position.y, laneZ);
            }
        }
        else
        {
            if (!ShouldStopForTrafficLight())
            {
                transform.position += Vector3.forward * moveDirection * speed * Time.deltaTime;
            }

            if (moveDirection > 0 && transform.position.z > endZ)
            {
                transform.position = new Vector3(laneX, transform.position.y, startZ);
            }
            else if (moveDirection < 0 && transform.position.z < startZ)
            {
                transform.position = new Vector3(laneX, transform.position.y, endZ);
            }
        }
    }

    private bool ShouldStopForTrafficLight()
    {
        if (!obeyTrafficLights)
        {
            return false;
        }

        if (moveOnXAxis)
        {
            return ShouldStopOnXAxis();
        }

        return ShouldStopOnZAxis();
    }

    private bool ShouldStopOnZAxis()
    {
        foreach (float intersectionZ in mainRoadIntersections)
        {
            float stopLineZ = intersectionZ - stopLineOffset * moveDirection;
            float distanceToStopLine = (stopLineZ - transform.position.z) * moveDirection;

            if (distanceToStopLine > 0f && distanceToStopLine < stopDistance)
            {
                return !IsMainRoadGreen();
            }
        }

        return false;
    }

    private bool ShouldStopOnXAxis()
    {
        foreach (float intersectionX in crossRoadIntersections)
        {
            float stopLineX = intersectionX - stopLineOffset * moveDirection;
            float distanceToStopLine = (stopLineX - transform.position.x) * moveDirection;

            if (distanceToStopLine > 0f && distanceToStopLine < stopDistance)
            {
                return !IsCrossRoadGreen();
            }
        }

        return false;
    }

    private bool IsMainRoadGreen()
    {
        return IsGreenForOffset(0f);
    }

    private bool IsCrossRoadGreen()
    {
        return IsGreenForOffset(6.5f);
    }

    private bool IsGreenForOffset(float startOffset)
    {
        float totalTime = GreenDuration + YellowDuration + RedDuration;
        float timer = (Time.time + startOffset) % totalTime;
        return timer < GreenDuration;
    }
}
