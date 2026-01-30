public class ActiveStatus
{
    public StatusDefinition Definition { get; }
    public int RemainingTurns { get; private set; }

    public ActiveStatus(StatusDefinition definition)
    {
        Definition = definition;
        RemainingTurns = definition != null ? definition.durationTurns : 0;
    }

    public void Refresh()
    {
        if (Definition != null)
            RemainingTurns = Definition.durationTurns;
    }

    public void TickDuration()
    {
        RemainingTurns--;
    }

    public bool IsExpired => RemainingTurns <= 0;
}
