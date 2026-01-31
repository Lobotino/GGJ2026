using System.Collections;
using UnityEngine;

public class BattleFighterAnimator : MonoBehaviour
{
    static readonly int IdleHash = Animator.StringToHash("Idle");

    Animator animator;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public IEnumerator PlayAndWait(string trigger)
    {
        if (animator == null || string.IsNullOrEmpty(trigger))
            yield break;

        animator.SetTrigger(trigger);

        // Wait one frame for the trigger to be consumed
        yield return null;

        // Wait until we leave Idle (in case transition hasn't started yet)
        float timeout = 1f;
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash != IdleHash && !animator.IsInTransition(0))
                break;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Wait until we return to Idle
        while (true)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash == IdleHash && !animator.IsInTransition(0))
                break;
            yield return null;
        }
    }
}
