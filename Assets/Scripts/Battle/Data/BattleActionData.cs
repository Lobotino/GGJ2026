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

    [Header("Animation")]
    public string animationTrigger;

    [Header("Defensive Flags")]
    public bool grantsGuard = false;
    public bool grantsCounter = false;

    [Header("Extended Effects")]
    public StatusDefinition selfStatusOnUse;
    public StatusType[] removeStatusTypes;
    public int statusDurationReduction;
    public StatusDefinition secondaryStatus;
    public bool conditionalBonusOnTargetStatus;
    public StatusType requiredTargetStatus;
    public float conditionalDamageMultiplier = 1f;
    public bool conditionalAPGain;
    public int apGainAmount;
    public bool isEnhancedCounter;
    public float enhancedCounterMultiplier = 1f;
    public bool overrideGuardMultiplier;
    public float guardMultiplierOverride = 0.5f;
    public bool reactiveGuard;
    public StatusDefinition reactiveGuardStatus;
    public bool lastActionBonus;
    public float lastActionMultiplier = 1f;
    public bool firstTurnBonus;
    public float firstTurnMultiplier = 1f;
    public bool selfDebuffIfConditionFails;
    public StatusDefinition selfDebuffOnFail;
}
