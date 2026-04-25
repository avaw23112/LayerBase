namespace LayerBase.Core.Event;

/// <summary>
///     静态分发树接口。由 Source Generator 实现�?
/// </summary>
public interface IStaticEventDispatcher<T> where T : struct
{
    /// <summary>
    ///     静态广播：由生成的 switch �?顺序调用 组成�?
    /// </summary>
    EventHandledState Dispatch(in T value, int sourceIndex, Propagation propagation);

    /// <summary>
    ///     静态局部调用�?
    /// </summary>
    EventHandledState DispatchLocal(int layerIndex, in T value);
}

/// <summary>
///     静态分发链接中心：提供 O(1) 的生成的代码注入点�?
/// </summary>
public static class StaticEventDispatcher<T> where T : struct
{
    // 如果该值为非空，则 GlobalEventCenter 将绕过动态分发�?
    public static IStaticEventDispatcher<T>? Dispatcher;
}

