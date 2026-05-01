namespace LayerBase.Core.Event;

public enum EventHandledState
{
    Continue,


    Handled,


    HandledAndContinue
}

public readonly struct Event<T> where T : struct
{
    public readonly T Value;

    public Event(T value)
    {
        Value = value;
    }

    public static implicit operator T(Event<T> e) => e.Value;
    public static implicit operator Event<T>(T value) => new(value);
}
