using UnityEngine;

[CreateAssetMenu(fileName = "BattleMaskData", menuName = "Game/Battle/Mask Data")]
public class BattleMaskData : ScriptableObject
{
    public MaskType maskType;
    public string displayName;
    [TextArea] public string loreDescription;

    public StatMultiplier statMultipliers = StatMultiplier.One;
    [TextArea] public string passiveEffect;
    public PassiveType passiveType;
    public StatusType vulnerability;

    public BattleActionData[] availableActions;

    [Header("Battle Visual")]
    [Tooltip("Prefab to spawn on the battle arena (if null, falls back to overworld prefab from MaskData)")]
    public GameObject battlePrefab;

    [Tooltip("Animator controller to swap when this mask is equipped (overrides the current controller without respawning the prefab)")]
    public RuntimeAnimatorController animatorController;

    [Header("Mask Change")]
    public int changeCooldownTurns = 0;
    public bool disallowConsecutiveChange = false;
    public bool applyInertia = false;
    public int inertiaApPenalty = 1;

    [Header("AP")]
    public int apBonus = 0;
}
