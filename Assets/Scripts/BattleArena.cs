using UnityEngine;

public class BattleArena : MonoBehaviour
{
    [SerializeField] Transform playerSlot;
    [SerializeField] Transform enemySlot;
    [SerializeField] SpriteRenderer background;
    [SerializeField] MaskData maskData;

    GameObject spawnedPlayer;
    GameObject spawnedEnemy;

    public GameObject SpawnedPlayer => spawnedPlayer;
    public GameObject SpawnedEnemy => spawnedEnemy;

    public void Setup(MaskType playerMask, MaskType enemyMask)
    {
        Cleanup();

        if (maskData == null) return;

        GameObject playerPrefab = maskData.GetPrefab(playerMask);
        GameObject enemyPrefab = maskData.GetPrefab(enemyMask);

        if (playerPrefab != null)
            spawnedPlayer = Instantiate(playerPrefab, playerSlot.position, Quaternion.identity, playerSlot);

        if (enemyPrefab != null)
        {
            spawnedEnemy = Instantiate(enemyPrefab, enemySlot.position, Quaternion.identity, enemySlot);
            // Flip enemy to face the player
            Vector3 s = spawnedEnemy.transform.localScale;
            spawnedEnemy.transform.localScale = new Vector3(-Mathf.Abs(s.x), s.y, s.z);
        }
    }

    /// <summary>
    /// Returns the world-space center of the background sprite.
    /// </summary>
    public Vector3 GetCenter()
    {
        if (background == null) return transform.position;
        return background.bounds.center;
    }

    /// <summary>
    /// Returns the orthographic size needed to fit the background sprite on screen,
    /// or 0 if no background is assigned.
    /// </summary>
    public float GetCameraSize(float cameraAspect)
    {
        if (background == null || background.sprite == null) return 0f;

        // Use renderer bounds — accounts for scale and position
        float worldHeight = background.bounds.size.y;
        float worldWidth = background.bounds.size.x;

        float sizeByHeight = worldHeight * 0.5f;
        float sizeByWidth = worldWidth / (2f * cameraAspect);

        return Mathf.Max(sizeByHeight, sizeByWidth);
    }

    public void Cleanup()
    {
        if (spawnedPlayer != null)
        {
            Destroy(spawnedPlayer);
            spawnedPlayer = null;
        }
        if (spawnedEnemy != null)
        {
            Destroy(spawnedEnemy);
            spawnedEnemy = null;
        }
    }

    public void SwapFighterSprite(bool isPlayer, MaskType newMask)
    {
        if (maskData == null) return;
        GameObject prefab = maskData.GetPrefab(newMask);
        if (prefab == null) return;

        if (isPlayer)
        {
            if (spawnedPlayer != null)
                Destroy(spawnedPlayer);
            spawnedPlayer = Instantiate(prefab, playerSlot.position, Quaternion.identity, playerSlot);
        }
        else
        {
            if (spawnedEnemy != null)
                Destroy(spawnedEnemy);
            spawnedEnemy = Instantiate(prefab, enemySlot.position, Quaternion.identity, enemySlot);
            Vector3 s = spawnedEnemy.transform.localScale;
            spawnedEnemy.transform.localScale = new Vector3(-Mathf.Abs(s.x), s.y, s.z);
        }
    }
}
