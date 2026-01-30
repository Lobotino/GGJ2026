public static class AIDecider
{
    public static AICommand Decide(FighterState self, FighterState opponent, AIProfile profile)
    {
        if (self == null || profile == null)
            return AICommand.EndTurn();

        float hpPercent = self.MaxHP > 0 ? (float)self.CurrentHP / self.MaxHP : 0f;

        if (profile.rules != null)
        {
            foreach (var rule in profile.rules)
            {
                if (hpPercent <= rule.hpThreshold)
                {
                    if (rule.switchToMask != null && self.CanChangeMask(rule.switchToMask))
                        return AICommand.ChangeMask(rule.switchToMask);
                }

                if (rule.preferredAction != null && self.CanUseAction(rule.preferredAction))
                {
                    if (!ShouldSkipGuard(self, profile, rule.preferredAction))
                        return AICommand.UseAction(rule.preferredAction);
                }
            }
        }

        if (profile.fallbackAction != null && self.CanUseAction(profile.fallbackAction))
        {
            if (!ShouldSkipGuard(self, profile, profile.fallbackAction))
                return AICommand.UseAction(profile.fallbackAction);
        }

        return AICommand.EndTurn();
    }

    static bool ShouldSkipGuard(FighterState self, AIProfile profile, BattleActionData action)
    {
        if (profile == null || action == null) return false;
        if (profile.guardEveryNTurns <= 1) return false;
        if (!action.grantsGuard) return false;
        if (self.Context == null) return false;
        int turnIndex = self.Context.TurnNumber;
        return turnIndex % profile.guardEveryNTurns != 0;
    }
}

public struct AICommand
{
    public bool IsEndTurn;
    public BattleActionData Action;
    public BattleMaskData Mask;

    public static AICommand UseAction(BattleActionData action)
    {
        return new AICommand { Action = action };
    }

    public static AICommand ChangeMask(BattleMaskData mask)
    {
        return new AICommand { Mask = mask };
    }

    public static AICommand EndTurn()
    {
        return new AICommand { IsEndTurn = true };
    }
}
