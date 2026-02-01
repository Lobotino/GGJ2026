using System.Collections;
using UnityEngine;

public class BattleFighterAnimator : MonoBehaviour
{
    static readonly int IdleHash = Animator.StringToHash("Idle");

    [Header("Attack Lunge")]
    [SerializeField] float lungeDistance = 0.5f;
    [SerializeField] float lungeDuration = 0.12f;
    [SerializeField] float returnDuration = 0.15f;

    [Header("Hit Shake")]
    [SerializeField] float shakeIntensity = 0.15f;
    [SerializeField] float shakeDuration = 0.3f;
    [SerializeField] int shakeCount = 6;

    [Header("Hit Flash")]
    [SerializeField] float flashDuration = 0.15f;
    [SerializeField] int flashCount = 2;

    static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

    Animator animator;
    SpriteRenderer spriteRenderer;
    MaterialPropertyBlock propertyBlock;
    bool lungedForward;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    public void SwapAnimatorController(RuntimeAnimatorController controller)
    {
        if (controller == null) return;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.runtimeAnimatorController = controller;
    }

    public IEnumerator PlayAndWait(string trigger, bool lunge = false, bool shake = false)
    {
        if (string.IsNullOrEmpty(trigger))
            yield break;

        bool isAttack = lunge;
        bool isHit = shake;

        if (isAttack)
        {
            yield return LungeForward();
            lungedForward = true;
        }

        if (animator != null)
        {
            animator.SetTrigger(trigger);

            // Wait a couple of frames for the trigger to be consumed
            yield return null;
            yield return null;

            // Check if we actually left Idle — if not, the trigger has no matching state
            var check = animator.GetCurrentAnimatorStateInfo(0);
            bool leftIdle = check.shortNameHash != IdleHash || animator.IsInTransition(0);

            if (leftIdle)
            {
                // Wait until we return to Idle
                float returnTimeout = 3f;
                float returnElapsed = 0f;
                while (returnElapsed < returnTimeout)
                {
                    var state = animator.GetCurrentAnimatorStateInfo(0);
                    if (state.shortNameHash == IdleHash && !animator.IsInTransition(0))
                        break;
                    returnElapsed += Time.deltaTime;
                    yield return null;
                }
            }
        }

        if (isHit)
        {
            StartCoroutine(Flash());
            yield return Shake();
        }
    }

    public IEnumerator PlayReturnBack()
    {
        if (!lungedForward)
            yield break;
        lungedForward = false;
        yield return ReturnBack();
    }

    IEnumerator LungeForward()
    {
        // Positive scale = player (lunge right), negative scale = enemy (lunge left)
        float direction = transform.localScale.x >= 0f ? 1f : -1f;
        Vector3 offset = new Vector3(direction * lungeDistance, 0f, 0f);
        Vector3 startPos = transform.localPosition;
        Vector3 targetPos = startPos + offset;

        float elapsed = 0f;
        while (elapsed < lungeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / lungeDuration);
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        transform.localPosition = targetPos;
    }

    IEnumerator ReturnBack()
    {
        float direction = transform.localScale.x >= 0f ? 1f : -1f;
        Vector3 offset = new Vector3(direction * lungeDistance, 0f, 0f);
        Vector3 startPos = transform.localPosition;
        Vector3 targetPos = startPos - offset;

        float elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / returnDuration);
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        transform.localPosition = targetPos;
    }

    IEnumerator Shake()
    {
        Vector3 originalPos = transform.localPosition;
        float stepDuration = shakeDuration / shakeCount;

        for (int i = 0; i < shakeCount; i++)
        {
            Vector2 rnd = Random.insideUnitCircle * shakeIntensity;
            Vector3 shakePos = originalPos + new Vector3(rnd.x, rnd.y, 0f);

            float elapsed = 0f;
            while (elapsed < stepDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / stepDuration;
                transform.localPosition = Vector3.Lerp(transform.localPosition, shakePos, t);
                yield return null;
            }
        }

        transform.localPosition = originalPos;
    }

    IEnumerator Flash()
    {
        if (spriteRenderer == null)
            yield break;

        float halfFlash = flashDuration / 2f;

        for (int i = 0; i < flashCount; i++)
        {
            SetFlashAmount(1f);
            yield return null;

            float elapsed = 0f;
            while (elapsed < halfFlash)
            {
                elapsed += Time.deltaTime;
                float t = 1f - Mathf.Clamp01(elapsed / halfFlash);
                SetFlashAmount(t);
                yield return null;
            }

            SetFlashAmount(0f);
        }
    }

    void SetFlashAmount(float amount)
    {
        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(FlashAmountId, amount);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }
}
