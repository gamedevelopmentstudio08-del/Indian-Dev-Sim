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

    private Rigidbody rb;
    private Vector3 nextPosition;
    private Quaternion nextRotation;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (moveOnXAxis)
        {
            float spawnX = moveDirection > 0 ? startX : endX;
            nextPosition = new Vector3(spawnX, transform.position.y, laneZ);
            nextRotation = Quaternion.Euler(0f, moveDirection > 0 ? 90f : -90f, 0f);
        }
        else
        {
            float spawnZ = moveDirection > 0 ? startZ : endZ;
            nextPosition = new Vector3(laneX, transform.position.y, spawnZ);
            nextRotation = Quaternion.Euler(0f, moveDirection > 0 ? 0f : 180f, 0f);
        }

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.position = nextPosition;
            rb.rotation = nextRotation;
        }
        else
        {
            transform.position = nextPosition;
            transform.rotation = nextRotation;
        }
    }

    private void Update()
    {
        if (rb == null)
        {
            MoveVehicle(Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            MoveVehicle(Time.fixedDeltaTime);
        }
    }

    private void MoveVehicle(float deltaTime)
    {
        Vector3 currentPosition = rb != null ? rb.position : transform.position;
        Quaternion currentRotation = rb != null ? rb.rotation : transform.rotation;

        if (moveOnXAxis)
        {
            if (!ShouldStopForTrafficLight())
            {
                currentPosition += Vector3.right * moveDirection * speed * deltaTime;
            }

            if (moveDirection > 0 && currentPosition.x > endX)
            {
                currentPosition = new Vector3(startX, currentPosition.y, laneZ);
            }
            else if (moveDirection < 0 && currentPosition.x < startX)
            {
                currentPosition = new Vector3(endX, currentPosition.y, laneZ);
            }
        }
        else
        {
            if (!ShouldStopForTrafficLight())
            {
                currentPosition += Vector3.forward * moveDirection * speed * deltaTime;
            }

            if (moveDirection > 0 && currentPosition.z > endZ)
            {
                currentPosition = new Vector3(laneX, currentPosition.y, startZ);
            }
            else if (moveDirection < 0 && currentPosition.z < startZ)
            {
                currentPosition = new Vector3(laneX, currentPosition.y, endZ);
            }
        }

        nextPosition = currentPosition;
        nextRotation = currentRotation;

        if (rb != null)
        {
            rb.MovePosition(nextPosition);
            rb.MoveRotation(nextRotation);
        }
        else
        {
            transform.position = nextPosition;
            transform.rotation = nextRotation;
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
        Vector3 position = rb != null ? rb.position : transform.position;

        foreach (float intersectionZ in mainRoadIntersections)
        {
            float stopLineZ = intersectionZ - stopLineOffset * moveDirection;
            float distanceToStopLine = (stopLineZ - position.z) * moveDirection;

            if (distanceToStopLine > 0f && distanceToStopLine < stopDistance)
            {
                return !IsMainRoadGreen();
            }
        }

        return false;
    }

    private bool ShouldStopOnXAxis()
    {
        Vector3 position = rb != null ? rb.position : transform.position;

        foreach (float intersectionX in crossRoadIntersections)
        {
            float stopLineX = intersectionX - stopLineOffset * moveDirection;
            float distanceToStopLine = (stopLineX - position.x) * moveDirection;

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
