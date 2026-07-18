namespace LayerBase.Core.Event;

using System.Threading;

/// <summary>
/// 事件类型 ID 分配器。
///
/// 作用：
/// 为每一个首次访问的事件类型分配一个全局唯一的 int ID。
///
/// 注意：
/// 该类型只负责分配 ID。
/// 不再保存 Type -> id 的字典。
/// 不再保存 id -> Type 的反查表。
/// </summary>
internal static class EventTypeIdAllocator
{
    /// <summary>
    /// 下一个可分配的事件类型 ID。
    /// </summary>
    private static int s_nextId;

    public static int Allocate()
    {
        return Interlocked.Increment(ref s_nextId);
    }

    public static int MaxId => Volatile.Read(ref s_nextId);
}

/// <summary>
/// 每一种事件类型对应的静态 ID 容器。
///
/// 设计说明：
/// EventTypeId<TEvent> 会为每个 TEvent 生成独立的静态字段。
/// 例如：
/// EventTypeId<CardPlayedEvent>.Id
/// EventTypeId<DamageEvent>.Id
/// 是两份不同的静态字段。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// 例如 CardPlayedEvent、DamageEvent、TurnStartedEvent。
/// </typeparam>
internal static class EventTypeId<TEvent> where TEvent : struct
{
    /// <summary>
    /// 当前 TEvent 对应的事件类型 ID。
    ///
    /// 逻辑说明：
    /// 1. 第一次访问 EventTypeId<TEvent>.Id 时，会调用 EventTypeIdAllocator.Allocate()。
    /// 2. Allocate() 会分配一个新的唯一 int ID。
    /// 3. 后续再次访问 EventTypeId<TEvent>.Id 时，不会再次分配。
    /// 4. 后续访问只是读取静态 readonly 字段。
    ///
    /// 性能说明：
    /// 热路径中不再发生 Dictionary 查找。
    /// 热路径中不再发生 lock。
    /// 热路径中不再使用 Type 作为 key。
    /// </summary>
    public static readonly int Id = EventTypeIdAllocator.Allocate();
}