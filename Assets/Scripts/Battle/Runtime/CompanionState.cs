using UnityEngine;

public class CompanionState
{
    public BattleMaskData Mask { get; }
    public FighterState Owner { get; }
    public int TurnsSinceLastAttack { get; private set; }
    public int NextAttackInterval { get; private set; }

    public CompanionState(BattleMaskData mask, FighterState owner)
    {
        Mask = mask;
        Owner = owner;
        TurnsSinceLastAttack = 0;
        NextAttackInterval = Random.Range(2, 4); // 2 or 3
    }

    public BattleActionData GetCompanionAction()
    {
        if (Mask == null || Mask.availableActions == null)
            return null;

        // Priority 1: first debuff action (has statusToApply, not healing)
        foreach (var action in Mask.availableActions)
        {
            if (action != null && action.statusToApply != null && !action.isHealing)
                return action;
        }

        // Priority 2: first attack with basePower > 0
        foreach (var action in Mask.availableActions)
        {
            if (action != null && action.category == ActionCategory.Attack && action.basePower > 0)
                return action;
        }

        return null;
    }

    public bool ShouldAttackThisTurn()
    {
        return TurnsSinceLastAttack >= NextAttackInterval;
    }

    public void OnTurnStart()
    {
        TurnsSinceLastAttack++;
    }

    public void ResetAfterAttack()
    {
        TurnsSinceLastAttack = 0;
        NextAttackInterval = Random.Range(2, 4); // 2 or 3
    }

    public int TurnsUntilAttack()
    {
        int remaining = NextAttackInterval - TurnsSinceLastAttack;
        return Mathf.Max(0, remaining);
    }
}
