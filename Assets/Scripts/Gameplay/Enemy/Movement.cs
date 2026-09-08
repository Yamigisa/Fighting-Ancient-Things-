using UnityEngine;
using System.Collections.Generic;

public class Movement : MonoBehaviour
{
    private float moveSpeed;
    private Vector2 moveDirection;
    public Vector2 MoveDirection => moveDirection;
    private Rigidbody2D body;
    private Transform destination;
    private bool isBlocked;
    private readonly List<Vector2> waypoints = new();
    private int waypointIndex;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    public void Initialize(float newMoveSpeed, Vector2 initialDirection)
    {
        moveSpeed = Mathf.Max(0f, newMoveSpeed);
        SetDirection(initialDirection);
    }

    public void SetDirection(Vector2 newDirection)
    {
        moveDirection = newDirection.sqrMagnitude > 0.0001f ? newDirection.normalized : Vector2.zero;
    }

    public void SetDestination(Transform newDestination)
    {
        if (waypoints.Count > 0)
            return;

        destination = newDestination;
    }

    public void SetWaypoints(params Vector2[] newWaypoints)
    {
        waypoints.Clear();
        foreach (Vector2 waypoint in newWaypoints)
            waypoints.Add(waypoint);

        waypointIndex = 0;
        destination = null;
    }

    public void SetBlocked(bool blocked)
    {
        isBlocked = blocked;
    }

    private void FixedUpdate()
    {
        if (body == null || isBlocked || moveSpeed <= 0f)
            return;

        Vector2 targetPosition;
        if (waypointIndex < waypoints.Count)
        {
            targetPosition = waypoints[waypointIndex];
            if (Vector2.Distance(body.position, targetPosition) <= 0.01f)
            {
                waypointIndex++;
                if (waypointIndex >= waypoints.Count)
                    return;

                targetPosition = waypoints[waypointIndex];
            }
        }
        else if (destination != null)
        {
            targetPosition = destination.position;
        }
        else
        {
            targetPosition = body.position + moveDirection;
        }

        SetDirection(targetPosition - body.position);

        if (moveDirection == Vector2.zero)
            return;

        body.MovePosition(Vector2.MoveTowards(body.position, targetPosition, moveSpeed * Time.fixedDeltaTime));
    }
}
