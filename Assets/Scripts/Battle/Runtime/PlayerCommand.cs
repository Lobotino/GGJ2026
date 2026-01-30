public enum PlayerCommandType
{
    UseAction,
    ChangeMask,
    EndTurn
}

public struct PlayerCommand
{
    public PlayerCommandType Type;
    public BattleActionData Action;
    public BattleMaskData Mask;
}
