namespace LayerBase.Actor;

/// <summary>
/// Actor 事件处理委托。
///
/// 作用：
/// 在 Actor 创建时，将实例方法绑定为委托。
/// Pump 时直接调用此委托，不再传入 Actor 实例。
/// </summary>
/// <typeparam name="TEvent">
/// 事件类型。
/// </typeparam>
/// <param name="value">
/// 事件值。
/// </param>
public delegate void ActorEventHandler<TEvent>(in TEvent value)
    where TEvent : struct;
