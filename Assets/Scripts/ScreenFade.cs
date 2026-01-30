using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFade : MonoBehaviour
{
    CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>Fade to black (alpha 0 → 1).</summary>
    public Coroutine FadeIn(float duration)
    {
        return StartCoroutine(Fade(0f, 1f, duration));
    }

    /// <summary>Fade from black (alpha 1 → 0).</summary>
    public Coroutine FadeOut(float duration)
    {
        return StartCoroutine(Fade(1f, 0f, duration));
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        canvasGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
