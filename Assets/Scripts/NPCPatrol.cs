using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NPCPatrol : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] Transform[] waypoints;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float waypointReachThreshold = 0.1f;

    [Header("Sprite Flip")]
    [Tooltip("Enable if the sprite faces left by default")]
    [SerializeField] bool spriteDefaultFacesLeft = false;

    [Header("Pause")]
    [SerializeField] float pauseDuration = 2.5f;

    [Header("Battle")]
    [SerializeField] BattleTransitionManager battleTransitionManager;

    public BattleTransitionManager BattleTransition => battleTransitionManager;
    [SerializeField] AIProfile aiProfile;

    public AIProfile AiProfile => aiProfile;

    [SerializeField] FighterProfile enemyProfileOverride;
    public FighterProfile EnemyProfileOverride => enemyProfileOverride;

    [Header("Available Masks (Battle)")]
    [Tooltip("If empty, uses the fighter profile's available masks")]
    [SerializeField] MaskType[] playerAvailableMasks;
    public MaskType[] PlayerAvailableMasks => playerAvailableMasks;

    [Header("Battle Prefabs (override overworld visuals)")]
    [SerializeField] GameObject playerBattlePrefab;
    [SerializeField] GameObject enemyBattlePrefab;

    [Header("Companions")]
    [SerializeField] MaskType playerCompanionMask = MaskType.None;
    [SerializeField] MaskType enemyCompanionMask = MaskType.None;
    [SerializeField] GameObject playerCompanionPrefab;
    [SerializeField] GameObject enemyCompanionPrefab;

    public MaskType PlayerCompanionMask => playerCompanionMask;
    public MaskType EnemyCompanionMask => enemyCompanionMask;
    public GameObject PlayerBattlePrefab => playerBattlePrefab;
    public GameObject EnemyBattlePrefab => enemyBattlePrefab;
    public GameObject PlayerCompanionPrefab => playerCompanionPrefab;
    public GameObject EnemyCompanionPrefab => enemyCompanionPrefab;

    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    int currentWaypointIndex;
    int direction = 1; // 1 = forward, -1 = backward
    bool paused;
    float resumeTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (waypoints == null || waypoints.Length < 2)
        {
            Debug.Log($"NPCPatrol on {name}: no waypoints — standing still.");
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
        if (spriteRenderer != null && diff.x != 0f)
        {
            bool movingLeft = diff.x < 0f;
            spriteRenderer.flipX = spriteDefaultFacesLeft ? !movingLeft : movingLeft;
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
        Debug.Log($"[NPCPatrol] OnTriggerEnter2D hit by {other.name}, tag={other.tag}, paused={paused}");

        if (paused) return;
        if (!other.CompareTag("Player")) return;

        paused = true;
        resumeTime = Time.time + pauseDuration;

        FaceTarget(other.transform);

        // If a DialogueTrigger with actual dialogue is present, let it handle the battle via post-action
        var dialogueTrigger = GetComponent<DialogueTrigger>();
        if (dialogueTrigger != null && dialogueTrigger.HasDialogue)
        {
            Debug.Log("[NPCPatrol] Deferring to DialogueTrigger (has dialogue)");
            return;
        }

        Debug.Log($"[NPCPatrol] battleTransitionManager={battleTransitionManager}, inBattle={battleTransitionManager?.InBattle}");

        if (battleTransitionManager != null && !battleTransitionManager.InBattle)
        {
            var playerMask = other.GetComponent<CharacterMask>();
            var npcMask = GetComponent<CharacterMask>();
            MaskType pMask = playerMask != null ? playerMask.CurrentMask : MaskType.None;
            MaskType nMask = npcMask != null ? npcMask.CurrentMask : MaskType.None;
            var playerMovement = other.GetComponent<PlayerMovement2D>();
            string available = playerAvailableMasks == null ? "null" : string.Join(", ", playerAvailableMasks);
            Debug.Log($"[NPCPatrol] Starting battle: playerMask={pMask}, npcMask={nMask}, aiProfile={aiProfile}, playerAvailableMasks={available}");
            battleTransitionManager.StartBattle(pMask, nMask, playerMovement, aiProfile,
                playerCompanionMask, enemyCompanionMask,
                playerBattlePrefab, enemyBattlePrefab,
                playerCompanionPrefab, enemyCompanionPrefab,
                playerAvailableMasks,
                enemyProfileOverride);
        }
    }

    void FaceTarget(Transform target)
    {
        if (spriteRenderer == null) return;
        float dx = target.position.x - transform.position.x;
        if (dx == 0f) return;

        bool targetIsLeft = dx < 0f;
        spriteRenderer.flipX = spriteDefaultFacesLeft ? !targetIsLeft : targetIsLeft;
    }
}
