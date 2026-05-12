using System.Runtime.CompilerServices;

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

    /// <summary>
    /// 获取剩余事件预算。
    ///
    /// 返回值：
    /// 如果 MaxEvents <= 0（无限制），返回 int.MaxValue。
    /// 否则返回剩余可处理事件数量，最小为 0。
    /// </summary>
    public int RemainingEventBudget
    {
        [System.Runtime.CompilerServices.MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (MaxEvents <= 0)
            {
                return int.MaxValue;
            }

            int remaining = MaxEvents - UsedEvents;
            return remaining > 0 ? remaining : 0;
        }
    }
}