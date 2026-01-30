using UnityEngine;

[CreateAssetMenu(fileName = "BattleAction", menuName = "Game/Battle/Action")]
public class BattleActionData : ScriptableObject
{
    public string actionName;
    public ActionCategory category;

    public int apCost = 1;
    public int mpCost = 0;

    public int basePower = 1;
    public bool isMagical = false;
    public bool isHealing = false;
    public bool targetSelf = false;

    public StatusDefinition statusToApply;

    [Header("Defensive Flags")]
    public bool grantsGuard = false;
    public bool grantsCounter = false;
}
