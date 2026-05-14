using System.Runtime.CompilerServices;
using LayerBase.Core.Event;

namespace LayerBase.Actor;

/// <summary>
/// EventStreamCenter 运行时包装。
///
/// 作用：
/// 1. 将泛型 EventStreamCenter 包装为非泛型 EventStreamRuntimeBase。
/// 2. 让 ActorWorld 可以统一保存、Pump、注销不同事件类型的 EventStream。
/// 3. 为指定 runtimeIndex + archetypeId + TEvent 维护唯一 EventStreamCenter。
///
/// 重要说明：
/// TEvent 本身已经让当前类型变成泛型静态缓存。
/// 因此静态缓存不需要再把 eventTypeId 编进数组索引。
///
/// 旧设计使用：
/// key = (runtimeIndex << 20) | (archetypeId << 10) | eventTypeId
///
/// 旧设计问题：
/// runtimeIndex 稍微变大时，Array.Resize(ref s_worlds, key + 1) 会创建巨大稀疏数组。
/// 例如 runtimeIndex = 12 时，12 << 20 约等于 1200 万个引用槽，容易造成 100MB 级别冷启动分配。
///
/// 新设计使用：
/// s_byRuntime[runtimeIndex][archetypeId]
///
/// 好处：
/// 1. runtimeIndex 只扩 runtime 维度。
/// 2. archetypeId 只扩 archetype 维度。
/// 3. 不再因为位移 key 产生巨大空洞数组。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// 必须是 struct，保持事件值语义并减少托管堆分配。
/// </typeparam>
internal sealed class EventStreamRuntime<TEvent> : EventStreamRuntimeBase
    where TEvent : struct
{
    /// <summary>
    /// 按 runtimeIndex 分组的 EventStreamRuntime 表。
    ///
    /// 第一层索引：
    /// runtimeIndex，表示 ActorWorld 的运行时编号。
    ///
    /// 第二层索引：
    /// archetypeId，表示 Actor 类型所在的行为原型编号。
    ///
    /// 泛型参数 TEvent：
    /// 已经天然区分事件类型，所以这里不需要再用 eventTypeId 作为数组索引。
    /// </summary>
    private static EventStreamRuntime<TEvent>?[][] s_byRuntime =
        Array.Empty<EventStreamRuntime<TEvent>?[]>();

    /// <summary>
    /// 静态表写入锁。
    ///
    /// 作用：
    /// BindWorld / UnbindWorld / ResetAll 会修改静态表，需要避免并发写冲突。
    /// 热路径 GetCenterUnchecked 不加锁，只读当前数组快照。
    /// </summary>
    private static readonly object s_lock = new();

    /// <summary>
    /// 当前 ActorWorld 的运行时编号。
    /// </summary>
    private readonly int _runtimeIndex;

    /// <summary>
    /// 当前 EventStreamRuntime 绑定的 Actor archetype 编号。
    /// </summary>
    private readonly int _archetypeId;

    /// <summary>
    /// 当前事件类型对应的 EventStreamCenter。
    /// </summary>
    private readonly EventStreamCenter<TEvent> _center;

    /// <summary>
    /// 兼容旧 ActorWorld.GetOrCreateEventStreamRuntime 的搜索 key。
    ///
    /// 作用：
    /// 当前 ActorWorld.GetOrCreateEventStreamRuntime 仍然会用旧 key 遍历 _eventStreamRuntimes。
    /// 为了让本文件可以单独替换，暂时保留该属性。
    ///
    /// 注意：
    /// 这个 key 只用于 List 内比较，不再用于静态数组索引，因此不会触发巨大 Array.Resize。
    /// </summary>
    public int SearchKey => MakeLegacySearchKey(
        _runtimeIndex,
        _archetypeId,
        EventTypeId);

    /// <summary>
    /// 当前事件类型 ID。
    /// </summary>
    public override int EventTypeId { get; }

    /// <summary>
    /// 当前事件流是否为空。
    /// </summary>
    public override bool IsEmpty => _center.IsEmpty;

    /// <summary>
    /// 当前 runtimeIndex。
    /// </summary>
    public int RuntimeIndex => _runtimeIndex;

    /// <summary>
    /// 当前 archetypeId。
    /// </summary>
    public int ArchetypeId => _archetypeId;

    /// <summary>
    /// 构造 EventStreamRuntime。
    /// </summary>
    /// <param name="runtimeIndex">
    /// ActorWorld 的运行时编号。
    /// 用于隔离多个 ActorWorld 的静态缓存。
    /// </param>
    /// <param name="archetypeId">
    /// Actor 行为原型编号。
    /// 用于区分同一个 ActorWorld 内不同 Actor 类型的事件流。
    /// </param>
    /// <param name="options">
    /// EventStream 配置。
    /// 包含 SegmentCapacity 和 MaxRetainedSegments 等池化参数。
    /// </param>
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

    /// <summary>
    /// 当前事件类型的 EventStreamCenter。
    /// </summary>
    public EventStreamCenter<TEvent> Center => _center;

    /// <summary>
    /// Pump 当前事件流。
    /// </summary>
    /// <param name="maxCount">
    /// 本次最多处理多少封邮件。
    /// 用于接入 RuntimeFrameBudget。
    /// </param>
    /// <returns>
    /// 实际处理的邮件数量。
    /// </returns>
    public override int Pump(int maxCount)
    {
        return _center.Pump(maxCount);
    }

    /// <summary>
    /// 注销指定 slot 的事件处理器。
    /// </summary>
    /// <param name="slotIndex">
    /// Actor slot 下标。
    /// </param>
    public override void UnregisterHandler(int slotIndex)
    {
        _center.UnregisterHandler(slotIndex);
    }

    /// <summary>
    /// 绑定 EventStreamRuntime 到静态缓存。
    /// </summary>
    /// <param name="runtime">
    /// 要绑定的 EventStreamRuntime。
    /// </param>
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
                EnsureArchetypeCapacity(
                    ref byArchetype,
                    runtime._archetypeId);

                s_byRuntime[runtime._runtimeIndex] = byArchetype;
            }

            byArchetype[runtime._archetypeId] = runtime;
        }
    }

    /// <summary>
    /// 解绑指定 ActorWorld + archetype 的 EventStreamRuntime。
    /// </summary>
    /// <param name="runtimeIndex">
    /// ActorWorld 的运行时编号。
    /// </param>
    /// <param name="archetypeId">
    /// Actor 行为原型编号。
    /// </param>
    public static void UnbindWorld(int runtimeIndex, int archetypeId)
    {
        lock (s_lock)
        {
            if ((uint)runtimeIndex >= (uint)s_byRuntime.Length)
            {
                return;
            }

            EventStreamRuntime<TEvent>?[]? byArchetype =
                s_byRuntime[runtimeIndex];

            if (byArchetype == null ||
                (uint)archetypeId >= (uint)byArchetype.Length)
            {
                return;
            }

            byArchetype[archetypeId] = null;
        }
    }

    /// <summary>
    /// 获取指定 ActorWorld + archetype 的 EventStreamCenter。
    /// </summary>
    /// <param name="runtimeIndex">
    /// ActorWorld 的运行时编号。
    /// </param>
    /// <param name="archetypeId">
    /// Actor 行为原型编号。
    /// </param>
    /// <returns>
    /// 找到则返回 EventStreamCenter，否则返回 null。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EventStreamCenter<TEvent>? GetCenterUnchecked(int runtimeIndex, int archetypeId)
    {
        EventStreamRuntime<TEvent>?[][] byRuntime =
            s_byRuntime;

        if ((uint)runtimeIndex >= (uint)byRuntime.Length)
        {
            return null;
        }

        EventStreamRuntime<TEvent>?[]? byArchetype =
            byRuntime[runtimeIndex];

        if (byArchetype == null ||
            (uint)archetypeId >= (uint)byArchetype.Length)
        {
            return null;
        }

        return byArchetype[archetypeId]?._center;
    }

    /// <summary>
    /// 清理所有静态状态。
    ///
    /// 作用：
    /// 主要用于测试隔离。
    /// 会释放二维静态表对 runtime 的引用，避免测试之间互相污染。
    /// </summary>
    public static void ResetAll()
    {
        lock (s_lock)
        {
            s_byRuntime = Array.Empty<EventStreamRuntime<TEvent>?[]>();
        }
    }

    /// <summary>
    /// 确保 runtime 维度容量足够。
    /// </summary>
    /// <param name="runtimeIndex">
    /// 需要支持的 runtimeIndex。
    /// </param>
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

        Array.Resize(
            ref s_byRuntime,
            newSize);
    }

    /// <summary>
    /// 创建 archetype 维度数组。
    /// </summary>
    /// <param name="archetypeId">
    /// 需要支持的 archetypeId。
    /// </param>
    /// <returns>
    /// 可容纳 archetypeId 的数组。
    /// </returns>
    private static EventStreamRuntime<TEvent>?[] CreateArchetypeArray(int archetypeId)
    {
        int size = 4;

        while (size <= archetypeId)
        {
            size *= 2;
        }

        return new EventStreamRuntime<TEvent>?[size];
    }

    /// <summary>
    /// 确保 archetype 维度容量足够。
    /// </summary>
    /// <param name="byArchetype">
    /// archetype 维度数组引用。
    /// </param>
    /// <param name="archetypeId">
    /// 需要支持的 archetypeId。
    /// </param>
    private static void EnsureArchetypeCapacity(
        ref EventStreamRuntime<TEvent>?[] byArchetype,
        int archetypeId)
    {
        int newSize = byArchetype.Length == 0 ? 4 : byArchetype.Length;

        while (newSize <= archetypeId)
        {
            newSize *= 2;
        }

        Array.Resize(
            ref byArchetype,
            newSize);
    }

    /// <summary>
    /// 旧 SearchKey 计算。
    ///
    /// 作用：
    /// 仅用于兼容 ActorWorld.GetOrCreateEventStreamRuntime 中的 List 查询。
    /// 不再作为静态数组索引。
    /// </summary>
    /// <param name="runtimeIndex">
    /// ActorWorld 运行时编号。
    /// </param>
    /// <param name="archetypeId">
    /// Actor 行为原型编号。
    /// </param>
    /// <param name="eventTypeId">
    /// 事件类型 ID。
    /// </param>
    /// <returns>
    /// 旧版搜索 key。
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int MakeLegacySearchKey(int runtimeIndex, int archetypeId, int eventTypeId)
    {
        return (runtimeIndex << 20) | (archetypeId << 10) | eventTypeId;
    }
}
