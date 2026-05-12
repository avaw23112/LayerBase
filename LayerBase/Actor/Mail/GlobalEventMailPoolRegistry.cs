using LayerBase.Core.Event;

namespace LayerBase.Actor;

internal sealed class GlobalEventMailPoolRegistry
{
    private object?[] _poolsByEventId = new object?[64];

    public EventMailPool<TEvent> GetOrCreate<TEvent>(ActorMailOptions options)
        where TEvent : struct
    {
        int eventId = EventTypeId<TEvent>.Id;
        EnsureCapacity(eventId);

        object? existing = _poolsByEventId[eventId];
        if (existing != null)
        {
            return (EventMailPool<TEvent>)existing;
        }

        var pool = new EventMailPool<TEvent>(options);
        _poolsByEventId[eventId] = pool;
        return pool;
    }

    public void Clear()
    {
        Array.Clear(_poolsByEventId, 0, _poolsByEventId.Length);
    }

    private void EnsureCapacity(int eventId)
    {
        if ((uint)eventId < (uint)_poolsByEventId.Length)
        {
            return;
        }

        int newSize = _poolsByEventId.Length;
        while (newSize <= eventId)
        {
            newSize <<= 1;
        }

        Array.Resize(ref _poolsByEventId, newSize);
    }
}