using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    [Header("Sprite Flip")]
    [Tooltip("Enable if the sprite faces left by default")]
    [SerializeField] private bool spriteDefaultFacesLeft = false;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 input;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        input = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
        }
        input = input.normalized;
#else
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        input = new Vector2(x, y).normalized;
#endif
    }

    private void FixedUpdate()
    {
        if (spriteRenderer != null && input.x != 0f)
        {
            bool movingLeft = input.x < 0f;
            spriteRenderer.flipX = spriteDefaultFacesLeft ? !movingLeft : movingLeft;
        }

        Vector2 newPosition = rb.position + input * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }
}
