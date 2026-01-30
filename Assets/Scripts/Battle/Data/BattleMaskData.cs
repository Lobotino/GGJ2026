using UnityEngine;

[CreateAssetMenu(fileName = "BattleMaskData", menuName = "Game/Battle/Mask Data")]
public class BattleMaskData : ScriptableObject
{
    public MaskType maskType;
    public string displayName;
    [TextArea] public string loreDescription;

    public StatMultiplier statMultipliers = StatMultiplier.One;
    [TextArea] public string passiveEffect;
    public StatusType vulnerability;

    public BattleActionData[] availableActions;

    [Header("Mask Change")]
    public int changeCooldownTurns = 0;
    public bool disallowConsecutiveChange = false;
    public bool applyInertia = false;
    public int inertiaApPenalty = 1;

    [Header("AP")]
    public int apBonus = 0;
}
