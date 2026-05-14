using System.Runtime.CompilerServices;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

/// <summary>
/// EventStreamCenter 运行时包装。
///
/// 作用：
/// 将泛型 EventStreamCenter 包装为非泛型接口，便于 ActorWorld 管理。
/// 每个 (runtimeIndex, archetypeId, eventTypeId) 组合对应一个独立的 EventStreamCenter。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
internal sealed class EventStreamRuntime<TEvent> : EventStreamRuntimeBase
    where TEvent : struct
{
    // key: (runtimeIndex << 20 | archetypeId << 10 | eventTypeId) 保证不同组合使用不同的 center
    private static EventStreamRuntime<TEvent>?[] s_worlds = Array.Empty<EventStreamRuntime<TEvent>?>();
    private static readonly object s_lock = new();

    private readonly int _key;
    private readonly EventStreamCenter<TEvent> _center;

    /// <summary>
    /// 用于查找的 key。
    /// </summary>
    public int SearchKey => _key;

    /// <summary>
    /// 事件类型 ID。
    /// </summary>
    public override int EventTypeId { get; }

    /// <summary>
    /// 该事件流是否为空。
    /// </summary>
    public override bool IsEmpty => _center.IsEmpty;

    /// <summary>
    /// 构造 EventStreamRuntime。
    /// </summary>
    public EventStreamRuntime(int runtimeIndex, int archetypeId, EventStreamOptions options)
    {
        int eventTypeId = EventTypeId<TEvent>.Id;
        _key = MakeKey(runtimeIndex, archetypeId, eventTypeId);
        _center = new EventStreamCenter<TEvent>(options);
        EventTypeId = eventTypeId;
    }

    /// <summary>
    /// 获取该事件类型的 EventStreamCenter。
    /// </summary>
    public EventStreamCenter<TEvent> Center => _center;

    /// <summary>
    /// Pump 该事件类型的事件流。
    /// </summary>
    public override int Pump(int maxCount) => _center.Pump(maxCount);

    /// <summary>
    /// 注销指定 slot 的事件处理器。
    /// </summary>
    public override void UnregisterHandler(int slotIndex) => _center.UnregisterHandler(slotIndex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int MakeKey(int runtimeIndex, int archetypeId, int eventTypeId)
    {
        return (runtimeIndex << 20) | (archetypeId << 10) | eventTypeId;
    }

    /// <summary>
    /// 绑定 EventStreamRuntime。
    /// </summary>
    public static void BindWorld(EventStreamRuntime<TEvent> runtime)
    {
        lock (s_lock)
        {
            int key = runtime._key;
            if (key >= s_worlds.Length)
            {
                Array.Resize(ref s_worlds, key + 1);
            }
            s_worlds[key] = runtime;
        }
    }

    /// <summary>
    /// 解绑 EventStreamRuntime。
    /// </summary>
    public static void UnbindWorld(int runtimeIndex, int archetypeId)
    {
        lock (s_lock)
        {
            int eventTypeId = EventTypeId<TEvent>.Id;
            int key = MakeKey(runtimeIndex, archetypeId, eventTypeId);
            if ((uint)key < (uint)s_worlds.Length)
            {
                s_worlds[key] = null;
            }
        }
    }

    /// <summary>
    /// 获取指定 (runtimeIndex, archetypeId) 的 EventStreamCenter。
    /// </summary>
    public static EventStreamCenter<TEvent>? GetCenterUnchecked(int runtimeIndex, int archetypeId)
    {
        int eventTypeId = EventTypeId<TEvent>.Id;
        int key = MakeKey(runtimeIndex, archetypeId, eventTypeId);
        if ((uint)key < (uint)s_worlds.Length)
        {
            return s_worlds[key]?._center;
        }
        return null;
    }

    /// <summary>
    /// 清理所有静态状态（用于测试隔离）。
    /// </summary>
    public static void ResetAll()
    {
        lock (s_lock)
        {
            Array.Clear(s_worlds, 0, s_worlds.Length);
        }
    }
}
