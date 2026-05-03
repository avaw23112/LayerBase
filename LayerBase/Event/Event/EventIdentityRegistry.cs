using System.Collections.Concurrent;
using System.Reflection;

namespace LayerBase.Core.Event;

/// <summary>
/// 事件稳定身份注册表。
///
/// 注意：
/// 这个注册表不参与热路径事件派发。
/// 它只为调试、日志、Runtime Policy Dump 和回放系统 provide 稳定信息。
/// </summary>
public static class EventIdentityRegistry
{
    /// <summary>
    /// runtime event id 到事件身份的映射。
    /// key 是 EventTypeId<TEvent>.Id。
    /// value 是事件身份快照。
    /// </summary>
    private static readonly ConcurrentDictionary<int, EventIdentity> s_byRuntimeId = new();

    /// <summary>
    /// stable event id 到事件身份的映射。
    /// key 是 EventIdentityAttribute.StableId。
    /// value 是事件身份快照。
    /// </summary>
    private static readonly ConcurrentDictionary<int, EventIdentity> s_byStableId = new();

    /// <summary>
    /// stable event key 到事件身份的映射。
    /// key 是 EventIdentityAttribute.StableKey。
    /// value 是事件身份快照。
    /// </summary>
    private static readonly ConcurrentDictionary<string, EventIdentity> s_byStableKey = new();

    /// <summary>
    /// 获取或创建指定事件类型的身份信息。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 事件类型。
    /// 它必须是 struct，以保持和现有事件系统约束一致。
    /// </typeparam>
    /// <returns>
    /// 当前事件类型的身份快照。
    /// </returns>
    public static EventIdentity GetOrCreate<TEvent>()
        where TEvent : struct
    {
        var runtimeId = EventTypeId<TEvent>.Id;

        if (s_byRuntimeId.TryGetValue(runtimeId, out var existing))
        {
            return existing;
        }

        return CreateAndRegister<TEvent>(runtimeId);
    }

    /// <summary>
    /// 创建并注册事件身份。
    /// </summary>
    /// <typeparam name="TEvent">
    /// 事件类型。
    /// </typeparam>
    /// <param name="runtimeId">
    /// 当前运行期事件 ID。
    /// </param>
    /// <returns>
    /// 新创建的事件身份快照。
    /// </returns>
    private static EventIdentity CreateAndRegister<TEvent>(int runtimeId)
        where TEvent : struct
    {
        var eventType = typeof(TEvent);
        var attr = eventType.GetCustomAttribute<EventIdentityAttribute>();

        var stableId = attr?.StableId ?? 0;
        var stableKey = attr?.StableKey ?? eventType.FullName ?? eventType.Name;
        var version = attr?.Version ?? 1;

        var identity = new EventIdentity(
            runtimeId: runtimeId,
            stableId: stableId,
            stableKey: stableKey,
            version: version,
            eventType: eventType);

        if (!s_byRuntimeId.TryAdd(runtimeId, identity))
        {
            return s_byRuntimeId[runtimeId];
        }

        if (stableId > 0 && !s_byStableId.TryAdd(stableId, identity))
        {
            throw new InvalidOperationException(
                $"Duplicate stable event id: {stableId}.");
        }

        if (!s_byStableKey.TryAdd(stableKey, identity))
        {
            throw new InvalidOperationException(
                $"Duplicate stable event key: {stableKey}.");
        }

        return identity;
    }

    /// <summary>
    /// 尝试通过运行期 ID 查找事件身份。
    /// </summary>
    /// <param name="runtimeId">
    /// 运行期事件 ID。
    /// </param>
    /// <param name="identity">
    /// 找到的事件身份。
    /// </param>
    /// <returns>
    /// true 表示找到；false 表示未注册。
    /// </returns>
    public static bool TryGetByRuntimeId(int runtimeId, out EventIdentity identity)
    {
        return s_byRuntimeId.TryGetValue(runtimeId, out identity);
    }

    /// <summary>
    /// 清理注册表。
    /// 该方法应在 LayerHub.Reset 时调用。
    /// </summary>
    public static void Reset()
    {
        s_byRuntimeId.Clear();
        s_byStableId.Clear();
        s_byStableKey.Clear();
    }
}
