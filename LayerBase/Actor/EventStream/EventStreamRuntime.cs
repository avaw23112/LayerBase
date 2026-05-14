using System.Runtime.CompilerServices;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

/// <summary>
/// Generic EventStreamCenter wrapper bound to one runtime/archetype pair.
/// </summary>
internal sealed class EventStreamRuntime<TEvent> : EventStreamRuntimeBase
    where TEvent : struct
{
    private static EventStreamRuntime<TEvent>?[][] s_byRuntime =
        Array.Empty<EventStreamRuntime<TEvent>?[]>();

    private static readonly object s_lock = new();

    private readonly int _runtimeIndex;
    private readonly int _archetypeId;
    private readonly EventStreamCenter<TEvent> _center;

    public override int EventTypeId { get; }

    public override bool IsEmpty => _center.IsEmpty;

    public override int RuntimeIndex => _runtimeIndex;

    public override int ArchetypeId => _archetypeId;

    public EventStreamRuntime(
        int runtimeIndex,
        int archetypeId,
        EventStreamOptions options)
    {
        _runtimeIndex = runtimeIndex;
        _archetypeId = archetypeId;
        _center = new EventStreamCenter<TEvent>(options);
        EventTypeId = EventTypeId<TEvent>.Id;
    }

    public EventStreamCenter<TEvent> Center => _center;

    public override int Pump(int maxCount)
    {
        return _center.Pump(maxCount);
    }

    public override void UnregisterHandler(int slotIndex)
    {
        _center.UnregisterHandler(slotIndex);
    }

    public static void BindWorld(EventStreamRuntime<TEvent> runtime)
    {
        lock (s_lock)
        {
            EnsureRuntimeCapacity(runtime._runtimeIndex);

            EventStreamRuntime<TEvent>?[]? byArchetype =
                s_byRuntime[runtime._runtimeIndex];

            if (byArchetype == null)
            {
                byArchetype = CreateArchetypeArray(runtime._archetypeId);
                s_byRuntime[runtime._runtimeIndex] = byArchetype;
            }
            else if ((uint)runtime._archetypeId >= (uint)byArchetype.Length)
            {
                EnsureArchetypeCapacity(ref byArchetype, runtime._archetypeId);
                s_byRuntime[runtime._runtimeIndex] = byArchetype;
            }

            byArchetype[runtime._archetypeId] = runtime;
        }
    }

    public static void UnbindWorld(int runtimeIndex, int archetypeId)
    {
        lock (s_lock)
        {
            if ((uint)runtimeIndex >= (uint)s_byRuntime.Length)
            {
                return;
            }

            EventStreamRuntime<TEvent>?[]? byArchetype = s_byRuntime[runtimeIndex];
            if (byArchetype == null ||
                (uint)archetypeId >= (uint)byArchetype.Length)
            {
                return;
            }

            byArchetype[archetypeId] = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EventStreamCenter<TEvent>? GetCenterUnchecked(int runtimeIndex, int archetypeId)
    {
        EventStreamRuntime<TEvent>?[][] byRuntime = s_byRuntime;

        if ((uint)runtimeIndex >= (uint)byRuntime.Length)
        {
            return null;
        }

        EventStreamRuntime<TEvent>?[]? byArchetype = byRuntime[runtimeIndex];
        if (byArchetype == null ||
            (uint)archetypeId >= (uint)byArchetype.Length)
        {
            return null;
        }

        return byArchetype[archetypeId]?._center;
    }

    public static void ResetAll()
    {
        lock (s_lock)
        {
            s_byRuntime = Array.Empty<EventStreamRuntime<TEvent>?[]>();
        }
    }

    private static void EnsureRuntimeCapacity(int runtimeIndex)
    {
        if ((uint)runtimeIndex < (uint)s_byRuntime.Length)
        {
            return;
        }

        int newSize = s_byRuntime.Length == 0 ? 4 : s_byRuntime.Length;
        while (newSize <= runtimeIndex)
        {
            newSize *= 2;
        }

        Array.Resize(ref s_byRuntime, newSize);
    }

    private static EventStreamRuntime<TEvent>?[] CreateArchetypeArray(int archetypeId)
    {
        int size = 4;
        while (size <= archetypeId)
        {
            size *= 2;
        }

        return new EventStreamRuntime<TEvent>?[size];
    }

    private static void EnsureArchetypeCapacity(
        ref EventStreamRuntime<TEvent>?[] byArchetype,
        int archetypeId)
    {
        int newSize = byArchetype.Length == 0 ? 4 : byArchetype.Length;
        while (newSize <= archetypeId)
        {
            newSize *= 2;
        }

        Array.Resize(ref byArchetype, newSize);
    }
}
