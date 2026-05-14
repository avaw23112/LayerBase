using System.Runtime.CompilerServices;

namespace LayerBase.Event.EventMetaData;

public static class EventMetaDataRegistry
{
    public static void RegisterMetaData<EventType>(IEventMetaData metaData)
        where EventType : struct
    {
        if (metaData == null) throw new ArgumentNullException(nameof(metaData));
        EventMetaDataHandler.RegisterMetaData<EventType>(metaData);
    }

    public static Actor.ActorMailOptions? GetActorMailOptions<TEvent>()
        where TEvent : struct
    {
        EventMetaDataAutoRegister<TEvent>.EnsureInitialized();
        EventMetaData<TEvent>? metaData = EventMetaDataHandler.ResolveRegisteredMetaData<TEvent>();
        return metaData?.GetActorMailOptions();
    }
}

/// <summary>
/// EventMetaData 自动注册器。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
public static class EventMetaDataAutoRegister<TEvent>
    where TEvent : struct
{
    /// <summary>
    /// 是否已经触发过 TEvent 的静态构造函数。
    /// </summary>
    private static bool s_classConstructorTriggered;

    /// <summary>
    /// 可重复执行的元数据注册动作。
    /// </summary>
    private static Action? s_replay;

    /// <summary>
    /// 设置可重复执行的注册动作。
    /// </summary>
    public static void SetReplay(Action replay)
    {
        s_replay = replay ?? throw new ArgumentNullException(nameof(replay));
    }

    /// <summary>
    /// 确保 TEvent 的元数据已注册。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureInitialized()
    {
        if (!s_classConstructorTriggered)
        {
            EnsureClassConstructorTriggeredSlow();
        }

        s_replay?.Invoke();
    }

    /// <summary>
    /// 慢路径：触发 TEvent 静态构造函数。
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void EnsureClassConstructorTriggeredSlow()
    {
        if (s_classConstructorTriggered)
        {
            return;
        }

        RuntimeHelpers.RunClassConstructor(typeof(TEvent).TypeHandle);
        s_classConstructorTriggered = true;
    }
}
