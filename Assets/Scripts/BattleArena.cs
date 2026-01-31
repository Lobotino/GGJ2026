using UnityEngine;

public class BattleArena : MonoBehaviour
{
    [SerializeField] Transform playerSlot;
    [SerializeField] Transform enemySlot;
    [SerializeField] Transform playerCompanionSlot;
    [SerializeField] Transform enemyCompanionSlot;
    [SerializeField] SpriteRenderer background;
    [SerializeField] MaskData maskData;

    [Header("Battle Prefabs (override overworld prefabs)")]
    [SerializeField] GameObject defaultPlayerBattlePrefab;
    [SerializeField] GameObject defaultEnemyBattlePrefab;
    [SerializeField] GameObject defaultPlayerCompanionPrefab;
    [SerializeField] GameObject defaultEnemyCompanionPrefab;
    [SerializeField] float companionScale = 0.6f;

    GameObject spawnedPlayer;
    GameObject spawnedEnemy;
    GameObject spawnedPlayerCompanion;
    GameObject spawnedEnemyCompanion;

    BattleFighterAnimator playerAnimator;
    BattleFighterAnimator enemyAnimator;
    BattleFighterAnimator playerCompanionAnimator;
    BattleFighterAnimator enemyCompanionAnimator;

    public GameObject SpawnedPlayer => spawnedPlayer;
    public GameObject SpawnedEnemy => spawnedEnemy;
    public BattleFighterAnimator PlayerAnimator => playerAnimator;
    public BattleFighterAnimator EnemyAnimator => enemyAnimator;
    public BattleFighterAnimator PlayerCompanionAnimator => playerCompanionAnimator;
    public BattleFighterAnimator EnemyCompanionAnimator => enemyCompanionAnimator;

    public void Setup(MaskType playerMask, MaskType enemyMask)
    {
        Setup(null, null, null, null, MaskType.None, MaskType.None);
    }

    public void Setup(GameObject playerBattlePrefab, GameObject enemyBattlePrefab,
        GameObject playerCompPrefab, GameObject enemyCompPrefab,
        MaskType playerCompanionMask = MaskType.None, MaskType enemyCompanionMask = MaskType.None)
    {
        Cleanup();

        // Fighters: explicit prefab > default on arena
        GameObject pPrefab = playerBattlePrefab != null ? playerBattlePrefab : defaultPlayerBattlePrefab;
        GameObject ePrefab = enemyBattlePrefab != null ? enemyBattlePrefab : defaultEnemyBattlePrefab;

        if (pPrefab != null && playerSlot != null)
        {
            spawnedPlayer = Instantiate(pPrefab, playerSlot.position, Quaternion.identity, playerSlot);
            playerAnimator = EnsureFighterAnimator(spawnedPlayer);
        }

        if (ePrefab != null && enemySlot != null)
        {
            spawnedEnemy = Instantiate(ePrefab, enemySlot.position, Quaternion.identity, enemySlot);
            Vector3 s = spawnedEnemy.transform.localScale;
            spawnedEnemy.transform.localScale = new Vector3(-Mathf.Abs(s.x), s.y, s.z);
            enemyAnimator = EnsureFighterAnimator(spawnedEnemy);
        }

        // Companions: explicit prefab > default on arena > resolve from MaskData.battlePrefab
        GameObject pComp = ResolveCompanionPrefab(playerCompPrefab, defaultPlayerCompanionPrefab, playerCompanionMask);
        GameObject eComp = ResolveCompanionPrefab(enemyCompPrefab, defaultEnemyCompanionPrefab, enemyCompanionMask);

        SpawnCompanion(pComp, playerCompanionSlot, false, ref spawnedPlayerCompanion, ref playerCompanionAnimator);
        SpawnCompanion(eComp, enemyCompanionSlot, true, ref spawnedEnemyCompanion, ref enemyCompanionAnimator);
    }

    GameObject ResolveCompanionPrefab(GameObject explicitPrefab, GameObject defaultPrefab, MaskType companionMask)
    {
        if (explicitPrefab != null) return explicitPrefab;
        if (defaultPrefab != null) return defaultPrefab;

        // Auto-resolve from MaskData using the companion's mask type
        if (companionMask != MaskType.None && maskData != null)
        {
            var battleMask = maskData.GetBattleMask(companionMask);
            if (battleMask != null && battleMask.battlePrefab != null)
                return battleMask.battlePrefab;
        }
        return null;
    }

    void SpawnCompanion(GameObject prefab, Transform slot, bool flip, ref GameObject spawned, ref BattleFighterAnimator anim)
    {
        if (prefab == null || slot == null) return;

        Vector3 spawnPos = slot.position;
        spawnPos.z += 1f;
        spawned = Instantiate(prefab, spawnPos, Quaternion.identity, slot);
        spawned.transform.localScale *= companionScale;

        if (flip)
        {
            Vector3 s = spawned.transform.localScale;
            spawned.transform.localScale = new Vector3(-Mathf.Abs(s.x), s.y, s.z);
        }

        anim = EnsureFighterAnimator(spawned);
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
        playerAnimator = null;
        enemyAnimator = null;
        playerCompanionAnimator = null;
        enemyCompanionAnimator = null;

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
        if (spawnedPlayerCompanion != null)
        {
            Destroy(spawnedPlayerCompanion);
            spawnedPlayerCompanion = null;
        }
        if (spawnedEnemyCompanion != null)
        {
            Destroy(spawnedEnemyCompanion);
            spawnedEnemyCompanion = null;
        }
    }

    static BattleFighterAnimator EnsureFighterAnimator(GameObject go)
    {
        var anim = go.GetComponentInChildren<BattleFighterAnimator>();
        if (anim == null)
            anim = go.AddComponent<BattleFighterAnimator>();
        return anim;
    }

    public void SwapFighterSprite(bool isPlayer, BattleMaskData mask)
    {
        if (mask == null) return;
        GameObject prefab = mask.battlePrefab;
        if (prefab == null) return;

        if (isPlayer)
        {
            if (spawnedPlayer != null)
                Destroy(spawnedPlayer);
            spawnedPlayer = Instantiate(prefab, playerSlot.position, Quaternion.identity, playerSlot);
            playerAnimator = EnsureFighterAnimator(spawnedPlayer);
        }
        else
        {
            if (spawnedEnemy != null)
                Destroy(spawnedEnemy);
            spawnedEnemy = Instantiate(prefab, enemySlot.position, Quaternion.identity, enemySlot);
            Vector3 s = spawnedEnemy.transform.localScale;
            spawnedEnemy.transform.localScale = new Vector3(-Mathf.Abs(s.x), s.y, s.z);
            enemyAnimator = EnsureFighterAnimator(spawnedEnemy);
        }
    }
}
