namespace LayerBase.Core.Event;

public enum EventHandledState
{
    Continue,


    Handled,


    HandledAndContinue
}

public struct Event<T> where T : struct
{
    public T Value;
    public ulong TargetMask;

    public Event(T value)
    {
        Value = value;
        TargetMask = 0;
    }

    internal int FindNextTarget(int currentLayer, EventCenter center)
    {
        var nextMask = TargetMask & ~((1UL << (currentLayer + 1)) - 1);
        return center.FindFirstBit(nextMask);
    }
}