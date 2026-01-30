using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    [Header("Grid Movement")]
    [SerializeField] private GridMoveNode2D startNode;
    [SerializeField] private bool snapToStartNode = true;
    [SerializeField] private float arriveThreshold = 0.05f;
    [SerializeField] private float autoFindNodeRadius = 6f;

    [Header("Death & Respawn")]
    [Tooltip("If true, the player will be teleported to the respawn point on battle defeat")]
    [SerializeField] private bool canDie = true;
    [Tooltip("The player will be moved here after dying in battle")]
    [SerializeField] private Transform respawnPoint;

    [Header("Sprite Flip")]
    [Tooltip("Enable if the sprite faces left by default")]
    [SerializeField] private bool spriteDefaultFacesLeft = false;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private GridMoveNode2D currentNode;
    private GridMoveNode2D targetNode;
    private Vector2 targetPosition;
    private bool isMoving;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        currentNode = startNode != null ? startNode : FindNearestNode(autoFindNodeRadius);
        if (currentNode != null && snapToStartNode)
        {
            rb.position = currentNode.Position;
            transform.position = currentNode.Position;
        }
    }

    private void Update()
    {
        if (isMoving)
            return;

        if (currentNode == null)
            currentNode = FindNearestNode(autoFindNodeRadius);

        if (currentNode == null)
            return;

        if (TryGetInputDirection(out Vector2Int direction))
            TryStartMove(direction);
    }

    private void FixedUpdate()
    {
        if (!isMoving)
            return;

        Vector2 newPosition = Vector2.MoveTowards(rb.position, targetPosition, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);

        if ((targetPosition - newPosition).sqrMagnitude <= arriveThreshold * arriveThreshold)
        {
            rb.MovePosition(targetPosition);
            currentNode = targetNode;
            targetNode = null;
            isMoving = false;
        }
    }

    private void TryStartMove(Vector2Int direction)
    {
        GridMoveNode2D next = currentNode.GetNeighbor(direction);
        if (next == null)
            return;

        targetNode = next;
        targetPosition = next.Position;
        isMoving = true;

        if (spriteRenderer != null && direction.x != 0)
        {
            bool movingLeft = direction.x < 0;
            spriteRenderer.flipX = spriteDefaultFacesLeft ? !movingLeft : movingLeft;
        }
    }

    private GridMoveNode2D FindNearestNode(float maxDistance)
    {
#if UNITY_2023_1_OR_NEWER
        GridMoveNode2D[] nodes = FindObjectsByType<GridMoveNode2D>(FindObjectsSortMode.None);
#else
        GridMoveNode2D[] nodes = FindObjectsOfType<GridMoveNode2D>();
#endif

        if (nodes == null || nodes.Length == 0)
            return null;

        Vector2 pos = rb != null ? rb.position : (Vector2)transform.position;
        float maxDistanceSqr = maxDistance * maxDistance;
        GridMoveNode2D best = null;
        float bestDistSqr = float.PositiveInfinity;

        for (int i = 0; i < nodes.Length; i++)
        {
            float distSqr = (nodes[i].Position - pos).sqrMagnitude;
            if (distSqr < bestDistSqr && distSqr <= maxDistanceSqr)
            {
                bestDistSqr = distSqr;
                best = nodes[i];
            }
        }

        return best;
    }

    public bool CanDie => canDie;

    public void TeleportToRespawn()
    {
        if (respawnPoint == null) return;

        Vector2 pos = respawnPoint.position;
        rb.position = pos;
        transform.position = pos;
        isMoving = false;
        targetNode = null;
        currentNode = FindNearestNode(autoFindNodeRadius);
    }

    private bool TryGetInputDirection(out Vector2Int direction)
    {
        direction = Vector2Int.zero;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
                direction = Vector2Int.right;
            else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
                direction = Vector2Int.left;
            else if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
                direction = Vector2Int.up;
            else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
                direction = Vector2Int.down;
        }
#else
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            direction = Vector2Int.right;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            direction = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            direction = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            direction = Vector2Int.down;
#endif

        return direction != Vector2Int.zero;
    }
}
