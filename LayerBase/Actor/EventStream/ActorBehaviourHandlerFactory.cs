namespace LayerBase.Actor;

/// <summary>
/// Actor 行为处理器工厂委托。
///
/// 作用：
/// 在 Actor 创建时调用，将 Actor 实例转换为事件处理委托。
/// 生成器会生成类似 static actor => actor.OnEvent 的代码。
/// </summary>
/// <typeparam name="TActor">
/// Actor 类型。
/// </typeparam>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
/// <param name="actor">
/// Actor 实例。
/// </param>
/// <returns>
/// 绑定到该 Actor 实例的事件处理委托。
/// </returns>
public delegate ActorEventHandler<TEvent> ActorBehaviourHandlerFactory<TActor, TEvent>(
    TActor actor)
    where TActor : class, IActor
    where TEvent : struct;
