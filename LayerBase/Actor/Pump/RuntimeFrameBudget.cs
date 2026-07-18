namespace LayerBase.Actor;

public struct RuntimeFrameBudget
{
    public int MaxWorkItems;
    public int UsedWorkItems;
    public long DeadlineTicks;
    public int RemainingPostCount;
    public int StartingScopeIndex;

    public RuntimeFrameBudget(int maxEvents, int usedEvents, long deadlineTicks,
        int remainingPostCount = 0, int startingScopeIndex = 0)
    {
        MaxWorkItems = maxEvents;
        UsedWorkItems = usedEvents;
        DeadlineTicks = deadlineTicks;
        RemainingPostCount = remainingPostCount;
        StartingScopeIndex = startingScopeIndex;
    }

    public int MaxEvents
    {
        get => MaxWorkItems;
        set => MaxWorkItems = value;
    }

    public int UsedEvents
    {
        get => UsedWorkItems;
        set => UsedWorkItems = value;
    }

    public int RemainingWorkItems
    {
        get
        {
            if (MaxWorkItems <= 0)
                return int.MaxValue;

            int remaining = MaxWorkItems - UsedWorkItems;
            return remaining > 0 ? remaining : 0;
        }
    }

    public bool HasRemainingWork()
    {
        return MaxWorkItems <= 0 || UsedWorkItems < MaxWorkItems;
    }

    public bool HasRemainingTime(long nowTicks)
    {
        return DeadlineTicks <= 0 || nowTicks < DeadlineTicks;
    }

    public bool CanContinue(long nowTicks)
    {
        return HasRemainingWork() && HasRemainingTime(nowTicks);
    }

    public void Consume(int count)
    {
        if (count <= 0)
            return;

        UsedWorkItems += count;
    }

    public bool HasRemainingEventBudget()
    {
        return HasRemainingWork();
    }

    public bool HasRemainingTimeBudget(long nowTicks)
    {
        return HasRemainingTime(nowTicks);
    }

    public void ConsumeEvent()
    {
        Consume(1);
    }

    public int RemainingEventBudget => RemainingWorkItems;
}
