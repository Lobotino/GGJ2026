using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NPCPatrol : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] Transform[] waypoints;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float waypointReachThreshold = 0.1f;

    [Header("Pause")]
    [SerializeField] float pauseDuration = 2.5f;

    Rigidbody2D rb;
    int currentWaypointIndex;
    int direction = 1; // 1 = forward, -1 = backward
    bool paused;
    float resumeTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (waypoints == null || waypoints.Length < 2)
        {
            Debug.LogWarning($"NPCPatrol on {name}: need at least 2 waypoints.");
            enabled = false;
        }
    }

    void FixedUpdate()
    {
        if (paused)
        {
            if (Time.time >= resumeTime)
                paused = false;
            else
                return;
        }

        if (waypoints == null || waypoints.Length < 2) return;

        Transform target = waypoints[currentWaypointIndex];
        Vector2 pos = rb.position;
        Vector2 dest = target.position;
        Vector2 diff = dest - pos;

        // Flip sprite to face movement direction
        if (diff.x != 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Sign(diff.x) * Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        if (diff.sqrMagnitude < waypointReachThreshold * waypointReachThreshold)
        {
            AdvanceWaypoint();
            return;
        }

        Vector2 move = Vector2.MoveTowards(pos, dest, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(move);
    }

    void AdvanceWaypoint()
    {
        currentWaypointIndex += direction;

        if (currentWaypointIndex >= waypoints.Length)
        {
            direction = -1;
            currentWaypointIndex = waypoints.Length - 2;
        }
        else if (currentWaypointIndex < 0)
        {
            direction = 1;
            currentWaypointIndex = 1;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (paused) return;
        if (!other.CompareTag("Player")) return;

        paused = true;
        resumeTime = Time.time + pauseDuration;
    }
}
