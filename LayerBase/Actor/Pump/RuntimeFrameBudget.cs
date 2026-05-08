namespace LayerBase.Actor;

public ref struct RuntimeFrameBudget
{
    public int MaxEvents;
    public int UsedEvents;
    public long DeadlineTicks;

    public RuntimeFrameBudget(int maxEvents, int usedEvents, long deadlineTicks)
    {
        MaxEvents = maxEvents;
        UsedEvents = usedEvents;
        DeadlineTicks = deadlineTicks;
    }

    public bool HasRemainingEventBudget()
    {
        return MaxEvents <= 0 || UsedEvents < MaxEvents;
    }

    public bool HasRemainingTimeBudget(long nowTicks)
    {
        return DeadlineTicks <= 0 || nowTicks < DeadlineTicks;
    }

    public void ConsumeEvent()
    {
        UsedEvents++;
    }
}
