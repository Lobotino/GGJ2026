public class ActiveStatus
{
    public StatusDefinition Definition { get; private set; }
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

    public void Refresh(StatusDefinition newDefinition)
    {
        if (newDefinition != null)
        {
            Definition = newDefinition;
            RemainingTurns = newDefinition.durationTurns;
        }
        else
        {
            Refresh();
        }
    }

    public void ReduceDuration(int amount)
    {
        RemainingTurns = RemainingTurns - amount;
        if (RemainingTurns < 0)
            RemainingTurns = 0;
    }

    public void TickDuration()
    {
        RemainingTurns--;
    }

    public bool IsExpired => RemainingTurns <= 0;
}
