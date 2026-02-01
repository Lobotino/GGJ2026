using System;
using System.Collections;
using UnityEngine;

public class BattleTransitionManager : MonoBehaviour
{
    [SerializeField] ScreenFade screenFade;
    [SerializeField] BattleArena battleArena;
    [SerializeField] CameraFollow2D cameraFollow;
    [SerializeField] BattleController battleController;
    [SerializeField] float fadeDuration = 0.3f;
    [SerializeField] float battleDisplayDuration = 2f;

    bool inBattle;

    public bool InBattle => inBattle;

    public void StartBattle(MaskType playerMask, MaskType enemyMask, PlayerMovement2D playerMovement, AIProfile enemyAIProfile,
        MaskType playerCompanionMask = MaskType.None, MaskType enemyCompanionMask = MaskType.None,
        GameObject playerBattlePrefab = null, GameObject enemyBattlePrefab = null,
        GameObject playerCompanionPrefab = null, GameObject enemyCompanionPrefab = null,
        MaskType[] playerAvailableMasks = null,
        FighterProfile enemyProfileOverride = null,
        Action<bool> onBattleComplete = null)
    {
        if (inBattle) return;
        StartCoroutine(BattleSequence(playerMask, enemyMask, playerMovement, enemyAIProfile,
            playerCompanionMask, enemyCompanionMask,
            playerBattlePrefab, enemyBattlePrefab,
            playerCompanionPrefab, enemyCompanionPrefab,
            playerAvailableMasks, enemyProfileOverride,
            onBattleComplete));
    }

    IEnumerator BattleSequence(MaskType playerMask, MaskType enemyMask, PlayerMovement2D playerMovement, AIProfile enemyAIProfile,
        MaskType playerCompanionMask, MaskType enemyCompanionMask,
        GameObject playerBattlePrefab, GameObject enemyBattlePrefab,
        GameObject playerCompanionPrefab, GameObject enemyCompanionPrefab,
        MaskType[] playerAvailableMasks, FighterProfile enemyProfileOverride,
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

        Debug.Log("[Battle] Fading to black...");
        yield return screenFade.FadeIn(fadeDuration);

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
}
