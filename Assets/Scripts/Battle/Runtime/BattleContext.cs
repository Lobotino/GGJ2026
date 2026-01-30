public class BattleContext
{
    // Per-battle flags
    public bool PlayerFirstStatusReduced { get; set; }
    public bool EnemyFirstStatusReduced { get; set; }
    public bool CatLastStandUsedPlayer { get; set; }
    public bool CatLastStandUsedEnemy { get; set; }
    public bool FirstAttackUsedPlayer { get; set; }
    public bool FirstAttackUsedEnemy { get; set; }

    // Per-turn tracking
    public int ActionsThisTurn { get; set; }
    public bool PhysicalHitReceivedThisTurn { get; set; }
    public bool FirstControlStatusThisTurn { get; set; }

    // Turn counter
    public int TurnNumber { get; set; }

    public void ResetTurnTracking()
    {
        ActionsThisTurn = 0;
        PhysicalHitReceivedThisTurn = false;
        FirstControlStatusThisTurn = false;
    }

    public bool GetCatLastStandUsed(bool isPlayer)
    {
        return isPlayer ? CatLastStandUsedPlayer : CatLastStandUsedEnemy;
    }

    public void SetCatLastStandUsed(bool isPlayer)
    {
        if (isPlayer)
            CatLastStandUsedPlayer = true;
        else
            CatLastStandUsedEnemy = true;
    }

    public bool GetFirstAttackUsed(bool isPlayer)
    {
        return isPlayer ? FirstAttackUsedPlayer : FirstAttackUsedEnemy;
    }

    public void SetFirstAttackUsed(bool isPlayer)
    {
        if (isPlayer)
            FirstAttackUsedPlayer = true;
        else
            FirstAttackUsedEnemy = true;
    }
}
