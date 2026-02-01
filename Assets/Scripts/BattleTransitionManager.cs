using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BattleTransitionManager : MonoBehaviour
{
    [SerializeField] ScreenFade screenFade;
    [SerializeField] BattleArena battleArena;
    [SerializeField] CameraFollow2D cameraFollow;
    [SerializeField] BattleController battleController;
    [SerializeField] float fadeDuration = 0.3f;
    [SerializeField] float fadeHoldDuration = 0.15f;
    [SerializeField] float battleDisplayDuration = 2f;
    [Header("Battle Intro Image")]
    [Tooltip("Optional full-screen image shown after fade-in/out, before battle starts.")]
    [SerializeField] Image battleIntroImage;
    [SerializeField] float battleIntroImageDuration = 1f;
    [SerializeField] float battleIntroSlideDuration = 0.6f;
    [SerializeField] Vector2 battleIntroOffscreenDirection = Vector2.up;

    bool inBattle;
    bool preFadedToBlack;

    public bool InBattle => inBattle;

    void Awake()
    {
        if (battleIntroImage != null)
            battleIntroImage.enabled = false;
    }

    public void StartBattle(MaskType playerMask, MaskType enemyMask, PlayerMovement2D playerMovement, AIProfile enemyAIProfile,
        MaskType playerCompanionMask = MaskType.None, MaskType enemyCompanionMask = MaskType.None,
        GameObject playerBattlePrefab = null, GameObject enemyBattlePrefab = null,
        GameObject playerCompanionPrefab = null, GameObject enemyCompanionPrefab = null,
        MaskType[] playerAvailableMasks = null,
        FighterProfile enemyProfileOverride = null,
        Sprite battleIntroSprite = null,
        Action<bool> onBattleComplete = null)
    {
        if (inBattle) return;
        StartCoroutine(BattleSequence(playerMask, enemyMask, playerMovement, enemyAIProfile,
            playerCompanionMask, enemyCompanionMask,
            playerBattlePrefab, enemyBattlePrefab,
            playerCompanionPrefab, enemyCompanionPrefab,
            playerAvailableMasks, enemyProfileOverride, battleIntroSprite,
            onBattleComplete));
    }

    IEnumerator BattleSequence(MaskType playerMask, MaskType enemyMask, PlayerMovement2D playerMovement, AIProfile enemyAIProfile,
        MaskType playerCompanionMask, MaskType enemyCompanionMask,
        GameObject playerBattlePrefab, GameObject enemyBattlePrefab,
        GameObject playerCompanionPrefab, GameObject enemyCompanionPrefab,
        MaskType[] playerAvailableMasks, FighterProfile enemyProfileOverride,
        Sprite battleIntroSprite,
        Action<bool> onBattleComplete)
    {
        inBattle = true;

        if (screenFade == null) { Debug.LogError("[Battle] screenFade is not assigned!"); yield break; }
        if (battleArena == null) { Debug.LogError("[Battle] battleArena is not assigned!"); yield break; }
        if (cameraFollow == null) { Debug.LogError("[Battle] cameraFollow is not assigned!"); yield break; }

        // Freeze player input
        if (playerMovement != null)
            playerMovement.enabled = false;

        // Remember camera state to restore later
        Camera cam = cameraFollow.GetComponent<Camera>();
        if (cam == null) { Debug.LogError("[Battle] No Camera component on cameraFollow object!"); yield break; }
        Transform camTransform = cameraFollow.transform;
        Vector3 originalCamPos = camTransform.position;
        float originalSize = cam.orthographicSize;

        if (!preFadedToBlack)
        {
            Debug.Log("[Battle] Fading to black...");
            yield return screenFade.FadeIn(fadeDuration);
            yield return HoldOnBlack();
        }
        else
        {
            preFadedToBlack = false;
        }

        // Move camera to center of arena background and fit to it
        cameraFollow.enabled = false;
        Vector3 arenaCenter = battleArena.GetCenter();
        camTransform.position = new Vector3(arenaCenter.x, arenaCenter.y, camTransform.position.z);

        float arenaSize = battleArena.GetCameraSize(cam.aspect);
        if (arenaSize > 0f)
            cam.orthographicSize = arenaSize;

        // Setup arena characters
        battleArena.Setup(playerBattlePrefab, enemyBattlePrefab, playerCompanionPrefab, enemyCompanionPrefab,
            playerCompanionMask, enemyCompanionMask);

        Debug.Log("[Battle] Revealing arena...");
        yield return screenFade.FadeOut(fadeDuration);

        if (battleIntroSprite != null)
            yield return ShowBattleIntro(battleIntroSprite);

        if (battleController != null)
        {
            Debug.Log("[Battle] Running battle...");
            yield return battleController.RunBattle(playerMask, enemyMask, enemyAIProfile, playerCompanionMask, enemyCompanionMask, playerAvailableMasks, enemyProfileOverride);
        }
        else
        {
            Debug.Log("[Battle] Waiting...");
            yield return new WaitForSeconds(battleDisplayDuration);
        }

        Debug.Log("[Battle] Fading to black again...");
        yield return screenFade.FadeIn(fadeDuration);
        yield return HoldOnBlack();

        // Remove spawned arena characters
        battleArena.Cleanup();

        // Handle death: teleport player to respawn point while screen is still black
        bool playerDied = battleController != null && battleController.PlayerLost;
        if (playerDied && playerMovement != null && playerMovement.CanDie)
        {
            Debug.Log("[Battle] Player died — respawning...");
            playerMovement.TeleportToRespawn();
        }

        // Return camera
        cam.orthographicSize = originalSize;
        cameraFollow.enabled = true;
        // Let CameraFollow snap to (possibly new) player position
        camTransform.position = new Vector3(
            cameraFollow.transform.position.x,
            cameraFollow.transform.position.y,
            camTransform.position.z);

        Debug.Log("[Battle] Returning to overworld...");
        yield return screenFade.FadeOut(fadeDuration);

        // Unfreeze player
        if (playerMovement != null)
            playerMovement.enabled = true;

        inBattle = false;

        bool playerWon = battleController != null && !battleController.PlayerLost;
        onBattleComplete?.Invoke(playerWon);

        Debug.Log("[Battle] Done.");
    }

    IEnumerator ShowBattleIntro(Sprite sprite)
    {
        if (battleIntroImage == null)
        {
            Debug.LogWarning("[Battle] battleIntroImage is not assigned; skipping intro image.");
            yield break;
        }

        if (battleIntroImageDuration <= 0f)
            yield break;

        bool wasActive = battleIntroImage.gameObject.activeSelf;
        battleIntroImage.gameObject.SetActive(true);
        battleIntroImage.sprite = sprite;
        battleIntroImage.enabled = true;

        yield return new WaitForSeconds(battleIntroImageDuration);

        battleIntroImage.enabled = false;
        if (!wasActive)
            battleIntroImage.gameObject.SetActive(false);
    }

    public IEnumerator PlayDialogueBattleIntro(Sprite sprite, Action onBeforeFade = null)
    {
        if (battleIntroImage == null || screenFade == null)
        {
            Debug.LogWarning("[Battle] battleIntroImage or screenFade is not assigned; skipping dialogue intro.");
            yield break;
        }

        if (sprite == null)
            yield break;

        RectTransform rect = battleIntroImage.rectTransform;
        bool wasActive = battleIntroImage.gameObject.activeSelf;

        battleIntroImage.gameObject.SetActive(true);
        battleIntroImage.sprite = sprite;
        battleIntroImage.enabled = true;
        battleIntroImage.transform.SetAsLastSibling();

        onBeforeFade?.Invoke();

        Vector2 targetPos = Vector2.zero;
        Vector2 offscreenPos = GetOffscreenPosition(rect, battleIntroOffscreenDirection);
        rect.anchoredPosition = offscreenPos;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, battleIntroSlideDuration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(offscreenPos, targetPos, elapsed / duration);
            yield return null;
        }
        rect.anchoredPosition = targetPos;

        yield return screenFade.FadeIn(fadeDuration);
        yield return HoldOnBlack();
        battleIntroImage.enabled = false;
        if (!wasActive)
            battleIntroImage.gameObject.SetActive(false);

        preFadedToBlack = true;
    }

    Vector2 GetOffscreenPosition(RectTransform rect, Vector2 direction)
    {
        Vector2 dir = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.up;
        float distance = 0f;
        RectTransform parentRect = rect != null ? rect.parent as RectTransform : null;
        if (parentRect != null)
        {
            float width = parentRect.rect.width;
            float height = parentRect.rect.height;
            distance = Mathf.Abs(dir.x) * width + Mathf.Abs(dir.y) * height;
        }
        if (distance <= 0f)
            distance = Mathf.Max(Screen.width, Screen.height);

        return dir * (distance + 100f);
    }

    IEnumerator HoldOnBlack()
    {
        if (fadeHoldDuration <= 0f)
            yield break;

        yield return new WaitForSeconds(fadeHoldDuration);
    }
}
