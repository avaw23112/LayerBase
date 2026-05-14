using System.Runtime.CompilerServices;

namespace LayerBase.Event.EventMetaData;

public static class EventMetaDataRegistry
{
    public static void RegisterMetaData<EventType>(IEventMetaData metaData) where EventType : struct
    {
        if (metaData == null) throw new ArgumentNullException(nameof(metaData));
        EventMetaDataHandler.RegisterMetaData<EventType>(metaData);
    }

    public static Actor.ActorMailOptions? GetActorMailOptions<TEvent>() where TEvent : struct
    {
        EventMetaDataAutoRegister<TEvent>.EnsureInitialized();
        EventMetaData<TEvent>? metaData = EventMetaDataHandler.ResolveRegisteredMetaData<TEvent>();
        return metaData?.GetActorMailOptions();
    }
}

/// <summary>
/// EventMetaData 自动注册触发器。
///
/// 作用：
/// 1. 在读取 EventMetaData 前，强制触发 TEvent 的静态构造函数。
/// 2. 让源生成器写入 TEvent.static ctor 的注册逻辑稳定执行。
/// 3. 避免用户手动 new EventPrewarmBootstrapper。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
internal static class EventMetaDataAutoRegister<TEvent>
    where TEvent : struct
{
    /// <summary>
    /// 是否已经尝试触发过 TEvent 的静态构造函数。
    /// </summary>
    private static bool s_initialized;

    /// <summary>
    /// 确保 TEvent 的静态构造函数已经执行。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureInitialized()
    {
        if (s_initialized)
        {
            return;
        }

        EnsureInitializedSlow();
    }

    /// <summary>
    /// 慢路径初始化。
    ///
    /// 作用：
    /// RuntimeHelpers.RunClassConstructor 会强制执行 TEvent 的静态构造函数。
    /// 如果 TEvent 没有静态构造函数，该调用也是安全的。
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void EnsureInitializedSlow()
    {
        if (s_initialized)
        {
            return;
        }

        RuntimeHelpers.RunClassConstructor(
            typeof(TEvent).TypeHandle);

        s_initialized = true;
    }
}